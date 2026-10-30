// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Execution;

namespace Cratis.Stage.Specifications.Commands;

internal sealed class SemanticBodyRuleSession : IAsyncDisposable
{
    readonly Dictionary<string, SemanticImplementationBody> _bodies;
    readonly Lazy<ISemanticBodyExecutionProvider> _provider;

    internal SemanticBodyRuleSession(SemanticApplication application, SemanticBodyExecution execution)
    {
        _bodies = execution.Bodies.ToDictionary(body => body.Requirement.RequirementId, StringComparer.Ordinal);
        _provider = new(() => execution.CreateAsync(application, _bodies, CancellationToken.None).AsTask().GetAwaiter().GetResult());
    }

    public async ValueTask DisposeAsync()
    {
        if (_provider.IsValueCreated) await _provider.Value.DisposeAsync().ConfigureAwait(false);
    }

    internal SemanticRuleCheck Check(SemanticValidationRule rule, SemanticRuleContext context, CancellationToken cancellationToken)
    {
        if (rule.RequirementId is null || !_bodies.TryGetValue(rule.RequirementId, out var body)) return Unsupported(rule, "Named rule body is unavailable.");
        try
        {
            var result = _provider.Value.EvaluateAsync(body, context, cancellationToken).AsTask().GetAwaiter().GetResult();
            return result.Satisfied switch
            {
                true => new(SemanticRuleStatus.Satisfied, rule, string.Empty),
                false => new(SemanticRuleStatus.Failed, rule, rule.Message ?? $"Named rule '{rule.Name}' was not satisfied."),
                _ => new(SemanticRuleStatus.NotEvaluable, rule, result.Diagnostic ?? BodyDiagnostics.Format(BodyDiagnostics.WorkerFailed, "Body execution returned no answer."))
            };
        }
        catch (Exception exception)
        {
            return Unsupported(rule, exception.Message);
        }
    }

    static SemanticRuleCheck Unsupported(SemanticValidationRule rule, string message) =>
        new(SemanticRuleStatus.NotEvaluable, rule, BodyDiagnostics.Format(BodyDiagnostics.WorkerFailed, message));
}
