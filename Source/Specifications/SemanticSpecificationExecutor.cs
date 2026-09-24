// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Events;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.Admission;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.Comparison;
using Cratis.Stage.Specifications.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Stage.Specifications;

/// <summary>
/// Executes the admitted command subset against an isolated in-memory event log.
/// </summary>
public sealed class SemanticSpecificationExecutor : ISemanticSpecificationExecutor
{
    static readonly SemaphoreSlim _processGate = new(1, 1);
    static readonly ConcurrentDictionary<SemanticRevision, SemanticRuntimeTypes> _types = [];

    /// <inheritdoc/>
    public async Task<SemanticSpecificationRunReport> Run(SemanticExecutionPlan plan, SemanticSpecificationSelection selection, SemanticSpecificationRunOptions options, CancellationToken cancellationToken = default)
    {
        var results = new List<SemanticSpecificationRunRecord>();
        var selected = Select(plan.Model.Application, selection).ToArray();
        foreach (var scope in selection.Scopes.IsDefault ? [] : selection.Scopes)
        {
            if (!ContainsScope(plan.Model.Application, scope))
            {
                results.Add(new(scope.ToString(), scope.ToString(), string.Empty, string.Empty, SemanticSpecificationOutcome.Unsupported, null, new(StageExecutionCapability.Specification, scope.ToString(), "The requested semantic scope is not in the plan."), [], null));
            }
        }

        for (var index = 0; index < selected.Length; index++)
        {
            var (slice, specification) = selected[index];
            if (cancellationToken.IsCancellationRequested)
            {
                results.AddRange(selected.Skip(index).Select(item => Record(item.Slice, item.Specification, SemanticSpecificationOutcome.Cancelled)));
                break;
            }

            var blocker = options.DefaultCaller is null ? SemanticRunAdmission.Check(plan, specification) : new SemanticUnsupportedCapability(StageExecutionCapability.Authorization, specification.Id.ToString(), "Explicit caller context is not admitted.");
            if (blocker is not null)
            {
                results.Add(Record(slice, specification, SemanticSpecificationOutcome.Unsupported, unsupported: blocker));
                continue;
            }

            try
            {
                await _processGate.WaitAsync(cancellationToken);
                try
                {
                    results.Add(await Execute(specification, slice, plan, options, cancellationToken));
                }
                finally
                {
                    _processGate.Release();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                results.AddRange(selected.Skip(index).Select(item => Record(item.Slice, item.Specification, SemanticSpecificationOutcome.Cancelled)));
                break;
            }
            catch (Exception exception)
            {
                results.Add(Record(slice, specification, SemanticSpecificationOutcome.Failed, failures: [exception.Message]));
            }
        }

        return new("stage-spec-run/1", plan.Model.Application.Id.ToString(), plan.Revision.ToString(), results);
    }

    static async Task<SemanticSpecificationRunRecord> Execute(SemanticSpecification specification, SemanticSlice slice, SemanticExecutionPlan plan, SemanticSpecificationRunOptions options, CancellationToken cancellationToken)
    {
        var when = specification.When;
        var command = when is null ? null : plan.Commands[when.Command];
        var runtimeTypes = _types.GetOrAdd(plan.Revision, static (_, source) => new SemanticRuntimeTypes(source), plan);
        var runtimeType = command is null ? typeof(DynamicCommand) : runtimeTypes.ForCommand(command);
        var instance = command is null ? null : (DynamicCommand)Activator.CreateInstance(runtimeType)!;
        if (when is not null)
        {
            foreach (var value in when.Values)
            {
                var property = command!.Properties.Single(property => property.Id == value.TargetProperty);
                instance!.Data[property.Name] = JsonSerializer.Deserialize<JsonElement>(SemanticRunContext.Canonical(value.Value));
            }
        }

        var eventTypes = specification.GivenEvents.Select(given => given.EventContract)
            .Concat(command?.Produces.Select(produced => produced.EventContract) ?? [])
            .Concat(specification.WhenAppended is { } appendedEvent ? [appendedEvent.EventContract] : [])
            .Distinct().Select(runtimeTypes.For).ToArray();
        var eventStore = new EventStoreForTesting(null, new SemanticClientArtifactsProvider(eventTypes));
        var context = new SemanticRunContext(runtimeType, command!, specification, options, runtimeTypes, eventStore, plan.Model.SemanticVersion);

        // A new Chronicle-backed event log and scenario are created for each specification.
        foreach (var given in specification.GivenEvents)
        {
            await context.Append(given, cancellationToken);
        }
        var history = context.Facts.Select((fact, index) => new SemanticConstraintEvaluator.Fact(fact.EventContract, context.Destinations[index], fact.Values)).ToArray();
        if (specification.WhenAppended is { } appended)
        {
            var candidate = new SemanticConstraintEvaluator.Fact(appended.EventContract, appended.EventSource!.Value, appended.Values);
            if (SemanticConstraintEvaluator.FindViolation(plan, history, [candidate]) is { } violation)
            {
                return Rejected(slice, specification, SemanticConstraintEvaluator.Message(violation), violation.Name);
            }
            await context.Append(new SemanticSpecificationEvent(appended.EventContract, appended.Values) { EventSource = appended.EventSource }, appended.EventSource.Value, cancellationToken);
            return await Accepted(slice, specification, context, eventStore);
        }

        var caller = specification.GivenCaller;
        var principal = caller is null ? new ClaimsPrincipal() : SemanticPolicyEvaluator.Principal(caller);
        var allowed = SemanticPolicyEvaluator.Allows(command!.Authorization, plan, caller, principal, command, specification);
        if (allowed && SemanticRuleEvaluation.FirstFailure(command, specification, plan.Model.Application.Concepts) is { } failure)
        {
            return Rejected(slice, specification, failure);
        }
        if (allowed)
        {
            var candidates = command.Produces.Select(context.Produce)
                .Select(item => new SemanticConstraintEvaluator.Fact(item.Fact.EventContract, item.Destination, item.Fact.Values)).ToArray();
            if (SemanticConstraintEvaluator.FindViolation(plan, history, candidates) is { } constraint)
            {
                return Rejected(slice, specification, SemanticConstraintEvaluator.Message(constraint), constraint.Name);
            }
        }

        await using var scenario = new CommandScenario<object>();
        scenario.Services.AddSingleton(context);
        scenario.Services.AddSingleton<IDiscoverableValidators>(new SemanticCommandValidators(context));
        scenario.Services.AddSingleton<IAuthorizationEvaluator>(new SemanticArcAuthorization(plan, command, specification, principal, runtimeType));
        var principalOverride = new CurrentPrincipalAccessor(new HttpRequestContextAccessor());
        scenario.Services.AddSingleton<ICurrentPrincipalOverride>(principalOverride);
        scenario.Services.AddSingleton<ICurrentPrincipalAccessor>(principalOverride);
        using var scope = principalOverride.BeginScope(principal);
        var result = await scenario.Execute(instance!, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (result.HasExceptions)
        {
            return Record(slice, specification, SemanticSpecificationOutcome.Failed, failures: [.. result.ExceptionMessages]);
        }
        if (!result.IsAuthorized)
        {
            if (allowed || context.Facts.Count != specification.GivenEvents.Length)
                return Record(slice, specification, SemanticSpecificationOutcome.Failed, failures: ["Arc denied an allowed command or appended facts before denial."]);
            var failures = SemanticExpectationComparer.Compare(specification, [], [], "Caller is not authorized.", denied: true);
            return Record(slice, specification, failures.Count == 0 ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Failed, "Rejected", failures: failures, trace: new([], new Dictionary<string, string>(), new Dictionary<string, string>(), "Caller is not authorized."));
        }
        if (!allowed)
        {
            return Record(slice, specification, SemanticSpecificationOutcome.Failed, failures: ["Arc accepted a command rejected by the ESM authorization policy."]);
        }

        var rejection = result.ValidationResults.FirstOrDefault()?.Message;
        if (rejection is not null)
        {
            return Rejected(slice, specification, rejection);
        }
        return await Accepted(slice, specification, context, eventStore);
    }

    static SemanticSpecificationRunRecord Rejected(SemanticSlice slice, SemanticSpecification specification, string message, string? code = null)
    {
        var failures = SemanticExpectationComparer.Compare(specification, [], [], message, code);
        return Record(slice, specification, failures.Count == 0 ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Failed, "Rejected", failures: failures, trace: new SemanticExecutionTrace([], new Dictionary<string, string>(), new Dictionary<string, string>(), message) { RejectionCode = code });
    }

    static async Task<SemanticSpecificationRunRecord> Accepted(SemanticSlice slice, SemanticSpecification specification, SemanticRunContext context, EventStoreForTesting eventStore)
    {
        var persisted = await eventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
        if (persisted.Count != context.Facts.Count)
        {
            throw new SemanticAppendFailed("The in-memory Chronicle log does not contain every accepted fact.");
        }

        var facts = context.Facts.Skip(specification.GivenEvents.Length).ToArray();
        var destinations = context.Destinations.Skip(specification.GivenEvents.Length).ToArray();
        var failures = SemanticExpectationComparer.Compare(specification, facts, destinations, null);
        var trace = new SemanticExecutionTrace(
            [.. facts.Select((fact, index) => new SemanticTraceFact(fact.EventContract.ToString(), fact.EventSource?.Type.Kind == SemanticTypeReferenceKind.Concept ? fact.EventSource.Type.Target.ToString() : fact.EventSource?.Type.Primitive.ToString(), SemanticRunContext.Canonical(destinations[index]), fact.Values.ToDictionary(value => value.TargetProperty.ToString(), value => SemanticRunContext.Canonical(value.Value))))],
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            null);
        return Record(slice, specification, failures.Count == 0 ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Failed, "Accepted", failures: failures, trace: trace);
    }

    static SemanticSpecificationRunRecord Record(SemanticSlice slice, SemanticSpecification specification, SemanticSpecificationOutcome outcome, string? kind = null, SemanticUnsupportedCapability? unsupported = null, IReadOnlyList<string>? failures = null, SemanticExecutionTrace? trace = null) =>
        new(specification.Id.ToString(), specification.Name, slice.Id.ToString(), slice.Kind.ToString(), outcome, kind, unsupported, failures ?? [], trace);

    static bool ContainsScope(SemanticApplication application, SemanticId scope) =>
        scope == application.Id || application.Modules.Any(module => module.Id == scope || ContainsFeature(module.Features, scope));

    static bool ContainsFeature(IEnumerable<SemanticFeature> features, SemanticId scope) =>
        features.Any(feature => feature.Id == scope || feature.Slices.Any(slice => slice.Id == scope || slice.Specifications.Any(specification => specification.Id == scope)) || ContainsFeature(feature.Features, scope));

    static IEnumerable<(SemanticSlice Slice, SemanticSpecification Specification)> Select(SemanticApplication application, SemanticSpecificationSelection selection)
    {
        var scopes = selection.Scopes.IsDefault ? [] : selection.Scopes;
        foreach (var module in application.Modules)
        {
            foreach (var result in SelectFeatures(module.Features, scopes, scopes.IsEmpty || scopes.Contains(application.Id) || scopes.Contains(module.Id)))
            {
                yield return result;
            }
        }
    }

    static IEnumerable<(SemanticSlice Slice, SemanticSpecification Specification)> SelectFeatures(IEnumerable<SemanticFeature> features, System.Collections.Immutable.ImmutableArray<SemanticId> scopes, bool inherited)
    {
        foreach (var feature in features)
        {
            var selected = inherited || scopes.Contains(feature.Id);
            foreach (var slice in feature.Slices)
            {
                foreach (var specification in slice.Specifications)
                {
                    if (selected || scopes.Contains(slice.Id) || scopes.Contains(specification.Id))
                    {
                        yield return (slice, specification);
                    }
                }
            }

            foreach (var result in SelectFeatures(feature.Features, scopes, selected))
            {
                yield return result;
            }
        }
    }
}
