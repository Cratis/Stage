// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json;
using Cratis.DependencyInjection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Runtime;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Maintains the isolated semantic world for one Stage session.
/// </summary>
[IgnoreConvention]
internal sealed class SemanticRuntime : ISemanticRuntime, ISemanticRuntimeStatus, IDisposable
{
    static readonly TimeSpan _appendTimeout = TimeSpan.FromSeconds(15);
    readonly IAppendSemanticFacts _appender;
    readonly SemaphoreSlim _gate = new(1, 1);
    readonly SemanticEvaluator _evaluator = new();
    SemanticWorld _world = SemanticWorld.Empty;
    Func<SemanticWorld>? _worldProvider;
    ulong _knownTail;

    internal SemanticRuntime(SemanticExecutionPlan plan, IAppendSemanticFacts appender, SemanticWorld? world = null)
    {
        Plan = plan;
        _appender = appender;
        _world = world ?? SemanticWorld.Empty;

        // Rebuild admits only a contiguous history from sequence zero through the captured tail.
        _knownTail = _world.Facts.Length == 0 ? ulong.MaxValue : (ulong)_world.Facts.Length - 1;
    }

    internal SemanticRuntime(SemanticExecutionPlan plan, IAppendSemanticFacts appender, Func<SemanticWorld> world) : this(plan, appender)
    {
        _worldProvider = world;
    }

    /// <inheritdoc/>
    public SemanticExecutionPlan Plan { get; }

    /// <inheritdoc/>
    public string? FaultReason { get; private set; }

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
            InitializeWorld();
            if (FaultReason is not null)
            {
                return Faulted();
            }

            if (UnsupportedImplementation(command) is { } unsupported)
            {
                return unsupported;
            }

            var expectedTail = _knownTail;
            var result = Evaluate(command, payload, principal, occurrence);
            if (!validateOnly && result is SemanticAccepted accepted)
            {
                if (_appender is not ISemanticFactTail tail)
                {
                    FaultReason = "The fact appender cannot verify Chronicle's event-log tail.";
                    return Faulted();
                }

                ulong before;
                try
                {
                    before = await tail.Tail().WaitAsync(_appendTimeout);
                }
                catch (Exception exception)
                {
                    FaultReason = $"Cannot read the event-log tail before append: {exception.Message}";
                    return Faulted();
                }

                if (before != expectedTail)
                {
                    FaultReason = $"The event-log tail changed outside this session ({expectedTail} -> {before}).";
                    return Faulted();
                }

                try
                {
                    if (accepted.Facts.Length > 0)
                    {
                        await _appender.Append(accepted.Facts, occurrence, expectedTail).WaitAsync(_appendTimeout);
                    }
                    else
                    {
                        // No append reaches Chronicle to enforce a scope; do not report a stale world as current.
                        var after = await tail.Tail().WaitAsync(_appendTimeout);
                        if (after != before)
                        {
                            FaultReason = $"The event-log tail changed outside this session ({before} -> {after}).";
                            return Faulted();
                        }
                    }
                }
                catch (SemanticFactTailChanged exception)
                {
                    FaultReason = $"The event-log tail changed outside this session ({before} -> {exception.ActualTail}).";
                    return Faulted();
                }
                catch (Exception exception)
                {
                    try
                    {
                        var after = await tail.Tail().WaitAsync(_appendTimeout);
                        if (after != before || exception is not ProducedEventConstraintRejected)
                        {
                            FaultReason = $"Append outcome is unknown (tail {before} -> {after}): {exception.Message}";
                        }
                    }
                    catch (Exception tailFailure)
                    {
                        FaultReason = $"Append outcome is unknown; the event-log tail cannot be read: {tailFailure.Message}";
                    }

                    if (FaultReason is not null)
                    {
                        return Faulted();
                    }

                    throw;
                }

                _world = accepted.World;
                if (accepted.Facts.Length > 0)
                {
                    _knownTail = before == ulong.MaxValue ? (ulong)accepted.Facts.Length - 1 : before + (ulong)accepted.Facts.Length;
                }
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
            InitializeWorld();
            if (FaultReason is not null)
            {
                return Faulted();
            }

            if (UnsupportedImplementation() is { } unsupported)
            {
                return unsupported;
            }

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
            InitializeWorld();
            return FaultReason is null ? [.. _world.ReadModels.Where(instance => instance.ReadModel == model)] : [];
        }
        finally
        {
            _gate.Release();
        }
    }

    static IEnumerable<SemanticSlice> AllSlices(SemanticFeature feature) =>
        feature.Slices.Concat(feature.Features.SelectMany(AllSlices));

    static bool OpaqueRule(SemanticValidationRuleKind kind) =>
        kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation;

    SemanticUnsupported Faulted() => new(_world, SemanticExecutionCapability.Unknown, FaultReason!);

    SemanticUnsupported? UnsupportedImplementation(SemanticCommand? command = null)
    {
        var slices = Plan.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices);
        if (slices.SelectMany(slice => slice.Reducers).Any())
        {
            return new(_world, SemanticExecutionCapability.Projection, "Reducer implementation bodies cannot be executed by Stage.");
        }

        if (Plan.Model.Application.Concepts.Any(concept => concept.Validations.Any(rule => OpaqueRule(rule.Kind))) ||
            (command is not null && (!command.CodeValidations.IsEmpty || command.Validations.Any(rule => OpaqueRule(rule.Kind)))))
        {
            return new(_world, SemanticExecutionCapability.Command, "Validation implementation bodies cannot be executed by Stage.");
        }

        return null;
    }

    void InitializeWorld()
    {
        if (_worldProvider is not { } provider)
        {
            return;
        }

        var world = provider();
        _world = world;
        _knownTail = world.Facts.Length == 0 ? ulong.MaxValue : (ulong)world.Facts.Length - 1;
        _worldProvider = null;
    }

    SemanticExecutionResult Evaluate(SemanticCommand command, IReadOnlyDictionary<string, JsonElement> payload, ClaimsPrincipal principal, SemanticCommandOccurrence occurrence)
    {
        var unknown = payload.Keys.FirstOrDefault(key => command.Properties.All(property => !string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase)));
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
            return authorization is SemanticRejected { Category: SemanticRejectionCategory.Unauthorized } or SemanticUnsupported
                ? authorization
                : new SemanticRejected(_world, SemanticRejectionCategory.Contract, null, exception.Message);
        }
    }
}
