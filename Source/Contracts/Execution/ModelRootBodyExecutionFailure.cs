// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Contracts.Execution;

internal sealed class ModelRootBodyExecutionFailure(string diagnostic) : ISemanticBodyExecutionProvider
{
    public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new(new SemanticBodyExecutionResult(null, diagnostic));
    }

    public ValueTask<SemanticValidationBodyResult> ValidateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new(SemanticValidationBodyResult.Unsupported(diagnostic));
    }

    public ValueTask<SemanticReducerBodyResult> ReduceAsync(SemanticImplementationBody body, SemanticReducerContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new(SemanticReducerBodyResult.Unsupported(diagnostic));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
