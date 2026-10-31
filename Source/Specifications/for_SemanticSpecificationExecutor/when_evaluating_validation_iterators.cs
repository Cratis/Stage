// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_evaluating_validation_iterators : Specification
{
    [Theory]
    [InlineData(false, false, "Accepted", SemanticSpecificationOutcome.Passed)]
    [InlineData(false, true, "Rejected", SemanticSpecificationOutcome.Passed)]
    [InlineData(true, false, "Accepted", SemanticSpecificationOutcome.Passed)]
    [InlineData(true, true, "Rejected", SemanticSpecificationOutcome.Passed)]
    [InlineData(false, true, "Rejected", SemanticSpecificationOutcome.Unsupported)]
    [InlineData(true, true, "Rejected", SemanticSpecificationOutcome.Unsupported)]
    public async Task should_route_iterators_without_treating_worker_failure_as_a_validation_rejection(bool concept, bool reject, string selected, SemanticSpecificationOutcome outcome)
    {
        var loaded = Compile(concept);
        var requirement = Assert.Single(loaded.ImplementationRequirements);
        var manifest = new BodyUnitManifest("revision", [new(requirement.RequirementId, requirement.Role.ToString(), true) { BodySha256 = requirement.ContentHash }]);
        var verdict = outcome == SemanticSpecificationOutcome.Unsupported ? new BodyEvaluation("faulted", Diagnostic: BodyDiagnostics.Format(BodyDiagnostics.WorkerFailed, "Worker failed.")) : new(reject ? "unsatisfied" : "satisfied", Messages: reject ? ["Amount refused"] : []);
        var worker = new ValidationWorker(verdict);
        await using var bodies = SemanticBodyExecution.From(loaded, manifest, () => worker);
        var specification = loaded.Plan.Specifications.Values.Single(candidate => candidate.Name == selected);
        var facts = -1;
        var executor = new SemanticSpecificationExecutor { ObserveWorldFacts = (_, count) => facts = count };
        var report = await executor.Run(loaded.Plan, new([specification.Id]), new() { BodyExecution = bodies });
        var result = Assert.Single(report.Results);
        Assert.True(result.Outcome == outcome, result.Unsupported?.Details ?? string.Join(';', result.Failures));
        facts.ShouldEqual(reject ? 0 : 1);
        worker.ValidationCalls.ShouldEqual(1);
        worker.EvaluationCalls.ShouldEqual(0);
    }

    sealed class ValidationWorker(BodyEvaluation result) : IBodyWorkerClient
    {
        internal int ValidationCalls { get; private set; }
        internal int EvaluationCalls { get; private set; }

        public Task<BodyUnitManifest> CompileAsync(string modelPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<BodyEvaluation> EvaluateAsync(BodyEvaluationRequest request, CancellationToken cancellationToken = default)
        {
            EvaluationCalls++;
            return Task.FromResult(new BodyEvaluation("refused"));
        }

        public Task<BodyEvaluation> ValidateAsync(BodyEvaluationRequest request, CancellationToken cancellationToken = default)
        {
            ValidationCalls++;
            return Task.FromResult(result);
        }
    }

    static LoadedSemanticModel Compile(bool concept)
    {
        var validation = concept ? "" : """
                    validate csharp
                      ```csharp
                      if (context.Artifact.Amount <= 0) yield return "Amount refused";
                      ```
            """;
        var source = $$"""
            concept Amount : Int
              {{(concept ? "validate csharp\n    ```csharp\n    if (context.Value <= 0) yield return \"Amount refused\";\n    ```" : "")}}
            module M
              feature F
                slice StateChange Add
                  command Add
                    id String identifier
                    amount Amount
            {{validation}}
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
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Validations"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("validations"), "validations", "Validations.play", source);
        var result = new SemanticModelCompiler().Compile("Validations", SemanticDocumentSet.Create([document], catalog));
        Assert.True(result.Success, string.Join(';', result.Diagnostics));
        return new(result.Value.Model, SemanticExecutionPlan.Compile(result.Value.Model).Plan)
        {
            ImplementationRequirements = result.ImplementationRequirements,
            ImplementationContents = SemanticImplementationBodies.Resolve(result.Value.Documents, result.ImplementationRequirements)
        };
    }
}
