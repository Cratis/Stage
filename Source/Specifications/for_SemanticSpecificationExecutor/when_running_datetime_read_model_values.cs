// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_datetime_read_model_values : Specification
{
    [Theory]
    [InlineData(false, "2026-01-01T00:00:00Z", "2026-01-01T00:00:00Z", true)]
    [InlineData(false, "2026-01-01T00:00:00.0000000Z", "2026-01-01T00:00:00.0000000Z", true)]
    [InlineData(false, "2026-01-01T01:00:00.1234567+01:00", "2026-01-01T01:00:00.1234567+01:00", true)]
    [InlineData(false, "2026-01-01T00:00:00Z", "2026-01-01T01:00:00+01:00", false)]
    [InlineData(false, "2026-01-01T00:00:00Z", "2026-01-01T00:00:01Z", false)]
    [InlineData(false, "2026-01-01T00:00:00Z", "2026-01-01T00:00:00+00:00", true)]
    [InlineData(false, "2026-01-01T00:00:00+00:00", "2026-01-01T00:00:00Z", true)]
    [InlineData(false, "2026-01-01T00:00:00Z", "2026-01-01T00:00:00.0000000Z", true)]
    [InlineData(false, "2026-01-01T00:00:00.123Z", "2026-01-01T00:00:00.1230000Z", true)]
    [InlineData(true, "2026-01-01T00:00:00Z", "2026-01-01T00:00:00Z", true)]
    public async Task should_agree_with_the_reference_when_one_instant_is_spelled_one_way(bool clock, string authored, string expected, bool passes)
    {
        var plan = Compile(clock, authored, expected);
        var reference = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
        reference.Passed.ShouldEqual(passes);
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        result.Outcome.ShouldEqual(passes ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Failed);
    }

    static SemanticExecutionPlan Compile(bool clock, string authored, string expected)
    {
        var source = $$"""
            module Scheduling
              feature Windows
                slice StateChange Record
                  command Record
                    id String identifier
                    at DateTime
                    produces Recorded
                      for id
                      at = {{(clock ? "$context.occurred" : "at")}}
                  event Recorded
                    at DateTime
                  specification ExactText
            {{(clock ? $"        given clock \"{authored}\"" : string.Empty)}}
                    when Record
                      id = "one"
                      at = "{{authored}}"
                    then Recorded
                      for "one"
                      at = "{{authored}}"
                    then readmodel Current
                      id = "one"
                      at = "{{expected}}"
                slice StateView Current
                  readmodel Current
                    id String key
                    at DateTime
                  projection CurrentProjection => Current
                    no automap
                    from Recorded
                      id = $eventSourceId
                      at = at
            """;

        // Bind directly so deliberately mismatched expectations reach the reference comparer.
        var syntax = new ScreenplayCompiler().Parse(source);
        Assert.True(syntax.Success, string.Join(';', syntax.Diagnostics));
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("DateTimeProjection"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "app.play", source);
        var bound = new SemanticModelBinder().Bind("DateTimeProjection", syntax.Value!, SemanticDocumentSet.Create([document], catalog));
        Assert.True(bound.Success, string.Join(';', bound.Diagnostics));
        return SemanticExecutionPlan.Compile(bound.Value!.Model).Plan!;
    }
}
