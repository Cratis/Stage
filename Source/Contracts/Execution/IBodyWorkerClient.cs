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
}
