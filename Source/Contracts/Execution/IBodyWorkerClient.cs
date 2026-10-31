// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Execution;

/// <summary>The target-neutral client for isolated model-body compilation and evaluation.</summary>
public interface IBodyWorkerClient
{
    /// <summary>Compiles the resolved body units for one model in a short-lived process.</summary>
    /// <param name="modelPath">The model source or serialized semantic model.</param>
    /// <param name="cancellationToken">The caller's cancellation.</param>
    /// <returns>The compilation and admission manifest.</returns>
    Task<BodyUnitManifest> CompileAsync(string modelPath, CancellationToken cancellationToken = default);

    /// <summary>Evaluates one admitted body in the persistent execute process.</summary>
    /// <param name="request">The requirement and portable member values.</param>
    /// <param name="cancellationToken">The caller's cancellation.</param>
    /// <returns>The bounded evaluation verdict.</returns>
    Task<BodyEvaluation> EvaluateAsync(BodyEvaluationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Executes an admitted validation iterator.</summary>
    /// <param name="request">The requirement and portable context members.</param>
    /// <param name="cancellationToken">The caller's cancellation.</param>
    /// <returns>The bounded validation messages or an explicit refusal.</returns>
    Task<BodyEvaluation> ValidateAsync(BodyEvaluationRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new BodyEvaluation("refused", Diagnostic: "Code validations are unavailable in this worker client (Stage#891 slice S2)."));

    /// <summary>Executes an admitted reducer transition.</summary>
    /// <param name="request">The current, event, key, tenant, occurred and sequenceNumber members.</param>
    /// <param name="cancellationToken">The caller's cancellation.</param>
    /// <returns>The next state, deletion or an explicit refusal.</returns>
    Task<BodyEvaluation> ReduceAsync(BodyEvaluationRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new BodyEvaluation("refused", Diagnostic: BodyDiagnostics.NotExecuted(Cratis.Screenplay.Semantics.SemanticImplementationRole.ReducerTransition)));
}
