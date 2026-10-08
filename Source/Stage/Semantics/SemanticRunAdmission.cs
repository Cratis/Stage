// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Checks the entire reachable behavior before allowing a specification to execute.
/// </summary>
public static class SemanticRunAdmission
{
    /// <summary>
    /// Finds later-version constructs that block live model admission.
    /// </summary>
    /// <param name="application">The application.</param>
    /// <returns>The precise capability refusals.</returns>
    public static IEnumerable<SemanticAdmissionFeature> ModelFeatures(SemanticApplication application) =>
        SemanticVersionFeatures.InApplication(application).Concat(SemanticVersionFeatures.Slices(application).SelectMany(SemanticVersionFeatures.InSlice)).Select(Feature);

    /// <summary>
    /// Finds later-version constructs in a command.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The precise capability refusals.</returns>
    public static IEnumerable<SemanticAdmissionFeature> CommandFeatures(SemanticCommand command) => SemanticVersionFeatures.InCommand(command).Select(Feature);

    /// <summary>
    /// Finds later-version assertions in a specification.
    /// </summary>
    /// <param name="specification">The specification.</param>
    /// <returns>The precise capability refusals.</returns>
    public static IEnumerable<SemanticAdmissionFeature> SpecificationFeatures(SemanticSpecification specification) => SemanticVersionFeatures.InSpecification(specification).Select(Feature);

    /// <summary>
    /// Returns the first unsupported construct in deterministic precedence order.
    /// </summary>
    /// <param name="plan">The executable plan.</param>
    /// <param name="specification">The specification to check.</param>
    /// <returns>A typed blocker, or null when execution is safe.</returns>
    public static SemanticUnsupportedCapability? Check(SemanticExecutionPlan plan, SemanticSpecification specification)
    {
        static SemanticUnsupportedCapability Block(StageExecutionCapability capability, SemanticId id, string details) => new(capability, id.ToString(), details);
        var slices = plan.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices).ToArray();
        if (LaterVersionConstruct(plan, slices, specification) is { } later) return later;

