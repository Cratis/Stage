// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_command_clock_conditions : Specification
{
    [Theory]
    [InlineData("WithClock")]
    [InlineData("MissingClock")]
    public async Task should_agree_with_the_reference_without_reading_an_implicit_clock(string name)
    {
        const string source = """
            module Scheduling
              feature Windows
                slice StateChange Keep
                  command Keep
                    id String identifier
                    at DateTime
                    validate
                      require at >= now
                    produces Kept
                      for id
                  command Unsupported
                    id String identifier
                    at DateTime
                    validate
                      at > now
                    produces Kept
                      for id
                  event Kept
                  specification WithClock
                    given clock "2026-01-02T00:00:00.0000000Z"
                    when Keep
                      id = "window"
                      at = "2026-01-02T00:00:00.0000001Z"
                    then Kept
                  specification MissingClock
                    when Keep
                      id = "window"
                      at = "2026-01-02T00:00:00.0000001Z"
                    then error "A command using a clock operand needs the instant of the command; the execution request supplies no occurrence."
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ClockConditions"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", source);
        var compilation = new SemanticModelCompiler().Compile("ClockConditions", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value.Model).Plan!;
        var specification = plan.Specifications.Values.Single(candidate => candidate.Name == name);
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        Assert.True(reference.Passed, string.Join(';', reference.Failures));
        var report = await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new() { Clock = new UnexpectedClock() });
        var result = report.Results.Single();
        Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Outcome}: {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
        result.ExecutionKind.ShouldEqual(reference.Execution.Kind.ToString());
        if (name == "MissingClock") result.Trace!.RejectionCategory.ShouldEqual(nameof(SemanticRejectionCategory.Contract));
    }

    sealed class UnexpectedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => throw new InvalidOperationException("The options clock must not supply a command occurrence.");
    }
}
