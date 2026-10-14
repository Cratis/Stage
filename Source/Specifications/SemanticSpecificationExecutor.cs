// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Http;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.Comparison;
using Cratis.Stage.Specifications.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Stage.Specifications;

/// <summary>
/// Executes admitted specifications and event reactions against an isolated in-memory event log.
/// </summary>
public sealed partial class SemanticSpecificationExecutor : ISemanticSpecificationExecutor
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
                results.AddRange(selected.Skip(index).Select(item => Cancelled(item.Slice, item.Specification)));
                break;
            }

            var blocker = options.DefaultCaller is null ? SemanticSpecificationAdmission.Check(plan, specification) : new SemanticUnsupportedCapability(StageExecutionCapability.Authorization, specification.Id.ToString(), "Explicit caller context is not admitted.");
            if (blocker is not null)
            {
                results.Add(Unsupported(slice, specification, blocker));
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
                results.AddRange(selected.Skip(index).Select(item => Cancelled(item.Slice, item.Specification)));
                break;
            }
            catch (OpaquePolicyReached exception)
            {
                results.Add(Unsupported(slice, specification, new(StageExecutionCapability.Authorization, specification.Id.ToString(), exception.Message)));
            }
            catch (UnsupportedSemanticMapping exception)
            {
                results.Add(Unsupported(slice, specification, new(StageExecutionCapability.Command, specification.Id.ToString(), exception.Message)));
            }
            catch (Exception exception)
            {
                results.Add(Failed(slice, specification, [exception.Message]));
            }
        }

        var enriched = results.Select(result => EnrichCaseFailure(result, options)).ToArray();

        return new("stage-spec-run/1", plan.Model.Application.Id.ToString(), plan.Revision.ToString(), enriched);
    }

    static SemanticSpecificationRunRecord EnrichCaseFailure(SemanticSpecificationRunRecord result, SemanticSpecificationRunOptions options)
    {
        if (result.Outcome != SemanticSpecificationOutcome.Failed ||
            !SemanticId.TryParse(result.SpecificationId, out var id) ||
            !options.SpecificationOrigins.TryGetValue(id, out var origin) || origin.Case is not { } row)
        {
            return result;
        }

        var fixtures = origin.Steps.SelectMany(step => step.Values.Select(value =>
            $"{step.Role}{(step.Example is null ? string.Empty : $" {step.Example.Name}")}: {value.Property} = {SpecificationFixtureText.Expression(value.Value)} ({value.Origin.ToString().ToLowerInvariant()}{(value.OverriddenValue is null ? string.Empty : $", replaces {SpecificationFixtureText.Expression(value.OverriddenValue)}")})"));
        var provenance = $"Effective fixtures: {string.Join("; ", fixtures)}.";
        var prefix = $"Case '{row.Name}' of '{origin.Authored.Name}': ";

        return result with { Failures = [.. result.Failures.Select(failure => $"{prefix}{failure} {provenance}")] };
    }

    static async Task<SemanticSpecificationRunRecord> Execute(SemanticSpecification specification, SemanticSlice slice, SemanticExecutionPlan plan, SemanticSpecificationRunOptions options, CancellationToken cancellationToken)
    {
        if (SemanticSpecificationClock.TryGet(specification, out var instant)) options = options with { Clock = new FixedSemanticClock(instant) };
        var when = specification.When;
        var command = when is null ? null : plan.Commands[when.Command];
        var runtimeTypes = _types.GetOrAdd(plan.Revision, static (_, source) => new SemanticRuntimeTypes(source), plan);
        var runtimeType = command is null ? typeof(DynamicCommand) : runtimeTypes.ForCommand(command);
        var instance = command is null ? null : Materialize(runtimeType, command, when!);

        var eventTypes = plan.Events.Keys.Select(runtimeTypes.For).ToArray();
        var defaults = new Defaults(new SemanticClientArtifactsProvider(eventTypes));
        using var eventScenario = new EventScenario(defaults);
        var context = new SemanticRunContext(runtimeType, command!, specification, options, runtimeTypes, eventScenario.EventLog, plan);

        // A new Chronicle-backed event log and scenario are created for each specification.
        foreach (var given in specification.GivenEvents)
        {
            await context.Append(given, cancellationToken);
        }
        var history = context.Facts.Select((fact, index) => new SemanticConstraintEvaluator.Fact(fact.EventContract, context.Destinations[index], fact.Values)).ToArray();
        if (specification.WhenAppended is { } appended)
        {
            var candidate = new SemanticConstraintEvaluator.Fact(appended.EventContract, appended.EventSource?.Value ?? SemanticValue.Null, appended.Values);
            if (SemanticConstraintEvaluator.FindViolation(plan, history, [candidate]) is { } violation)
            {
                return Rejected(slice, specification, new SemanticRejection(SemanticConstraintEvaluator.Message(violation), violation.Name));
            }
            await context.Append(new SemanticSpecificationEvent(appended.EventContract, appended.Values) { EventSource = appended.EventSource, Route = appended.Route }, candidate.Destination, cancellationToken);
        }
        else if (command is not null)
        {
            if ((await ExecuteCommand(new(context, instance!, slice, specification, plan), cancellationToken)).Failure is { } outcome) return outcome;
        }

        var actionFacts = context.Facts.Count - specification.GivenEvents.Length;
        var reactions = new SemanticRunReactions(plan, context, specification.GivenEvents.Length, async (fixture, collectRefusalDetails) =>
        {
            var invoked = plan.Commands[fixture.When!.Command];
            var invokedType = runtimeTypes.ForCommand(invoked);
            var invokedContext = new SemanticRunContext(invokedType, invoked, fixture, options, runtimeTypes, eventScenario.EventLog, plan, context);
            var invokedInstance = Materialize(invokedType, invoked, fixture.When);
            var principal = fixture.GivenCaller is null ? new ClaimsPrincipal() : SemanticPolicyEvaluator.SystemPrincipal(fixture.GivenCaller);

            return await ExecuteCommand(new(invokedContext, invokedInstance, slice, specification, plan, principal), cancellationToken, collectRefusalDetails);
        });
        var reactionFailure = specification.WhenRedelivered is { } redelivery
            ? await reactions.Redeliver(redelivery.Reaction, redelivery.Given, cancellationToken)
            : await reactions.Settle(cancellationToken);
        if (reactionFailure is { } failure) return failure.ToRecord(slice, specification);

        return await Accepted(slice, specification, context, eventScenario, runtimeTypes, defaults, actionFacts);
    }

    static DynamicCommand Materialize(Type runtimeType, SemanticCommand command, SemanticSpecificationCommand when)
    {
        var instance = (DynamicCommand)Activator.CreateInstance(runtimeType)!;
        foreach (var value in when.Values)
        {
            var property = command.Properties.Single(property => property.Id == value.TargetProperty);
            instance.Data[property.Name] = JsonSerializer.Deserialize<JsonElement>(SemanticRunContext.Canonical(value.Value));
        }

        return instance;
    }

    static async Task<SemanticInvocationOutcome> ExecuteCommand(SemanticCommandRun run, CancellationToken cancellationToken, bool collectRefusalDetails = false)
    {
        var context = run.Context;
        var specification = context.Specification;
        var command = context.Command;
        var priorFacts = context.Facts.Count;
        var caller = specification.GivenCaller;
        var principal = run.ReactionPrincipal ?? (caller is null ? new ClaimsPrincipal() : SemanticPolicyEvaluator.Principal(caller));
        var allowed = SemanticPolicyEvaluator.Allows(command.Authorization, run.Plan, caller, principal, command, specification);
        if (allowed && PrepareCommand(run, collectRefusalDetails) is { } preparation) return preparation;

        await using var scenario = new CommandScenario<object>();
        var principalOverride = ConfigureScenario(scenario, run, principal);
        using var scope = principalOverride.BeginScope(principal);
        var result = await scenario.Execute(run.Instance, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var failure = CommandOutcome(run, result, allowed, priorFacts);
        var refusal = collectRefusalDetails && !allowed && !result.HasExceptions && !result.IsAuthorized && context.Facts.Count == priorFacts
            ? new SemanticInvocationRefusalOutcome("authorization", [], SemanticRejection.Unauthorized.Message) : null;

        return new(failure, refusal, result);
    }

    static CurrentPrincipalAccessor ConfigureScenario(CommandScenario<object> scenario, SemanticCommandRun run, ClaimsPrincipal principal)
    {
        var context = run.Context;
        if (SemanticSpecificationClock.TryGet(context.Specification, out var instant)) scenario.Clock(instant);
        scenario.Services.AddSingleton(context);
        scenario.Services.AddSingleton<IDiscoverableValidators>(new SemanticCommandValidators(context));
        scenario.Services.AddSingleton<IAuthorizationEvaluator>(new SemanticArcAuthorization(run.Plan, context.Command, context.Specification, principal, context.CommandType));
        var principalOverride = new CurrentPrincipalAccessor(new HttpRequestContextAccessor());
        scenario.Services.AddSingleton<ICurrentPrincipalOverride>(principalOverride);
        scenario.Services.AddSingleton<ICurrentPrincipalAccessor>(principalOverride);

        return principalOverride;
    }

    static SemanticSpecificationRunRecord? CommandOutcome(SemanticCommandRun run, CommandResult result, bool allowed, int priorFacts)
    {
        var (slice, expected) = (run.Slice, run.Expected);
        if (result.HasExceptions) return Failed(slice, expected, [.. result.ExceptionMessages]);
        if (!result.IsAuthorized)
        {
            if (allowed || run.Context.Facts.Count != priorFacts) return Failed(slice, expected, ["Arc denied an allowed command or appended facts before denial."]);

            return Rejected(slice, expected, SemanticRejection.Unauthorized);
        }
        if (!allowed) return Failed(slice, expected, ["Arc accepted a command rejected by the ESM authorization policy."]);

        return result.ValidationResults.FirstOrDefault()?.Message is { } rejection ? Rejected(slice, expected, rejection) : null;
    }

    static SemanticInvocationOutcome? PrepareCommand(SemanticCommandRun run, bool collectRefusalDetails)
    {
        var (context, slice, expected) = (run.Context, run.Slice, run.Expected);
        if (run.IsInvocation && InvocationContractFailure(context, run.Plan) is { } contractFailure) return new(Rejected(slice, expected, contractFailure));
        if (collectRefusalDetails)
        {
            var failures = SemanticRuleEvaluation.Failures(context.Command, context.Specification, run.Plan.Model.Application.Concepts).ToArray();
            if (failures.Length > 0)
            {
                var errors = failures.Where(failure => failure.Severity == SemanticValidationSeverity.Error).ToArray();
                return new(Rejected(slice, expected, failures[0].Message), errors.Length == 0 ? null : new("validation", [], string.Join('\n', errors.Select(failure => failure.Message))));
            }
        }
        if (CommandInputOutcome(context, slice, run.Plan.Model.Application.Concepts, true) is { } inputOutcome) return new(Reframe(inputOutcome, slice, expected));
        if (run.IsInvocation && SemanticSpecificationAdmission.Check(run.Plan, context.Specification) is { } blocker) return new(Unsupported(slice, expected, blocker));
        if (NeedsExplicitDestination(context))
        {
            return new(Unsupported(slice, expected, new(StageExecutionCapability.IdentityAllocation, context.Command.Id.ToString(), "An accepted command requires an explicit destination.")));
        }

        return CommandAppendOutcome(run, collectRefusalDetails);
    }

    static bool NeedsExplicitDestination(SemanticRunContext context) =>
        context.Specification.When!.EventSource is null && context.Command.Destination?.Value is null && context.Command.Produces.Where(context.Holds).Any(produced => produced.Destination is null);

    static SemanticInvocationOutcome? CommandAppendOutcome(SemanticCommandRun run, bool collectRefusalDetails)
    {
        var (context, slice, expected) = (run.Context, run.Slice, run.Expected);
        var history = context.Facts.Select((fact, index) => new SemanticConstraintEvaluator.Fact(fact.EventContract, context.Destinations[index], fact.Values)).ToArray();
        var candidates = context.Command.Produces.Where(context.Holds).Select(context.Produce)
            .Select(item => new SemanticConstraintEvaluator.Fact(item.Fact.EventContract, item.Destination, item.Fact.Values)).ToArray();
        if (ReusesGeneratedSource(context, candidates.Select(candidate => candidate.Destination)))
        {
            return new(Rejected(slice, expected, SemanticGenerationShapes.FreshnessMessage));
        }
        var violations = SemanticConstraintEvaluator.FindViolations(run.Plan, history, candidates);
        var constraints = (collectRefusalDetails ? violations : violations.Take(1)).ToArray();
        if (constraints.Length > 0)
        {
            var first = constraints[0];
            var refusal = collectRefusalDetails ? new SemanticInvocationRefusalOutcome(
                "constraint",
                [.. constraints.Select(constraint => new SemanticConstraintFailure(constraint.Name, SemanticConstraintEvaluator.Message(constraint)))],
                string.Join('\n', constraints.Select(SemanticConstraintEvaluator.Message))) : null;
            return new(Rejected(slice, expected, new SemanticRejection(SemanticConstraintEvaluator.Message(first), first.Name)), refusal);
        }

        return context.Facts.Count - expected.GivenEvents.Length + candidates.Length > SemanticRunReactions.MaximumFacts
            ? new(SemanticRunReactions.FactLimit().ToRecord(slice, expected)) : null;
    }

    static string? InvocationContractFailure(SemanticRunContext context, SemanticExecutionPlan plan)
    {
        try
        {
            foreach (var value in context.Specification.When!.Values)
            {
                var property = context.Command.Properties.Single(property => property.Id == value.TargetProperty);
                SemanticRunReactionExpressions.Validate(plan, value.Value, property.Type, $"command property '{property.Name}'");
            }
        }
        catch (InvalidSemanticContract exception)
        {
            return exception.Message;
        }

        return null;
    }

    static SemanticSpecificationRunRecord Reframe(SemanticSpecificationRunRecord result, SemanticSlice slice, SemanticSpecification expected) =>
        result.Trace?.Rejection is { } rejection
            ? Rejected(slice, expected, new SemanticRejection(rejection, result.Trace.RejectionCode))
            : result with { SpecificationId = expected.Id.ToString(), Name = expected.Name, SliceId = slice.Id.ToString(), SliceKind = slice.Kind.ToString() };

    static SemanticSpecificationRunRecord? CommandInputOutcome(SemanticRunContext context, SemanticSlice slice, IEnumerable<SemanticConcept> concepts, bool allowed)
    {
        if (!allowed) return null;
        if (SemanticRuleEvaluation.FirstFailure(context.Command, context.Specification, concepts) is { } failure)
        {
            return Rejected(slice, context.Specification, failure);
        }

        // The reference resolves the route after authorization and validation, before identity allocation.
        if (!context.TryResolveRoute(out var routeFailure))
        {
            return Rejected(slice, context.Specification, SemanticStreamIdFormatter.FailureMessage(routeFailure));
        }

        return GeneratedFixtureOutcome(context, slice, concepts, allowed);
    }

    static SemanticSpecificationRunRecord? GeneratedFixtureOutcome(SemanticRunContext context, SemanticSlice slice, IEnumerable<SemanticConcept> concepts, bool allowed)
    {
        if (!allowed) return null;
        var command = context.Command;
        var properties = command.Properties.Where(property => property.IsGenerated).ToArray();
        if (properties.Length == 0) return null;
        var specification = context.Specification;
        var generated = specification.When!.GeneratedValues.IsDefault ? [] : specification.When.GeneratedValues;
        if (FixtureShapeFailure(context, properties, generated) is { } shapeFailure)
        {
            return Rejected(slice, specification, shapeFailure);
        }
        if (properties.Any(property => !generated.Any(value => value.TargetProperty == property.Id)))
        {
            return Unsupported(slice, specification, new(StageExecutionCapability.IdentityAllocation, command.Id.ToString(), "Generated command values require deterministic fixtures."));
        }
        var failure = SemanticRuleEvaluation.GeneratedFailure(command, specification, concepts);

        return failure is null ? null : Rejected(slice, specification, failure);
    }

    static string? FixtureShapeFailure(SemanticRunContext context, IEnumerable<SemanticProperty> properties, System.Collections.Immutable.ImmutableArray<SemanticPropertyValue> generated)
    {
        if (context.Plan.Model.SemanticVersion.IsAtLeast(SemanticVersion.V15)) return StrategyFixtureFailure(context, generated);

        return GeneratedFixturesMatch(properties, generated) ? null : "Generated fixtures do not match the command's generated UUID properties.";
    }

    static bool GeneratedFixturesMatch(IEnumerable<SemanticProperty> properties, System.Collections.Immutable.ImmutableArray<SemanticPropertyValue> generated) =>
        generated.Select(value => value.TargetProperty).Distinct().Count() == generated.Length &&
        generated.All(value => properties.Any(property => property.Id == value.TargetProperty) && IsUuidFixture(value.Value));

    static bool IsUuidFixture(SemanticValue value) => value is SemanticTextValue text && Guid.TryParse(text.Value, out _);

    static async Task<SemanticSpecificationRunRecord> Accepted(SemanticSlice slice, SemanticSpecification specification, SemanticRunContext context, EventScenario eventScenario, SemanticRuntimeTypes runtimeTypes, Defaults defaults, int actionFacts)
    {
        var plan = context.Plan;
        var persisted = await eventScenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
        if (persisted.Count != context.Facts.Count)
        {
            throw new SemanticAppendFailed("The in-memory Chronicle log does not contain every accepted fact.");
        }

        var facts = context.Facts.Skip(specification.GivenEvents.Length).ToArray();
        var destinations = context.Destinations.Skip(specification.GivenEvents.Length).ToArray();
        var projected = SemanticRunProjections.Execute(plan, specification, context, runtimeTypes, defaults);
        foreach (var assertion in specification.ThenQueries)
        {
            if (!SemanticPolicyEvaluator.Allows(plan.Queries[assertion.Query], assertion.Key, plan, specification.GivenCaller))
            {
                return Rejected(slice, specification, SemanticRejection.Unauthorized);
            }
            if (SemanticQueryValidation.Failures(plan, plan.Queries[assertion.Query], assertion).FirstOrDefault() is { } validation)
            {
                return Rejected(slice, specification, new SemanticRejection(validation.Message) { Category = SemanticRejectionCategory.Validation });
            }
        }
        var failures = SemanticExpectationComparer.Compare(specification, new(facts, destinations, plan, actionFacts)).ToList();
        failures.AddRange(SemanticExpectationComparer.CompareProjections(specification, projected, plan));
        failures.AddRange(SemanticExpectationComparer.CompareResponse(specification.ThenReturns, context.Response));
        var trace = new SemanticExecutionTrace(
            [.. facts.Select((fact, index) => new SemanticTraceFact(
                fact.EventContract.ToString(),
                fact.EventSource?.Type.Kind == SemanticTypeReferenceKind.Concept ? fact.EventSource.Type.Target.ToString() : fact.EventSource?.Type.Primitive.ToString(),
                SemanticRunContext.Canonical(destinations[index]),
                fact.Values.ToDictionary(value => value.TargetProperty.ToString(), value => SemanticRunContext.Canonical(value.Value)))
            {
                Route = SemanticRunRoutes.Format(plan, fact.Route)
            })],
            projected.ToDictionary(row => $"{row.ReadModel}:{SemanticRunContext.Canonical(row.Key)}", row => JsonSerializer.Serialize(row.Values.ToDictionary(value => value.TargetProperty.ToString(), value => SemanticRunContext.Canonical(value.Value)))),
            specification.ThenQueries.Select((query, index) => (query, index)).ToDictionary(
                entry => $"{entry.index}:{entry.query.Query}:{SemanticRunContext.Canonical(entry.query.Key)}",
                entry => JsonSerializer.Serialize(projected.Where(row => row.ReadModel == plan.Queries[entry.query.Query].ReadModel && SemanticExpectationComparer.AreEqual(row.Key, entry.query.Key)).Select(row => row.Values.ToDictionary(value => value.TargetProperty.ToString(), value => SemanticRunContext.Canonical(value.Value))))),
            null);
        return Settled(slice, specification, failures, trace);
    }

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