        // A per-run result must not pass an application whose selected slice cannot be rendered.
        // Include declarations in the specification's slice even if the example never produces them.
        foreach (var slice in slices)
        {
            var collidingEvent = slice.Events.FirstOrDefault(@event =>
                !GeneratedPascalCase.EventMembersAreUnique(@event.Name, @event.Properties.Select(property => property.Name)));
            if (collidingEvent is not null)
            {
                var referenced = slices.SelectMany(candidate => candidate.Projections)
                    .Any(projection => ProjectionReferencedEventNamesAreUnique.Contracts(projection).Contains(collidingEvent.Id));
                return Block(
                    referenced ? StageExecutionCapability.Projection : StageExecutionCapability.Command,
                    collidingEvent.Id,
                    "Event property names collide in generated C#.");
            }
            var collidingCommand = slice.Commands.FirstOrDefault(command =>
                !GeneratedPascalCase.CommandMembersAreUnique(command.Name, command.Properties.Select(property => property.Name)));
            if (collidingCommand is not null) return Block(StageExecutionCapability.Command, collidingCommand.Id, "Command property names collide in generated C#.");
            var collidingReadModel = slice.ReadModels.FirstOrDefault(readModel =>
                !GeneratedPascalCase.ReadModelMembersAreUnique(
                    readModel.Name,
                    readModel.Properties.Select(property => property.Name),
                    slice.Queries.Where(query => query.ReadModel == readModel.Id).Select(query => query.Name)) ||
                !GeneratedPascalCase.QueriesAreUnique(slice.Queries.Where(query => query.ReadModel == readModel.Id)
                    .Select(query => (query.Name, SemanticRunProjectionAdmission.QueryType(query.Argument.Type, plan), query.Argument.Name))));
            if (collidingReadModel is not null) return Block(StageExecutionCapability.Projection, collidingReadModel.Id, "A read-model property name or query collides in generated C#.");
            var collidingConstraint = slice.Constraints.FirstOrDefault(constraint =>
                !GeneratedPascalCase.ConstraintTypeNameIsSafe(constraint.Name));
            if (collidingConstraint is not null) return Block(StageExecutionCapability.Command, slice.Id, "Constraint type name collides with generated C# members.");
            var collidingProjection = slice.Projections.FirstOrDefault(projection =>
                projection.Scope is not null && !GeneratedPascalCase.ProjectionTypeNameIsSafe(projection.Name));
            if (collidingProjection is not null) return Block(StageExecutionCapability.Projection, collidingProjection.Id, "Projection type name collides with generated C# members.");
        }
        var typeCollision = GeneratedTypeNames.Collisions(
            plan.Model.Application,
            GeneratedTypeNames.AllSlices(plan.Model.Application)).FirstOrDefault();
        if (typeCollision.Artifact.IsSet)
        {
            var capability = typeCollision.Kind == "ReadModel" || typeCollision.Kind == "Projection"
                ? StageExecutionCapability.Projection : StageExecutionCapability.Command;
            return Block(capability, typeCollision.Artifact, typeCollision.Kind == "Namespace"
                ? $"Generated type '{typeCollision.Name}' collides with a generated C# namespace."
                : $"{typeCollision.Kind} '{typeCollision.Name}' collides with another generated C# type.");
        }
        var identifiers = slices.SelectMany(slice => slice.Commands.SelectMany(command => command.Properties)
            .Concat(slice.ReadModels.SelectMany(model => model.Properties)))
            .Where(property => property.IsIdentifier && property.Type.Kind == SemanticTypeReferenceKind.Concept)
            .Select(property => property.Type.Target).ToHashSet();
        var collidingConcept = plan.Model.Application.Concepts.FirstOrDefault(concept =>
            !GeneratedPascalCase.ConceptMembersAreUnique(concept.Name, concept.Values, identifiers.Contains(concept.Id)));
        if (collidingConcept is not null) return Block(StageExecutionCapability.Command, collidingConcept.Id, "Concept members collide in generated C#.");
        var collidingType = plan.Model.Application.Types.FirstOrDefault(type =>
            !GeneratedPascalCase.RecordMembersAreUnique(type.Name, type.Properties.Select(property => property.Name)));
        if (collidingType is not null) return Block(StageExecutionCapability.Command, collidingType.Id, "Type property names collide in generated C#.");
        var reducer = slices.SelectMany(slice => slice.Reducers).FirstOrDefault();
        if (reducer is not null) return Block(StageExecutionCapability.Projection, reducer.ReadModel, "Reducer implementation bodies cannot be executed by Stage.");
        var opaqueConcept = plan.Model.Application.Concepts.FirstOrDefault(concept => concept.Validations.Any(rule => OpaqueRule(rule.Kind)));
        if (opaqueConcept is not null) return Block(StageExecutionCapability.Command, opaqueConcept.Id, "Validation implementation bodies cannot be executed by Stage.");
        if (!specification.GivenReadModels.IsEmpty) return Block(StageExecutionCapability.GivenReadModel, specification.GivenReadModels[0].ReadModel, "Given read-model state cannot be seeded into the per-run projection scenario.");
        if (specification.GivenCaller is { Authenticated: false } caller && (!caller.Roles.IsEmpty || !caller.Claims.IsEmpty))
        {
            return Block(StageExecutionCapability.Authorization, specification.Id, "An unauthenticated caller cannot carry roles or claims; Arc supplies an empty guest principal.");
        }
        if (specification.GivenCaller?.Claims.Any(claim => string.Equals(claim.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase)) == true)
        {
            return Block(StageExecutionCapability.Authorization, specification.Id, "Role-URI claim types cannot be used as claims; roles and claims are separate in Screenplay.");
        }
        if (specification.WhenAppended is { } appended)
        {
            if (appended.EventSource is null) return Block(StageExecutionCapability.IdentityAllocation, appended.EventContract, "Direct append requires an explicit event source.");
            if (!plan.Events.TryGetValue(appended.EventContract, out var appendedContract)) return Block(StageExecutionCapability.PlanIssue, appended.EventContract, "The appended event is not in the plan.");
            if (!GeneratedPascalCase.EventMembersAreUnique(appendedContract.Name, appendedContract.Properties.Select(property => property.Name))) return Block(StageExecutionCapability.Command, appendedContract.Id, "Event property names collide in generated C#.");
            if (appendedContract.Properties.Any(property => !Scalar(property.Type))) return Block(StageExecutionCapability.Occurrence, appended.EventContract, "Only scalar appended event values are admitted.");
        }
        foreach (var given in specification.GivenEvents)
        {
            if (given.EventSource is null) return Block(StageExecutionCapability.IdentityAllocation, given.EventContract, "Given events require an explicit event source.");
            if (!plan.Events.TryGetValue(given.EventContract, out var givenContract)) return Block(StageExecutionCapability.PlanIssue, given.EventContract, "The Given event is not in the plan.");
            if (!GeneratedPascalCase.EventMembersAreUnique(givenContract.Name, givenContract.Properties.Select(property => property.Name))) return Block(StageExecutionCapability.Command, givenContract.Id, "Event property names collide in generated C#.");
            if (givenContract.Properties.Any(property => !Scalar(property.Type))) return Block(StageExecutionCapability.Command, given.EventContract, "Only scalar Given event values are admitted.");
        }
        if (specification.WhenAppended is not null) return SemanticRunProjectionAdmission.Check(plan, specification, specification.GivenEvents.Select(given => given.EventContract).Append(specification.WhenAppended.EventContract));
        if (specification.When is not { } when) return Block(StageExecutionCapability.Specification, specification.Id, "Only command or direct-append specifications are admitted.");
        if (!plan.Commands.TryGetValue(when.Command, out var command)) return Block(StageExecutionCapability.Command, when.Command, "The command is not in the plan.");
        if (!GeneratedPascalCase.CommandMembersAreUnique(command.Name, command.Properties.Select(property => property.Name))) return Block(StageExecutionCapability.Command, command.Id, "Command property names collide in generated C#.");
        foreach (var emitted in command.Produces)
        {
            if (plan.Events.TryGetValue(emitted.EventContract, out var producedContract) &&
                !GeneratedPascalCase.EventMembersAreUnique(producedContract.Name, producedContract.Properties.Select(property => property.Name)))
            {
                return Block(StageExecutionCapability.Command, producedContract.Id, "Event property names collide in generated C#.");
            }
        }
        if (!command.CodeValidations.IsEmpty || command.Validations.Any(rule => OpaqueRule(rule.Kind)))
        {
            return Block(StageExecutionCapability.Command, command.Id, "Validation implementation bodies cannot be executed by Stage.");
        }
        if (command.Properties.Any(property => property.Type.Kind == SemanticTypeReferenceKind.Concept &&
            plan.Model.Application.Concepts.Single(concept => concept.Id == property.Type.Target).Validations.Any(rule => !SupportedRule(rule.Kind))))
        {
            return Block(StageExecutionCapability.Command, command.Id, "A concept validation rule is not admitted.");
        }

