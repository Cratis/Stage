// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Semantics;

public static partial class SemanticRunAdmission
{
    static SemanticUnsupportedCapability Block(StageExecutionCapability capability, SemanticId id, string details) => new(capability, id.ToString(), details);

    static SemanticUnsupportedCapability? CheckSpecification(SemanticExecutionPlan plan, SemanticSpecification specification, SemanticBodyExecution? bodies)
    {
        if (ModelRefusal(plan, specification) is { } modelRefusal) return modelRefusal;

        // A specification runs only its own slice (Screenplay decision 0115): admit what that slice runs, not other slices' reactions or captures.
        if (plan.IsolatedSlice is null && plan.SliceOf(specification.Id) is { } owner) plan = plan.IsolatedTo(owner);
        var check = new RunCheck(plan, specification, bodies);

        return StructuralRefusal(check) ?? SemanticImplementationReach.ForSpecification(plan, specification, bodies) ?? BehaviorRefusal(check);
    }

    static SemanticUnsupportedCapability? StructuralRefusal(RunCheck check) => VersionRefusal(check) ?? RuntimeNameRefusal(check);

    static SemanticUnsupportedCapability? BehaviorRefusal(RunCheck check) => CallerRefusal(check.Specification) ?? FactRefusal(check) ?? ActionRefusal(check);

    static SemanticUnsupportedCapability? VersionRefusal(RunCheck check) =>
        LaterVersionConstruct(check.Plan, check.Slices, check.Specification) ?? SemanticRunTranslationAdmission.Check(check.Plan, check.Specification);

    // Runtime contracts keep authored property names, not the renderer's PascalCase members.
    // Case-insensitive duplicates still make JSON binding and Chronicle payloads ambiguous.
    static SemanticUnsupportedCapability? RuntimeNameRefusal(RunCheck check) =>
        check.Slices.Select(slice => SliceNameRefusal(check, slice)).FirstOrDefault(refusal => refusal is not null) ?? TypeNameRefusal(check.Plan);

    static SemanticUnsupportedCapability? SliceNameRefusal(RunCheck check, SemanticSlice slice) =>
        EventNameRefusal(check, slice) ?? CommandNameRefusal(slice) ?? ReadModelNameRefusal(slice);

    static SemanticUnsupportedCapability? EventNameRefusal(RunCheck check, SemanticSlice slice)
    {
        var colliding = slice.Events.FirstOrDefault(@event => !RuntimePropertyNamesAreUnique(@event.Properties));
        if (colliding is null) return null;

        var referenced = check.Slices.SelectMany(candidate => candidate.Projections)
            .Any(projection => ProjectionReferencedEventNamesAreUnique.Contracts(projection).Contains(colliding.Id));
        return Block(
            referenced ? StageExecutionCapability.Projection : StageExecutionCapability.Command,
            colliding.Id,
            "Event property names are not unique under case-insensitive runtime JSON binding.");
    }

    static SemanticUnsupportedCapability? CommandNameRefusal(SemanticSlice slice) =>
        slice.Commands.FirstOrDefault(command => !RuntimePropertyNamesAreUnique(command.Properties)) is { } colliding
            ? Block(StageExecutionCapability.Command, colliding.Id, "Command property names are not unique under case-insensitive runtime JSON binding.")
            : null;

    static SemanticUnsupportedCapability? ReadModelNameRefusal(SemanticSlice slice) =>
        slice.ReadModels.FirstOrDefault(readModel => !RuntimePropertyNamesAreUnique(readModel.Properties)) is { } colliding
            ? Block(StageExecutionCapability.Projection, colliding.Id, "Read-model property names are not unique under case-insensitive runtime JSON binding.")
            : null;

    static SemanticUnsupportedCapability? TypeNameRefusal(SemanticExecutionPlan plan) =>
        plan.Model.Application.Types.FirstOrDefault(type => !RuntimePropertyNamesAreUnique(type.Properties)) is { } colliding
            ? Block(StageExecutionCapability.Command, colliding.Id, "Type property names are not unique under case-insensitive runtime JSON binding.")
            : null;

