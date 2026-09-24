// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.Admission;

/// <summary>
/// Checks the entire reachable behavior before allowing a specification to execute.
/// </summary>
public static class SemanticRunAdmission
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
        if (specification.GivenCaller is not null || specification.ThenDenied) return Block(StageExecutionCapability.Authorization, specification.Id, "Caller and denial semantics are not admitted.");
        if (specification.WhenAppended is not null) return Block(StageExecutionCapability.Occurrence, specification.WhenAppended.EventContract, "Direct append is not a command.");
        foreach (var given in specification.GivenEvents)
        {
            if (given.EventSource is null) return Block(StageExecutionCapability.IdentityAllocation, given.EventContract, "Given events require an explicit event source.");
            if (!plan.Events.ContainsKey(given.EventContract)) return Block(StageExecutionCapability.PlanIssue, given.EventContract, "The Given event is not in the plan.");
        }
        if (specification.When is not { } when) return Block(StageExecutionCapability.Specification, specification.Id, "Only command specifications are admitted.");
        if (!plan.Commands.TryGetValue(when.Command, out var command)) return Block(StageExecutionCapability.Command, when.Command, "The command is not in the plan.");
        if (command.Authorization is not null) return Block(StageExecutionCapability.Authorization, command.Id, "Command authorization is not admitted.");
        if (!command.Requirements.IsEmpty) return Block(StageExecutionCapability.Requirement, command.Id, "Command requirements are not admitted.");
        if (plan.Constraints.Count > 0) return Block(StageExecutionCapability.Constraint, command.Id, "Append-time constraints are not admitted.");
        if (plan.Model.Application.Concepts.Any(concept => !concept.Validations.IsEmpty) && command.Properties.Any(property => property.Type.Kind == SemanticTypeReferenceKind.Concept))
        {
            return Block(StageExecutionCapability.Command, command.Id, "Concept validation is not admitted.");
        }

        if (command.Properties.Any(property => !Scalar(property.Type)) ||
            command.Produces.Any(produced => !plan.Events.TryGetValue(produced.EventContract, out var eventContract) || eventContract.Properties.Any(property => !Scalar(property.Type))))
        {
            return Block(StageExecutionCapability.Command, command.Id, "Only scalar command and event values are admitted.");
        }

        if (command.Validations.Any(rule => rule.Kind is not (SemanticValidationRuleKind.NotEmpty or SemanticValidationRuleKind.Minimum or SemanticValidationRuleKind.Maximum) || rule.Message is null || rule.Severity != SemanticValidationSeverity.Error))
        {
            return Block(StageExecutionCapability.Command, command.Id, "Only explicit-message NotEmpty, Minimum and Maximum rules are admitted.");
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
