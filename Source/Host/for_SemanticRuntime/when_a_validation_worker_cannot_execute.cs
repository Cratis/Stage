// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_a_validation_worker_cannot_execute : Specification
{
    [Theory]
    [InlineData("faulted", false)]
    [InlineData("faulted", true)]
    [InlineData("refused", false)]
    [InlineData("refused", true)]
    [InlineData("exceeded-time", false)]
    [InlineData("exceeded-time", true)]
    [InlineData("exceeded-memory", false)]
    [InlineData("exceeded-memory", true)]
    [InlineData("disabled", false)]
    [InlineData("disabled", true)]
    [InlineData("bypass", false)]
    [InlineData("bypass", true)]
    public async Task should_fail_closed_without_appending_even_when_the_caller_requests_a_bypass(string outcome, bool validateOnly)
    {
        var directory = Directory.CreateTempSubdirectory("stage-validation-failure-").FullName;
        try
        {
            var path = Path.Combine(directory, "Model.play");
            await File.WriteAllTextAsync(path, when_executing_command_validation_bodies.Model);
            var loaded = await SemanticModelLoader.LoadFromPathAsync(path);
            var requirement = Assert.Single(loaded.ImplementationRequirements);
            var code = DiagnosticCode(outcome);
            var worker = new FailedValidationWorker(new(outcome, Diagnostic: BodyDiagnostics.Format(code, "Validation could not execute.")));
            var manifest = new BodyUnitManifest("revision", [new(requirement.RequirementId, requirement.Role.ToString(), true) { BodySha256 = requirement.ContentHash }]);
            await using var bodies = outcome == "disabled" ? null : SemanticBodyExecution.From(loaded, manifest, () => worker);
            var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
            ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
            using var runtime = (IDisposable)SemanticRuntimeHosting.Create(loaded.Plan, appender, SemanticWorld.Empty, bodies);
            var payload = new Dictionary<string, JsonElement>
            {
                ["orderId"] = JsonSerializer.SerializeToElement("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                ["amount"] = JsonSerializer.SerializeToElement(1)
            };
            if (outcome == "bypass") payload["skipValidation"] = JsonSerializer.SerializeToElement(true);
            var result = await ((ISemanticRuntime)runtime).Execute(
                loaded.Plan.Commands.Values.Single(),
                payload,
                new ClaimsPrincipal(),
                new(DateTimeOffset.UnixEpoch, "caller", "Caller", "caller"),
                validateOnly);
            var details = outcome == "bypass" ? Assert.IsType<SemanticRejected>(result).Details : Assert.IsType<SemanticUnsupported>(result).Details;
            Assert.Contains(outcome == "bypass" ? "Unknown command property" : code, details, StringComparison.Ordinal);
            worker.Calls.ShouldEqual(outcome == "disabled" ? 0 : 1);
            await appender.DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    static string DiagnosticCode(string outcome) => outcome switch
    {
        "disabled" => "STAGE-IMPL-003",
        "exceeded-time" => BodyDiagnostics.ExceededTime,
        "exceeded-memory" => BodyDiagnostics.ExceededMemory,
        _ => BodyDiagnostics.WorkerFailed
    };

    sealed class FailedValidationWorker(BodyEvaluation failure) : IBodyWorkerClient
    {
        internal int Calls { get; private set; }

        public Task<BodyUnitManifest> CompileAsync(string modelPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<BodyEvaluation> EvaluateAsync(BodyEvaluationRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<BodyEvaluation> ValidateAsync(BodyEvaluationRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(failure);
        }
    }
}
