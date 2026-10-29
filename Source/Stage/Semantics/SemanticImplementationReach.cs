// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Semantics;

internal static class SemanticImplementationReach
{
    internal const string Code = "STAGE-IMPL-003";

    internal static SemanticUnsupportedCapability? ForSpecification(SemanticExecutionPlan plan, SemanticSpecification specification, SemanticBodyExecution? bodies = null)
    {
        var (events, _, blocker) = SemanticRunReactionAdmission.Reachable(plan, specification, specification.GivenEvents.Select(given => given.EventContract));
        if (blocker is not null) return blocker;

        // Match command admission: an expected refusal must not hide an opaque invoked command or a reducer its events feed.
        var (rejectedEvents, commands, commandBlocker) = SemanticRunReactionAdmission.Reachable(plan, specification, [], includeRejected: true);
        if (commandBlocker is not null) return commandBlocker;
        events.UnionWith(rejectedEvents);
        if (specification.When is { } when) commands.Add(when.Command);
        var queries = specification.ThenQueries.Select(assertion => assertion.Query).ToHashSet();
        var queried = plan.Queries.Values.Where(query => queries.Contains(query.Id)).Select(query => query.ReadModel).ToHashSet();
        var readModels = specification.GivenReadModels.Select(state => state.ReadModel)
            .Concat(specification.ThenReadModels.Select(assertion => assertion.ReadModel))
            .Concat(specification.ThenAbsentReadModels.Select(assertion => assertion.ReadModel)).ToHashSet();

        // Initial filtered seeds are allowed here; completed reducer-backed worlds refuse at the later world gate.
        return First(
            plan,
            new(commands, queries, bodies),
            (reducer, transition) => queried.Contains(reducer.ReadModel) ||
                (reducer.From is null && (readModels.Contains(reducer.ReadModel) || events.Contains(transition.EventContract))) ||
                ObservesGiven(plan, specification, reducer, transition));
    }

    internal static SemanticUnsupportedCapability? ForCommand(SemanticExecutionPlan plan, SemanticCommand command, SemanticBodyExecution? bodies = null)
    {
        // The shared conservative traversal follows every production, invocation and refusal-branch
        // production, without guessing which runtime guard will be taken.
        var specification = new SemanticSpecification(command.Id, command.Name, [], [], new(command.Id, []), [], [], [], []);

        return ForSpecification(plan, specification, bodies);
    }

    internal static SemanticUnsupportedCapability? ForQuery(SemanticExecutionPlan plan, SemanticKeyedQuery query) =>
        First(plan, new([], [query.Id], null), (reducer, _) => reducer.ReadModel == query.ReadModel);

    internal static SemanticUnsupportedCapability? ForFact(SemanticExecutionPlan plan, SemanticId @event, SemanticEventRoute? route, SemanticBodyExecution? bodies = null) =>
        First(
            plan,
            new([], [], bodies),
            (reducer, transition) => transition.EventContract == @event && SemanticObserverFilters.Matches(plan.Model.Application, reducer.From, route));

    internal static SemanticUnsupportedCapability? ForWorld(SemanticExecutionPlan plan, IEnumerable<SemanticId> readModels, SemanticBodyExecution? bodies = null)
    {
        var rows = readModels.ToHashSet();
        return First(plan, new([], [], bodies), (reducer, _) => rows.Contains(reducer.ReadModel));
    }

    internal static bool IsBodyRefusal(string details, SemanticBodyExecution? bodies) =>
        details.StartsWith($"{Code}:", StringComparison.Ordinal) ||
        bodies?.Manifest.Entries.Any(entry => !entry.Admitted && entry.DiagnosticMessage == details) == true;

    static bool ObservesGiven(SemanticExecutionPlan plan, SemanticSpecification specification, SemanticReducer reducer, SemanticReducerTransition transition) =>
        specification.GivenEvents.Any(fact => fact.EventContract == transition.EventContract &&
            SemanticObserverFilters.Matches(plan.Model.Application, reducer.From, SemanticObserverFilters.Kinds(plan, fact.Route)));

    static SemanticUnsupportedCapability? First(
        SemanticExecutionPlan plan,
        SemanticImplementationSelection selection,
        Func<SemanticReducer, SemanticReducerTransition, bool> reaches)
    {
        var commands = selection.Commands;
        var queries = selection.Queries;
        var bodies = selection.Bodies;
        var slices = SemanticVersionFeatures.Slices(plan.Model.Application).ToArray();
        var context = new SemanticApplicationContext(plan);
        var queryTypes = slices.SelectMany(slice => slice.Queries).Where(query => queries.Contains(query.Id)).SelectMany(QueryTypes).ToArray();
        var types = slices.SelectMany(slice => slice.Commands).Where(command => commands.Contains(command.Id)).SelectMany(command => command.Properties.Where(property => IncludeProperty(property, bodies)).Select(property => property.Type))
            .Concat(queryTypes);
        var concepts = Concepts(context, types);
        var queriedConcepts = Concepts(context, queryTypes);
        foreach (var concept in plan.Model.Application.Concepts.Where(concept => concepts.Contains(concept.Id)))
        {
            var execution = queriedConcepts.Contains(concept.Id) ? null : bodies;
            if (ConceptRefusals(concept, execution).FirstOrDefault() is { } refusal) return refusal;
        }

        return SliceRefusal(slices, selection, reaches);
    }

