// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json;
using Cratis.DependencyInjection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Maintains the isolated semantic world for one Stage session.
/// </summary>
[IgnoreConvention]
internal sealed class SemanticRuntime : ISemanticRuntime, IDisposable
{
    readonly IAppendSemanticFacts _appender;
    readonly SemaphoreSlim _gate = new(1, 1);
    readonly SemanticEvaluator _evaluator = new();
    SemanticWorld _world = SemanticWorld.Empty;

    internal SemanticRuntime(SemanticExecutionPlan plan, IAppendSemanticFacts appender)
    {
        Plan = plan;
        _appender = appender;
    }

    /// <inheritdoc/>
    public SemanticExecutionPlan Plan { get; }

    /// <inheritdoc/>
    public void Dispose() => _gate.Dispose();

    /// <inheritdoc/>
    public async Task<SemanticExecutionResult> Execute(
        SemanticCommand command,
        IReadOnlyDictionary<string, JsonElement> payload,
        ClaimsPrincipal principal,
        SemanticCommandOccurrence occurrence,
        bool validateOnly)
    {
        await _gate.WaitAsync();
        try
        {
            var result = Evaluate(command, payload, principal, occurrence);
            if (!validateOnly && result is SemanticAccepted accepted)
            {
                await _appender.Append(accepted.Facts, occurrence);
                _world = accepted.World;
            }

            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<SemanticExecutionResult> Query(SemanticKeyedQuery query, SemanticValue key, ClaimsPrincipal principal)
    {
        await _gate.WaitAsync();
        try
        {
            var request = SemanticExecutionRequest.ForQueries([new(query.Id, key)]) with { Caller = SemanticCallers.From(principal) };
            return _evaluator.Execute(Plan, _world, request);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ImmutableArray<SemanticReadModelInstance>> ReadModels(SemanticId model)
    {
        await _gate.WaitAsync();
        try
        {
            return [.. _world.ReadModels.Where(instance => instance.ReadModel == model)];
        }
        finally
        {
            _gate.Release();
        }
    }

    SemanticExecutionResult Evaluate(SemanticCommand command, IReadOnlyDictionary<string, JsonElement> payload, ClaimsPrincipal principal, SemanticCommandOccurrence occurrence)
    {
        var unknown = payload.Keys.FirstOrDefault(key => command.Properties.All(property => property.Name != key));
        try
        {
            var values = SemanticJsonValues.Bind(command.Properties, payload, Plan.Model);
            var request = SemanticExecutionRequest.Create(command.Id, values, []) with
            {
                Caller = SemanticCallers.From(principal),
                Occurrence = occurrence
            };
            var result = _evaluator.Execute(Plan, _world, request);
            if (unknown is not null && result is not SemanticRejected { Category: SemanticRejectionCategory.Unauthorized })
            {
                return new SemanticRejected(_world, SemanticRejectionCategory.Contract, null, $"Unknown command property '{unknown}'.");
            }

            return result;
        }
        catch (UnsupportedSemanticValue exception)
        {
            // Authorization is the evaluator's first decision, even for payloads the JSON binder cannot represent.
            var emptyValues = command.Properties.Select(property => new SemanticPropertyValue(property.Id, SemanticValue.Null)).ToImmutableArray();
            var authorization = _evaluator.Execute(Plan, _world, SemanticExecutionRequest.Create(command.Id, emptyValues, []) with
            {
                Caller = SemanticCallers.From(principal),
                Occurrence = occurrence
            });
            return authorization is SemanticRejected { Category: SemanticRejectionCategory.Unauthorized }
                ? authorization
                : new SemanticRejected(_world, SemanticRejectionCategory.Contract, null, exception.Message);
        }
    }
}
