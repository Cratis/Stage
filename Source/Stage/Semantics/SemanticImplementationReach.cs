// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Semantics;

internal static class SemanticImplementationReach
{
    internal const string Code = "STAGE-IMPL-003";

    internal static SemanticUnsupportedCapability? ForSpecification(SemanticExecutionPlan plan, SemanticSpecification specification)
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
        return First(plan, commands, queries, (reducer, transition) => queried.Contains(reducer.ReadModel) ||
            (reducer.From is null && (readModels.Contains(reducer.ReadModel) || events.Contains(transition.EventContract))) ||
            ObservesGiven(plan, specification, reducer, transition));
    }

    internal static SemanticUnsupportedCapability? ForCommand(SemanticExecutionPlan plan, SemanticCommand command)
    {
        // The shared conservative traversal follows every production, invocation and refusal-branch
        // production, without guessing which runtime guard will be taken.
        var specification = new SemanticSpecification(command.Id, command.Name, [], [], new(command.Id, []), [], [], [], []);

        return ForSpecification(plan, specification);
    }

    internal static SemanticUnsupportedCapability? ForQuery(SemanticExecutionPlan plan, SemanticKeyedQuery query) =>
        First(plan, [], [query.Id], (reducer, _) => reducer.ReadModel == query.ReadModel);

    internal static SemanticUnsupportedCapability? ForFact(SemanticExecutionPlan plan, SemanticId @event, SemanticEventRoute? route) =>
        First(plan, [], [], (reducer, transition) => transition.EventContract == @event &&
            SemanticObserverFilters.Matches(plan.Model.Application, reducer.From, route));

    internal static SemanticUnsupportedCapability? ForWorld(SemanticExecutionPlan plan, IEnumerable<SemanticId> readModels)
    {
        var rows = readModels.ToHashSet();
        return First(plan, [], [], (reducer, _) => rows.Contains(reducer.ReadModel));
    }

    static bool ObservesGiven(SemanticExecutionPlan plan, SemanticSpecification specification, SemanticReducer reducer, SemanticReducerTransition transition) =>
        specification.GivenEvents.Any(fact => fact.EventContract == transition.EventContract &&
            SemanticObserverFilters.Matches(plan.Model.Application, reducer.From, SemanticObserverFilters.Kinds(plan, fact.Route)));

    static SemanticUnsupportedCapability? First(
        SemanticExecutionPlan plan,
        HashSet<SemanticId> commands,
        HashSet<SemanticId> queries,
        Func<SemanticReducer, SemanticReducerTransition, bool> reaches)
    {
        var slices = SemanticVersionFeatures.Slices(plan.Model.Application).ToArray();
        var context = new SemanticApplicationContext(plan);
        var types = slices.SelectMany(slice => slice.Commands).Where(command => commands.Contains(command.Id)).SelectMany(command => command.Properties.Select(property => property.Type))
            .Concat(slices.SelectMany(slice => slice.Queries).Where(query => queries.Contains(query.Id)).SelectMany(QueryTypes));
        var concepts = types.SelectMany(type => SemanticValueShapeAdmission.Closure(context, type))
            .Where(member => member.Type.Kind == SemanticTypeReferenceKind.Concept).Select(member => member.Type.Target).ToHashSet();
        foreach (var concept in plan.Model.Application.Concepts.Where(concept => concepts.Contains(concept.Id)))
        {
            var rule = concept.Validations.FirstOrDefault(rule => Opaque(rule.Kind));
            if (rule is not null)
            {
                return Refusal(StageExecutionCapability.Command, concept.Id, $"Concept '{concept.Name}' validation '{rule.RequirementId}'");
            }
        }

        // Concepts precede module contents in the model. Within modules, keep slice and
        // declaration order rather than dictionary or cascade discovery order.
        foreach (var slice in slices)
        {
            foreach (var command in slice.Commands.Where(command => commands.Contains(command.Id)))
            {
                var requirement = command.CodeValidations.Select(validation => validation.RequirementId)
                    .Concat(command.Validations.Where(rule => Opaque(rule.Kind)).Select(rule => rule.RequirementId)).FirstOrDefault();
                if (requirement is not null)
                {
                    return Refusal(StageExecutionCapability.Command, command.Id, $"Command '{command.Name}' validation '{requirement}'");
                }
            }
            foreach (var reducer in slice.Reducers)
            {
                var transition = reducer.Transitions.FirstOrDefault(transition => reaches(reducer, transition));
                if (transition is not null)
                {
                    return Refusal(StageExecutionCapability.Projection, reducer.ReadModel, $"Reducer '{reducer.Name}' transition '{transition.RequirementId}'");
                }
            }
        }

        return null;
    }

    static IEnumerable<SemanticTypeReference> QueryTypes(SemanticKeyedQuery query) =>
        (query.Argument is { } argument ? new[] { argument.Type } : [])
            .Concat(query.KeyParts.IsDefaultOrEmpty ? [] : query.KeyParts.Select(part => part.Type))
            .Concat(query.Filters.Select(filter => filter.Type));

    static bool Opaque(SemanticValidationRuleKind kind) =>
        kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation;

    static SemanticUnsupportedCapability Refusal(StageExecutionCapability capability, SemanticId owner, string body) =>
        new(capability, owner.ToString(), $"{Code}: {body} is an implementation body; Stage does not execute implementation bodies without a target.");
}