        // The reference projects given events while establishing the world, and produced facts only when the command
        // is accepted. A specification expecting a rejection appends nothing, so its produced events reach no projection.
        var produced = specification.ThenErrors.IsEmpty && !specification.ThenDenied ? command.Produces.Select(produce => produce.EventContract) : [];
        var reachableEvents = specification.GivenEvents.Select(given => given.EventContract).Concat(produced).ToHashSet();
        if (SemanticRunProjectionAdmission.Check(plan, specification, reachableEvents) is { } projectionBlock) return projectionBlock;

        if (command.Properties.Any(property => !Scalar(property.Type)) ||
            command.Produces.Any(produced => !plan.Events.TryGetValue(produced.EventContract, out var eventContract) || eventContract.Properties.Any(property => !Scalar(property.Type))))
        {
            return Block(StageExecutionCapability.Command, command.Id, "Only scalar command and event values are admitted.");
        }

        if (command.Validations.Any(rule => !SupportedRule(rule.Kind)))
        {
            return Block(StageExecutionCapability.Command, command.Id, "A command validation rule is not admitted.");
        }

        if (command.Produces.Any(produced => produced.Condition is not null || produced.When is not null || produced.Mappings.Any(mapping => mapping.Source is not (SemanticValueExpression or SemanticResolvedExpression { Root: SemanticExpressionRootKind.Command, Source: SemanticExpressionSourceKind.Property }))))
        {
            return Block(StageExecutionCapability.Command, command.Id, "Conditional or non-command mappings are not admitted.");
        }

