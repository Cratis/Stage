// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.Admission;

/// <summary>
/// Checks the entire reachable behavior before allowing a specification to execute.
/// </summary>
internal static class SemanticRunAdmission
{
    /// <summary>
    /// Returns the first unsupported construct in deterministic precedence order.
    /// </summary>
    /// <param name="plan">The executable plan.</param>
    /// <param name="specification">The specification to check.</param>
    /// <returns>A typed blocker, or null when execution is safe.</returns>
    public static SemanticUnsupportedCapability? Check(SemanticExecutionPlan plan, SemanticSpecification specification)
    {
        static SemanticUnsupportedCapability Block(StageExecutionCapability capability, SemanticId id, string details) => new(capability, id.ToString(), details);
        if (!specification.GivenReadModels.IsEmpty) return Block(StageExecutionCapability.GivenReadModel, specification.GivenReadModels[0].ReadModel, "Seeded read-model state requires a per-run projection engine.");
        if (!specification.ThenReadModels.IsEmpty) return Block(StageExecutionCapability.Projection, specification.ThenReadModels[0].ReadModel, "Read-model assertions require a per-run projection engine.");
        if (!specification.ThenQueries.IsEmpty) return Block(StageExecutionCapability.Query, specification.ThenQueries[0].Query, "Keyed queries require a per-run projection engine.");
        if (specification.GivenCaller?.Claims.Any(claim => string.Equals(claim.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase)) == true)
        {
            return Block(StageExecutionCapability.Authorization, specification.Id, "Role-URI claim types cannot be used as claims; roles and claims are separate in Screenplay.");
        }
        if (specification.WhenAppended is { } appended)
        {
            if (appended.EventSource is null) return Block(StageExecutionCapability.IdentityAllocation, appended.EventContract, "Direct append requires an explicit event source.");
            if (!plan.Events.TryGetValue(appended.EventContract, out var appendedContract)) return Block(StageExecutionCapability.PlanIssue, appended.EventContract, "The appended event is not in the plan.");
            if (appendedContract.Properties.Any(property => !Scalar(property.Type))) return Block(StageExecutionCapability.Occurrence, appended.EventContract, "Only scalar appended event values are admitted.");
        }
        foreach (var given in specification.GivenEvents)
        {
            if (given.EventSource is null) return Block(StageExecutionCapability.IdentityAllocation, given.EventContract, "Given events require an explicit event source.");
            if (!plan.Events.TryGetValue(given.EventContract, out var givenContract)) return Block(StageExecutionCapability.PlanIssue, given.EventContract, "The Given event is not in the plan.");
            if (givenContract.Properties.Any(property => !Scalar(property.Type))) return Block(StageExecutionCapability.Command, given.EventContract, "Only scalar Given event values are admitted.");
        }
        if (specification.WhenAppended is not null) return ProjectionBlock(plan, specification, [specification.WhenAppended.EventContract]);
        if (specification.When is not { } when) return Block(StageExecutionCapability.Specification, specification.Id, "Only command or direct-append specifications are admitted.");
        if (!plan.Commands.TryGetValue(when.Command, out var command)) return Block(StageExecutionCapability.Command, when.Command, "The command is not in the plan.");
        if (command.Properties.Any(property => property.Type.Kind == SemanticTypeReferenceKind.Concept &&
            plan.Model.Application.Concepts.Single(concept => concept.Id == property.Type.Target).Validations.Any(rule => !SupportedRule(rule.Kind))))
        {
            return Block(StageExecutionCapability.Command, command.Id, "A concept validation rule is not admitted.");
        }

        // The reference projects given events while establishing the world, and produced facts only when the command
        // is accepted. A specification expecting a rejection appends nothing, so its produced events reach no projection.
        var produced = specification.ThenErrors.IsEmpty && !specification.ThenDenied ? command.Produces.Select(produce => produce.EventContract) : [];
        var reachableEvents = specification.GivenEvents.Select(given => given.EventContract).Concat(produced).ToHashSet();
        if (ProjectionBlock(plan, specification, reachableEvents) is { } projectionBlock) return projectionBlock;

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

    static SemanticUnsupportedCapability? ProjectionBlock(SemanticExecutionPlan plan, SemanticSpecification specification, IEnumerable<SemanticId> reachableEvents)
    {
        var reachable = specification.GivenEvents.Select(given => given.EventContract).Concat(reachableEvents).ToHashSet();
        var projection = plan.Projections.Values.FirstOrDefault(value =>
            (value.Scope is not null && reachable.Count > 0) || value.Transitions.Any(transition => reachable.Contains(transition.EventContract)));
        return projection is null ? null : new(StageExecutionCapability.Projection, projection.Id.ToString(), "A projection consumes events in this specification but per-run projection execution is not available.");
    }

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
