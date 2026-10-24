// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_rejecting_offsetless_datetime_inputs : Specification
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task should_validate_before_rules_even_for_skipped_comparisons(int scenario)
    {
        var plan = Compile(scenario);
        var reference = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
        var rejection = Assert.IsType<SemanticRejected>(reference.Execution);
        rejection.Category.ShouldEqual(SemanticRejectionCategory.Validation);
        rejection.ValidationFailures.Length.ShouldEqual(1);
        rejection.World.Facts.ShouldBeEmpty();
        var result = (await new SemanticSpecificationExecutor().Run(plan, new([]), new(), TestContext.Current.CancellationToken)).Results.Single();
        if (scenario == 5)
        {
            // Run admission still refuses attached code before any command executes (plan §4).
            result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
            return;
        }
        Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Outcome}: {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
        result.Trace.Rejection.ShouldEqual(rejection.Details);
        result.Trace.Facts.ShouldBeEmpty();
    }

    [Fact]
    public async Task should_authorize_before_rejecting_the_input()
    {
        var plan = Compile(0, authorize: true);
        var reference = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
        ((SemanticRejected)reference.Execution).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
        var result = (await new SemanticSpecificationExecutor().Run(plan, new([]), new(), TestContext.Current.CancellationToken)).Results.Single();
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
        result.Trace.Rejection.ShouldEqual("Caller is not authorized.");
        result.Trace.Facts.ShouldBeEmpty();
    }

    [Fact]
    public async Task should_keep_then_matching_ordinal()
    {
        const string source = """
            module Scheduling
              feature Windows
                slice StateChange Keep
                  command Keep
                    id String identifier
                    from DateTime
                    to DateTime
                    validate
                      require to == from
                    produces Kept
                      for id
                      at = to
                  event Kept
                    at DateTime
                  specification ExactSpelling
                    when Keep
                      id = "window"
                      from = "2026-01-01T00:00:00.0000000Z"
                      to = "2026-01-01T01:00:00.0000000+01:00"
                    then Kept
                      for "window"
                      at = "2026-01-01T00:00:00.0000000Z"
            """;

        // Bind directly like Screenplay's ExactSpelling vector: source preflight rejects intentionally wrong then-values.
        var syntax = new ScreenplayCompiler().Parse(source).Value;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("OrdinalDates"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "app.play", source);
        var bound = new SemanticModelBinder().Bind("OrdinalDates", syntax, SemanticDocumentSet.Create([document], catalog));
        Assert.True(bound.Success, string.Join(';', bound.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(bound.Value.Model).Plan;
        var reference = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
        reference.Passed.ShouldBeFalse();
        reference.Execution.ShouldBeOfExactType<SemanticAccepted>();
        var result = (await new SemanticSpecificationExecutor().Run(plan, new([]), new(), TestContext.Current.CancellationToken)).Results.Single();
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
        result.ExecutionKind.ShouldEqual("Accepted");
    }

    static SemanticExecutionPlan Compile(int scenario, bool authorize = false)
    {
        const string valid = "2026-01-01T00:00:00.0000000Z";
        const string missing = "2026-01-02T00:00:00.0000000";
        var clause = scenario switch
        {
            2 => "to >= \"2026-01-01T00:00:00Z\"",
            3 => "require id == \"window\" or to >= from",
            4 => "id == \"failure\" message \"Rule ran\"",
            _ => "require to >= from"
        };
        var source = $$"""
            policy Member
              require role "Member"
            module Scheduling
              feature Windows
                slice StateChange Keep
                  command Keep
                    id String identifier
                    from DateTime
                    to DateTime
            {{(authorize ? "        authorize Member" : "")}}
                    validate
                      {{clause}}
            {{(scenario == 5 ? "        validate\n          ```csharp\n          yield return \"Rule ran\";\n          ```" : "")}}
            {{(scenario == 4 ? "        produces when id == \"other\" and to >= from\n          Kept\n            for id" : "        produces Kept\n          for id")}}
                  event Kept
                  specification MissingOffset
            {{(authorize ? "        given caller\n          authenticated" : "")}}
                    when Keep
                      id = "window"
                      from = "{{(scenario == 1 ? missing : valid)}}"
                      to = "{{(scenario == 1 ? valid : missing)}}"
                    {{(authorize ? "then denied" : $"then error \"DateTime command property '{(scenario == 1 ? "from" : "to")}' requires an explicit offset or Z.\"")}}
            """;
        return Bind(source);
    }

    static SemanticExecutionPlan Bind(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("DateInputs"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "app.play", source);
        var compilation = new SemanticModelCompiler().Compile("DateInputs", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        return SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }
}
