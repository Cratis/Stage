// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using NSubstitute;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public partial class when_evaluating_worker_rules
{
    [Theory]
    [InlineData("Date", "satisfied", SemanticSpecificationOutcome.Passed)]
    [InlineData("Date", "unsatisfied", SemanticSpecificationOutcome.Passed)]
    [InlineData("DateTime", "satisfied", SemanticSpecificationOutcome.Passed)]
    [InlineData("DateTime", "unsatisfied", SemanticSpecificationOutcome.Passed)]
    [InlineData("DateTime", "faulted", SemanticSpecificationOutcome.Unsupported)]
    public async Task should_dispatch_opaque_date_rules_to_the_worker_even_with_a_chronological_requirement(string primitive, string verdict, SemanticSpecificationOutcome expected)
    {
        var loaded = CompileDates(primitive, verdict);
        var workers = new List<IBodyWorkerClient>();
        var bodies = Execution(loaded, verdict, workers);
        var result = (await new SemanticSpecificationExecutor().Run(loaded.Plan, new([]), new() { BodyExecution = bodies })).Results.Single();
        Assert.True(result.Outcome == expected, result.Unsupported?.Details ?? string.Join(';', result.Failures));
        await Assert.Single(workers).Received(1).EvaluateAsync(Arg.Any<BodyEvaluationRequest>(), Arg.Any<CancellationToken>());
        await ((IAsyncDisposable)workers.Single()).DidNotReceive().DisposeAsync();
        await bodies.DisposeAsync();
        await ((IAsyncDisposable)workers.Single()).Received(1).DisposeAsync();
        if (verdict == "unsatisfied") result.ExecutionKind.ShouldEqual("Rejected");
    }

    static LoadedSemanticModel CompileDates(string primitive, string verdict)
    {
        var from = primitive == "Date" ? "2026-01-01" : "2026-01-01T00:00:00Z";
        var to = primitive == "Date" ? "2026-01-02" : "2026-01-02T00:00:00Z";
        var then = verdict == "satisfied" ? $"then Kept\n          from = \"{from}\"" : "then error \"Rule ran\"";
        var source = $$"""
            module M
              feature F
                slice StateChange Keep
                  command Keep
                    id String identifier
                    from {{primitive}}
                    to {{primitive}}
                    validate
                      require to >= from
                      from rule Calm message "Rule ran"
                        ```csharp
                        return true;
                        ```
                    produces Kept
                      for id
                      from = from
                  event Kept
                    from {{primitive}}
                  specification Checked
                    when Keep
                      id = "window"
                      from = "{{from}}"
                      to = "{{to}}"
                    {{then}}
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Dates"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("dates"), "dates", "Dates.play", source);
        var result = new SemanticModelCompiler().Compile("Dates", SemanticDocumentSet.Create([document], catalog));
        Assert.True(result.Success, string.Join(';', result.Diagnostics));
        var model = result.Value.Model;
        return new(model, SemanticExecutionPlan.Compile(model).Plan)
        {
            ImplementationRequirements = result.ImplementationRequirements,
            ImplementationContents = SemanticImplementationBodies.Resolve(result.Value.Documents, result.ImplementationRequirements)
        };
    }
}