    static SemanticUnsupportedCapability? CallerRefusal(SemanticSpecification specification)
    {
        if (specification.GivenCaller is { Authenticated: false } caller && CarriesIdentity(caller))
        {
            return Block(StageExecutionCapability.Authorization, specification.Id, "An unauthenticated caller cannot carry roles or claims; Arc supplies an empty guest principal.");
        }

        return specification.GivenCaller?.Claims.Any(claim => string.Equals(claim.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase)) == true
            ? Block(StageExecutionCapability.Authorization, specification.Id, "Role-URI claim types cannot be used as claims; roles and claims are separate in Screenplay.")
            : null;
    }

    static bool CarriesIdentity(SemanticCaller caller) => !caller.Roles.IsEmpty || !caller.Claims.IsEmpty;

    static SemanticUnsupportedCapability? FactRefusal(RunCheck check) => AppendedFactRefusal(check) ?? GivenFactRefusal(check);

    static SemanticUnsupportedCapability? AppendedFactRefusal(RunCheck check)
    {
        if (check.Specification.WhenAppended is not { } appended) return null;
        if (!check.Plan.Events.TryGetValue(appended.EventContract, out var contract)) return Block(StageExecutionCapability.PlanIssue, appended.EventContract, "The appended event is not in the plan.");

        return EventValueRefusal(check.Plan, contract, appended.Values) is { } refusal ? Block(StageExecutionCapability.Occurrence, appended.EventContract, refusal) : null;
    }

    static SemanticUnsupportedCapability? GivenFactRefusal(RunCheck check)
    {
        foreach (var given in check.Specification.GivenEvents)
        {
            if (!check.Plan.Events.TryGetValue(given.EventContract, out var contract)) return Block(StageExecutionCapability.PlanIssue, given.EventContract, "The Given event is not in the plan.");
            if (EventValueRefusal(check.Plan, contract, given.Values) is { } refusal) return Block(StageExecutionCapability.Command, given.EventContract, refusal);
        }

        return null;
    }

    static SemanticUnsupportedCapability? ActionRefusal(RunCheck check)
    {
        var specification = check.Specification;
        if (specification.WhenAppended is { } appended)
        {
            return SemanticRunReactionAdmission.CheckProjections(check.Plan, specification, check.GivenContracts.Append(appended.EventContract));
        }

        if (specification.When is not { } when) return SemanticRunReactionAdmission.CheckProjections(check.Plan, specification, check.GivenContracts);
        if (!check.Plan.Commands.TryGetValue(when.Command, out var command)) return Block(StageExecutionCapability.Command, when.Command, "The command is not in the plan.");

        return CommandPlanRefusal(check.Plan, command) ??
            CommandContractRefusal(check, command) ??
            (command.Handler is not null ? HandlerRefusal(check, command) : ProductionRefusal(check, command));
    }

    static SemanticUnsupportedCapability? CommandContractRefusal(RunCheck check, SemanticCommand command)
    {
        if (CommandValidationRefusal(check.Plan, command) is { } validationRefusal) return Block(StageExecutionCapability.Command, command.Id, validationRefusal);

        var valueContext = new SemanticApplicationContext(check.Plan);
        var valueRefusal = command.Properties.Select(property => SemanticValueShapeAdmission.Refusal(valueContext, property.Type, check.Bodies)).FirstOrDefault(refusal => refusal is not null);
        return valueRefusal is null ? null : Block(StageExecutionCapability.Command, command.Id, valueRefusal);
    }

    // A described handler determines its own productions. Only history can be projected before its body is reached.
    static SemanticUnsupportedCapability? HandlerRefusal(RunCheck check, SemanticCommand command) =>
        ValidationRuleRefusal(check, command) ?? SemanticRunReactionAdmission.CheckProjections(check.Plan, check.Specification, check.GivenContracts);

    static SemanticUnsupportedCapability? ValidationRuleRefusal(RunCheck check, SemanticCommand command) =>
        command.Validations.Any(rule => !SupportedRule(rule, check.Bodies))
            ? Block(StageExecutionCapability.Command, command.Id, "A command validation rule is not admitted.")
            : null;

