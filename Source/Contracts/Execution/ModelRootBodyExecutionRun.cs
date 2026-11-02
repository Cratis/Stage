// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Contracts.Execution;

internal sealed class ModelRootBodyExecutionRun(ISemanticBodyExecutionProvider provider, Func<ValueTask> release) : ISemanticBodyExecutionProvider
{
    int _disposed;

    public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) => provider.EvaluateAsync(body, context, cancellationToken);
    public ValueTask<SemanticValidationBodyResult> ValidateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) => provider.ValidateAsync(body, context, cancellationToken);
    public ValueTask<SemanticReducerBodyResult> ReduceAsync(SemanticImplementationBody body, SemanticReducerContext context, CancellationToken cancellationToken) => provider.ReduceAsync(body, context, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        try { await provider.DisposeAsync().ConfigureAwait(false); }
        finally { await release().ConfigureAwait(false); }
    }
}