        if (command.Produces.Any(produced => produced.Destination is not null and not (SemanticValueExpression or SemanticResolvedExpression { Root: SemanticExpressionRootKind.Command })))
        {
            return Block(StageExecutionCapability.Command, command.Id, "The destination expression is not admitted.");
        }

        if (specification.ThenErrors.IsEmpty && when.EventSource is null && command.Destination?.Value is null && command.Produces.Any(produced => produced.Destination is null))
        {
            return Block(StageExecutionCapability.IdentityAllocation, command.Id, "An accepted command requires an explicit destination.");
        }
        return null;
    }

    static SemanticAdmissionFeature Feature(SemanticVersionFeature feature) => new(feature.Artifact.ToString(), feature.Kind, feature.Capability.ToString(), $"{feature.Code}: {feature.Message}");

    // ESM v5-v7 constructs Stage cannot execute block the run rather than being skipped: a reaction's follow-up work,
    // an absence assertion, a generated value or a response would otherwise be dropped and the run could pass.
    // Any automation construct in the model blocks every run, since its consequences could follow any command.
    static SemanticUnsupportedCapability? LaterVersionConstruct(SemanticExecutionPlan plan, SemanticSlice[] slices, SemanticSpecification specification)
    {
        var command = specification.When is { } when && plan.Commands.TryGetValue(when.Command, out var found) ? found : null;
        var feature = SemanticVersionFeatures.InApplication(plan.Model.Application)
            .Concat(slices.SelectMany(SemanticVersionFeatures.InSlice))
            .Concat(SemanticVersionFeatures.InSpecification(specification))
            .Concat(command is null ? [] : SemanticVersionFeatures.InCommand(command))
            .FirstOrDefault();
        return feature is null ? null : new(feature.Capability, feature.Artifact.ToString(), $"{feature.Code}: {feature.Message}");
    }

    static IEnumerable<SemanticSlice> AllSlices(SemanticFeature feature) =>
        feature.Slices.Concat(feature.Features.SelectMany(AllSlices));

    static bool OpaqueRule(SemanticValidationRuleKind kind) =>
        kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation;

    static bool SupportedRule(SemanticValidationRuleKind kind) => kind is
        SemanticValidationRuleKind.NotEmpty or SemanticValidationRuleKind.Maximum or SemanticValidationRuleKind.Minimum or
        SemanticValidationRuleKind.Equal or SemanticValidationRuleKind.NotEqual or SemanticValidationRuleKind.GreaterThan or
        SemanticValidationRuleKind.GreaterThanOrEqual or SemanticValidationRuleKind.LessThan or
        SemanticValidationRuleKind.LessThanOrEqual or SemanticValidationRuleKind.Length or SemanticValidationRuleKind.Matches;

    static bool Scalar(SemanticTypeReference type)
    {
        if (type.IsCollection)
        {
            return false;
        }

        return type.Kind switch
        {
            SemanticTypeReferenceKind.Concept => true,
            SemanticTypeReferenceKind.Primitive => type.Primitive != SemanticPrimitiveType.Unknown,
            _ => false
        };
    }
}