    static SemanticUnsupportedCapability? ProductionRefusal(RunCheck check, SemanticCommand command) =>
        ReachableProjectionRefusal(check, command) ??
        ProducedEventRefusal(check, command) ??
        ValidationRuleRefusal(check, command) ??
        ProductionShapeRefusal(check, command) ??
        DestinationShapeRefusal(command) ??
        IdentityAllocationRefusal(check, command);

    static SemanticUnsupportedCapability? ReachableProjectionRefusal(RunCheck check, SemanticCommand command)
    {
        // Projections never run for expected rejection/denial, whether the command rejects or reports an acceptance mismatch.
        var specification = check.Specification;
        var produced = specification.ThenErrors.IsEmpty && !specification.ThenDenied
            ? SemanticProductionConditions.Taken(command, specification.When!, check.Plan.Model.Application).Select(produce => produce.EventContract) : [];
        var reachableEvents = check.GivenContracts.Concat(produced).ToHashSet();

        return SemanticRunReactionAdmission.CheckProjections(check.Plan, specification, reachableEvents);
    }

    static SemanticUnsupportedCapability? ProducedEventRefusal(RunCheck check, SemanticCommand command)
    {
        foreach (var production in command.Produces)
        {
            if (!check.Plan.Events.TryGetValue(production.EventContract, out var eventContract)) return Block(StageExecutionCapability.Command, command.Id, "The produced event is not in the plan.");
            if (EventValueRefusal(check.Plan, eventContract) is { } eventRefusal) return Block(StageExecutionCapability.Command, command.Id, eventRefusal);
        }

        return null;
    }

    static SemanticUnsupportedCapability? ProductionShapeRefusal(RunCheck check, SemanticCommand command) =>
        command.Produces.Any(produced => produced.Condition is not null || produced.Mappings.Any(mapping => IsUnadmittedMapping(check.Specification, mapping.Source)))
            ? Block(StageExecutionCapability.Command, command.Id, "Expression conditions or non-command mappings are not admitted.")
            : null;

    static bool IsUnadmittedMapping(SemanticSpecification specification, SemanticExpression source) =>
        source is not (SemanticValueExpression or SemanticReadPathExpression or SemanticResolvedExpression { Root: SemanticExpressionRootKind.Command, Source: SemanticExpressionSourceKind.Property }) &&
        !IsControlledOccurrence(specification, source);

    static bool IsControlledOccurrence(SemanticSpecification specification, SemanticExpression source) =>
        source is SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred } &&
        (SemanticSpecificationClock.TryGetOccurrence(specification, out _) || !specification.ThenErrors.IsEmpty || specification.ThenDenied);

    static SemanticUnsupportedCapability? DestinationShapeRefusal(SemanticCommand command) =>
        command.Produces.Any(produced => produced.Destination is not null and not (SemanticValueExpression or SemanticResolvedExpression { Root: SemanticExpressionRootKind.Command }))
            ? Block(StageExecutionCapability.Command, command.Id, "The destination expression is not admitted.")
            : null;

    // A routed command can reject its route before reaching identity allocation; defer that decision to execution.
    static SemanticUnsupportedCapability? IdentityAllocationRefusal(RunCheck check, SemanticCommand command)
    {
        var specification = check.Specification;
        var when = specification.When!;
        var requiresDestination = specification.ThenErrors.IsEmpty && command.Route is null && when.EventSource is null && command.Destination?.Value is null &&
            SemanticProductionConditions.Taken(command, when, check.Plan.Model.Application).Any(produced => produced.Destination is null);

        return requiresDestination ? Block(StageExecutionCapability.IdentityAllocation, command.Id, "An accepted command requires an explicit destination.") : null;
    }

    sealed record RunCheck(SemanticExecutionPlan Plan, SemanticSpecification Specification, SemanticBodyExecution? Bodies)
    {
        public SemanticSlice[] Slices { get; } = [.. Plan.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices)];

        public IEnumerable<SemanticId> GivenContracts => Specification.GivenEvents.Select(given => given.EventContract);
    }
}