    // Concepts precede module contents in the model. Within modules, keep slice and declaration order.
    static SemanticUnsupportedCapability? SliceRefusal(IEnumerable<SemanticSlice> slices, SemanticImplementationSelection selection, Func<SemanticReducer, SemanticReducerTransition, bool> reaches) =>
        slices.Select(slice => slice.Commands.Where(command => selection.Commands.Contains(command.Id))
            .Select(command => CommandRefusal(command, selection.Bodies)).FirstOrDefault(refusal => refusal is not null) ?? ReducerRefusal(slice, reaches))
            .FirstOrDefault(refusal => refusal is not null);

    static SemanticUnsupportedCapability? ReducerRefusal(SemanticSlice slice, Func<SemanticReducer, SemanticReducerTransition, bool> reaches) =>
        slice.Reducers.Select(reducer => ReducerRefusal(reducer, reducer.Transitions.FirstOrDefault(transition => reaches(reducer, transition))))
            .FirstOrDefault(refusal => refusal is not null);

    static SemanticUnsupportedCapability? ReducerRefusal(SemanticReducer reducer, SemanticReducerTransition? transition) => transition is null ? null :
        Refusal(StageExecutionCapability.Projection, reducer.ReadModel, SemanticImplementationRole.ReducerTransition, $"Reducer '{reducer.Name}' transition '{transition.RequirementId}'");

    static HashSet<SemanticId> Concepts(SemanticApplicationContext context, IEnumerable<SemanticTypeReference> types) =>
        [.. types.SelectMany(type => SemanticValueShapeAdmission.Closure(context, type))
            .Where(member => member.Type.Kind == SemanticTypeReferenceKind.Concept).Select(member => member.Type.Target)];

    static bool IncludeProperty(SemanticProperty property, SemanticBodyExecution? bodies) => bodies is null || !property.IsGenerated;

    static IEnumerable<SemanticUnsupportedCapability> ConceptRefusals(SemanticConcept concept, SemanticBodyExecution? bodies) =>
        concept.Validations.Where(rule => Opaque(rule.Kind) && !Admitted(rule, bodies))
            .Select(rule => RuleRefusal(Refusal(StageExecutionCapability.Command, concept.Id, ConceptRole(rule), $"Concept '{concept.Name}' validation '{rule.RequirementId}'"), rule.RequirementId, bodies));

    static bool Admitted(SemanticValidationRule rule, SemanticBodyExecution? bodies) =>
        rule.Kind == SemanticValidationRuleKind.RulePredicate && bodies?.IsAdmitted(rule.RequirementId) == true;

    static bool AdmittedCommandRule(SemanticCommand command, SemanticValidationRule rule, SemanticBodyExecution? bodies) =>
        !command.Properties.Any(property => property.Id == rule.Property && property.IsGenerated) && Admitted(rule, bodies);

    static SemanticUnsupportedCapability? CommandRefusal(SemanticCommand command, SemanticBodyExecution? bodies)
    {
        var requirement = command.CodeValidations.Select(validation => validation.RequirementId)
            .Concat(command.Validations.Where(rule => Opaque(rule.Kind) && !AdmittedCommandRule(command, rule, bodies)).Select(rule => rule.RequirementId)).FirstOrDefault();
        return requirement is null ? null : RuleRefusal(Refusal(StageExecutionCapability.Command, command.Id, CommandRole(command), $"Command '{command.Name}' validation '{requirement}'"), requirement, bodies);
    }

    static SemanticUnsupportedCapability RuleRefusal(SemanticUnsupportedCapability refusal, string? requirement, SemanticBodyExecution? bodies) =>
        bodies?.Refusal(requirement) is { } diagnostic ? refusal with { Details = diagnostic } : refusal;

    static IEnumerable<SemanticTypeReference> QueryTypes(SemanticKeyedQuery query) =>
        (query.Argument is { } argument ? new[] { argument.Type } : [])
            .Concat(query.KeyParts.IsDefaultOrEmpty ? [] : query.KeyParts.Select(part => part.Type))
            .Concat(query.Filters.Select(filter => filter.Type));

    static SemanticImplementationRole ConceptRole(SemanticValidationRule rule) =>
        rule.Kind == SemanticValidationRuleKind.CodeValidation ? SemanticImplementationRole.ConceptValidation : SemanticImplementationRole.RulePredicate;

    static SemanticImplementationRole CommandRole(SemanticCommand command) =>
        command.CodeValidations.IsEmpty ? SemanticImplementationRole.RulePredicate : SemanticImplementationRole.CommandValidation;

    static bool Opaque(SemanticValidationRuleKind kind) =>
        kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation;

    static SemanticUnsupportedCapability Refusal(StageExecutionCapability capability, SemanticId owner, SemanticImplementationRole role, string body) =>
        new(capability, owner.ToString(), $"{Code}: {body} is an implementation body; {BodyDiagnostics.NotExecuted(role)}");
}
