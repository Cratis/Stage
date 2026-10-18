// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json;
using Cratis.DependencyInjection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Runtime;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Maintains the isolated semantic world for one Stage session.
/// </summary>
[IgnoreConvention]
internal sealed partial class SemanticRuntime : ISemanticRuntime, ISemanticRuntimeStatus, IDisposable
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

    internal ISemanticFactReader? FactReader { get; init; }

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

            var decision = Prepare(command, payload, principal, occurrence);

            return validateOnly ? decision(_world) : await Commit(decision, occurrence);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc/>
    public Task<SemanticExecutionResult> Query(SemanticKeyedQuery query, SemanticValue key, ClaimsPrincipal principal) => Query(query, key, principal, []);

    /// <inheritdoc/>
    public async Task<SemanticExecutionResult> Query(SemanticKeyedQuery query, SemanticValue key, ClaimsPrincipal principal, ImmutableArray<SemanticQueryFilterValue> filters)
    {
        await _gate.WaitAsync();
        try
        {
            InitializeWorld();
            if (FaultReason is not null)
            {
                return Faulted();
            }

            if (UnsupportedImplementation(query: query) is { } unsupported)
            {
                return unsupported;
            }

            var request = SemanticExecutionRequest.ForQueries([new(query.Id, key) { Filters = filters }]) with { Caller = SemanticCallers.From(principal) };
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

    SemanticUnsupported Faulted() => new(_world, SemanticExecutionCapability.Unknown, FaultReason!);

    SemanticUnsupported? UnsupportedImplementation(SemanticCommand? command = null, SemanticKeyedQuery? query = null)
    {
        // Match Host model admission even for runtimes created directly, before evaluating or settling any command.
        if (Plan.Commands.Values.SelectMany(SemanticVersionFeatures.UnsupportedReadRequirements).FirstOrDefault() is { } feature)
        {
            return new(_world, SemanticExecutionCapability.Command, $"{feature.Code}: {feature.Message}");
        }

        if (command is not null && SemanticRunAdmission.CommandValidationRefusal(Plan, command) is { } validationRefusal)
        {
            return new(_world, SemanticExecutionCapability.Command, validationRefusal);
        }

        var refusal = ArtifactRefusal(command, query);
        if (refusal is null) return null;
        var capability = Enum.TryParse<SemanticExecutionCapability>(refusal.Capability.ToString(), out var parsed) ? parsed : SemanticExecutionCapability.Unknown;

        return new(_world, capability, refusal.Details);
    }

    SemanticUnsupportedCapability? ArtifactRefusal(SemanticCommand? command, SemanticKeyedQuery? query)
    {
        var artifact = command?.Id ?? query?.Id;
        var refusal = artifact is { } id ? SemanticPlanAdmission.Check(Plan, id) : null;
        refusal ??= command is null ? null : SemanticImplementationReach.ForCommand(Plan, command);

        return refusal ?? (query is null ? null : SemanticImplementationReach.ForQuery(Plan, query));
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

    SemanticExecutionResult Settle(SemanticAccepted accepted, SemanticCommandOccurrence occurrence, out bool invokedCommands)
    {
        var evaluator = new ReactionEvaluator(this, _evaluator, occurrence);
        var settlement = new SemanticReactionSettlement(evaluator);
        var result = settlement.Settle(Plan, accepted.World, accepted.Facts, occurrence.Occurred);
        invokedCommands = evaluator.InvokedCommands;

        return result is SemanticAccepted settled
            ? accepted with { World = settled.World, Facts = accepted.Facts.AddRange(settled.Facts) }
            : result with { World = _world };
    }

    Func<SemanticWorld, SemanticExecutionResult> Prepare(SemanticCommand command, IReadOnlyDictionary<string, JsonElement> payload, ClaimsPrincipal principal, SemanticCommandOccurrence occurrence)
    {
        var unknown = payload.Keys.FirstOrDefault(key => command.Properties.Where(property => !property.IsGenerated).All(property => !string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase)));
        try
        {
            var values = SemanticJsonValues.Bind(command.Properties.Where(property => !property.IsGenerated), payload, Plan.Model);
            var request = SemanticExecutionRequest.Create(command.Id, values, []) with
            {
                Caller = SemanticCallers.From(principal),
                Occurrence = occurrence,
                GeneratedValues = SemanticGeneratedValues.For(command, Plan.Model, occurrence.Occurred)
            };
            var decision = _evaluator.Decide(Plan, _world, request);

            return world =>
            {
                var result = decision.Commit(world);
                if (unknown is not null && result is not SemanticRejected { Category: SemanticRejectionCategory.Unauthorized })
                {
                    return new SemanticRejected(world, SemanticRejectionCategory.Contract, null, $"Unknown command property '{unknown}'.");
                }

                return SemanticGeneratedValues.ReportDefect(command, Plan.Model.Application, result);
            };
        }
        catch (UnsupportedSemanticValue exception)
        {
            // Authorization is the evaluator's first decision, even for payloads the JSON binder cannot represent.
            var emptyValues = command.Properties.Where(property => !property.IsGenerated).Select(property => new SemanticPropertyValue(property.Id, SemanticValue.Null)).ToImmutableArray();
            var authorization = _evaluator.Execute(Plan, _world, SemanticExecutionRequest.Create(command.Id, emptyValues, []) with
            {
                Caller = SemanticCallers.From(principal),
                Occurrence = occurrence,
                GeneratedValues = SemanticGeneratedValues.For(command, Plan.Model, occurrence.Occurred)
            });
            return world => authorization is SemanticRejected { Category: SemanticRejectionCategory.Unauthorized } or SemanticUnsupported
                ? authorization with { World = world }
                : new SemanticRejected(world, SemanticRejectionCategory.Contract, null, exception.Message);
        }
    }

    sealed class ReactionEvaluator(SemanticRuntime runtime, ISemanticEvaluator evaluator, SemanticCommandOccurrence occurrence) : ISemanticEvaluator
    {
        internal bool InvokedCommands { get; private set; }

        public SemanticExecutionResult Execute(SemanticExecutionPlan plan, SemanticWorld world, SemanticExecutionRequest request)
        {
            InvokedCommands = true;
            var command = plan.Commands.GetValueOrDefault(request.Command);
            if (runtime.UnsupportedImplementation(command) is { } unsupported)
            {
                return unsupported with { World = world };
            }

            if (command is not null && request.GeneratedValues.IsDefaultOrEmpty)
            {
                request = request with { GeneratedValues = SemanticGeneratedValues.For(command, plan.Model, request.Occurrence?.Occurred ?? occurrence.Occurred) };
            }

            return SemanticGeneratedValues.ReportDefect(command, plan.Model.Application, evaluator.Execute(plan, world, request));
        }
    }
}
