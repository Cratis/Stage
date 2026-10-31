// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_a_reducer_worker_cannot_execute : Specification
{
    [Theory]
    [InlineData("exceeded-time")]
    [InlineData("exceeded-memory")]
    [InlineData("faulted")]
    [InlineData("refused")]
    public async Task should_be_unsupported_and_leave_the_world_unchanged(string outcome)
    {
        var directory = Directory.CreateTempSubdirectory("stage-reducer-failure-").FullName;
        try
        {
            var path = Path.Combine(directory, "Model.play");
            await File.WriteAllTextAsync(path, when_executing_reducer_bodies.Model);
            var loaded = await SemanticModelLoader.LoadFromPathAsync(path);
            var worker = new FailedReducerWorker(new(outcome, Diagnostic: BodyDiagnostics.Format(BodyDiagnostics.WorkerFailed, "Reducer could not execute.")));
            var manifest = new BodyUnitManifest("revision", [.. loaded.ImplementationRequirements.Select(requirement => new BodyUnitEntry(requirement.RequirementId, requirement.Role.ToString(), true) { BodySha256 = requirement.ContentHash })]);
            await using var bodies = SemanticBodyExecution.From(loaded, manifest, () => worker);
            var (runtime, appender) = when_executing_reducer_bodies.Runtime(loaded, bodies);
            using var lifetime = (IDisposable)runtime;
            var result = await when_executing_reducer_bodies.Execute(runtime, loaded, "ScoreTalk", ("score", 7));
            Assert.Contains(BodyDiagnostics.WorkerFailed, Assert.IsType<SemanticUnsupported>(result).Details, StringComparison.Ordinal);
            Assert.True(worker.Calls > 0);
            await appender.DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
            Assert.Empty(await runtime.ReadModels(loaded.Plan.ReadModels.Values.Single().Id));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    sealed class FailedReducerWorker(BodyEvaluation failure) : IBodyWorkerClient
    {
        internal int Calls { get; private set; }

        public Task<BodyUnitManifest> CompileAsync(string modelPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<BodyEvaluation> EvaluateAsync(BodyEvaluationRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<BodyEvaluation> ValidateAsync(BodyEvaluationRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<BodyEvaluation> ReduceAsync(BodyEvaluationRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(failure);
        }
    }
}
