// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using NSubstitute;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public partial class when_evaluating_worker_rules : Specification
{
    [Theory]
    [InlineData("satisfied", "Accepted", SemanticSpecificationOutcome.Passed, false)]
    [InlineData("unsatisfied", "Rejected", SemanticSpecificationOutcome.Passed, false)]
    [InlineData("exceeded-time", "Rejected", SemanticSpecificationOutcome.Unsupported, false)]
    [InlineData("exceeded-memory", "Rejected", SemanticSpecificationOutcome.Unsupported, false)]
    [InlineData("faulted", "Rejected", SemanticSpecificationOutcome.Unsupported, false)]
    [InlineData("satisfied", "Accepted", SemanticSpecificationOutcome.Passed, true)]
    [InlineData("unsatisfied", "Rejected", SemanticSpecificationOutcome.Passed, true)]
    [InlineData("faulted", "Rejected", SemanticSpecificationOutcome.Unsupported, true)]
    public async Task should_check_the_body_in_a_disposed_per_specification_session(string verdict, string specification, SemanticSpecificationOutcome expected, bool concept)
    {
        var compilation = Compile(concept);
        var plan = SemanticExecutionPlan.Compile(compilation.Model).Plan;
        var workers = new List<IBodyWorkerClient>();
        var bodies = Execution(compilation, verdict, workers);
        var selected = plan.Specifications.Values.Single(candidate => candidate.Name == specification).Id;
        Assert.Null(SemanticSpecificationAdmission.Check(plan, plan.Specifications[selected], bodies));
        var facts = -1;
        var executor = new SemanticSpecificationExecutor { ObserveWorldFacts = (_, count) => facts = count };
        var result = (await executor.Run(plan, new([selected]), new() { BodyExecution = bodies })).Results.Single();
        Assert.True(result.Outcome == expected, result.Unsupported?.Details ?? string.Join(';', result.Failures));
        workers.Count.ShouldEqual(1);
        await workers.Single().Received(1).EvaluateAsync(Arg.Any<BodyEvaluationRequest>(), Arg.Any<CancellationToken>());
        await ((IAsyncDisposable)workers.Single()).DidNotReceive().DisposeAsync();
        await bodies.DisposeAsync();
        await ((IAsyncDisposable)workers.Single()).Received(1).DisposeAsync();
        facts.ShouldEqual(verdict == "satisfied" ? 1 : 0);
        if (expected == SemanticSpecificationOutcome.Unsupported) result.Unsupported.Details.Contains("STAGE-", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    public async Task should_share_the_application_worker_between_specifications_and_repeat_runs()
    {
        var compilation = Compile();
        var plan = SemanticExecutionPlan.Compile(compilation.Model).Plan;
        var workers = new List<IBodyWorkerClient>();
        var bodies = Execution(compilation, "satisfied", workers);
        var executor = new SemanticSpecificationExecutor();
        for (var run = 0; run < 2; run++) await executor.Run(plan, new([]), new() { BodyExecution = bodies });
        workers.Count.ShouldEqual(1);
        await workers.Single().Received(4).EvaluateAsync(Arg.Any<BodyEvaluationRequest>(), Arg.Any<CancellationToken>());
        await ((IAsyncDisposable)workers.Single()).DidNotReceive().DisposeAsync();
        await bodies.DisposeAsync();
        await ((IAsyncDisposable)workers.Single()).Received(1).DisposeAsync();
    }

    static SemanticBodyExecution Execution(LoadedSemanticModel loaded, string verdict, List<IBodyWorkerClient> workers)
    {
        var body = loaded.ImplementationRequirements.Single();
        var manifest = new BodyUnitManifest("revision", [new(body.RequirementId, "RulePredicate", true) { BodySha256 = body.ContentHash }]);
        return SemanticBodyExecution.From(loaded, manifest, () =>
        {
            var worker = Substitute.For<IBodyWorkerClient, IAsyncDisposable>();
            worker.EvaluateAsync(Arg.Any<BodyEvaluationRequest>(), Arg.Any<CancellationToken>()).Returns(new BodyEvaluation(verdict));
            workers.Add(worker);
            return worker;
        });
    }

    static LoadedSemanticModel Compile(bool concept = false)
    {
        const string source = """
            module M
              feature F
                slice StateChange Add
                  command Add
                    id String identifier
                    amount Int
                    validate
                      amount rule Positive message "Amount refused"
                        ```csharp
                        return context.Value > 0;
                        ```
                    produces Added
                      for id
                      amount = amount
                  event Added
                    amount Int
                  specification Accepted
                    when Add
                      id = "one"
                      amount = 5
                    then Added
                      amount = 5
                  specification Rejected
                    when Add
                      id = "one"
                      amount = -5
                    then error "Amount refused"
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Rules"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("rules"), "rules", "Rules.play", concept ? ConceptModel : source);
        var result = new SemanticModelCompiler().Compile("Rules", SemanticDocumentSet.Create([document], catalog));
        Assert.True(result.Success, string.Join(';', result.Diagnostics));
        var model = result.Value.Model;
        return new(model, SemanticExecutionPlan.Compile(model).Plan)
        {
            ImplementationRequirements = result.ImplementationRequirements,
            ImplementationContents = SemanticImplementationBodies.Resolve(result.Value.Documents, result.ImplementationRequirements)
        };
    }

    const string ConceptModel = """
        concept Amount : Int
          validate
            rule Positive message "Amount refused"
              ```csharp
              return context.Value > 0;
              ```
        module M
          feature F
            slice StateChange Add
              command Add
                id String identifier
                amount Amount
                produces Added
                  for id
                  amount = amount
              event Added
                amount Amount
              specification Accepted
                when Add
                  id = "one"
                  amount = 5
                then Added
                  amount = 5
              specification Rejected
                when Add
                  id = "one"
                  amount = -5
                then error "Amount refused"
        """;
}
