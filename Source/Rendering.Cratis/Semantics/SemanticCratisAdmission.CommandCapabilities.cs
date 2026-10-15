// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Explains each independently unsupported command shape without changing admission.
/// </summary>
internal static partial class SemanticCratisAdmission
{
    const string ValidationBodyReason = "Arc does not supply RuleContext.Occurred (received-at) to a generated validator, and Stage does not yet enforce the pure capability. Use equivalent declarative validation; implementation bodies are not supported yet (Cratis/Stage#409).";

    /// <summary>
    /// Reports every unsupported command capability, retaining the command diagnostic identity.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="slice">The declaring slice.</param>
    /// <param name="command">The command to admit.</param>
    /// <param name="diagnostics">The collected diagnostics.</param>
    /// <returns>Whether all command capabilities are supported.</returns>
    internal static bool ValidateCommandCapabilities(SemanticApplicationContext context, SemanticSlice slice, SemanticCommand command, List<ArtifactRenderDiagnostic> diagnostics)
    {
        var before = diagnostics.Count;
        foreach (var validation in command.CodeValidations)
        {
            diagnostics.Add(CommandCapability(command, $"validation implementation body '{validation.RequirementId}' cannot be rendered: {ValidationBodyReason}"));
        }

        ValidateCommandEventProperties(context, slice, command, diagnostics);
        foreach (var property in command.Properties.Where(property => !TypeExists(context, property.Type)))
        {
            diagnostics.Add(CommandCapability(command, $"property '{property.Name}' has an unresolved type; Stage cannot emit its C# type. Declare a supported primitive, concept, or composite type."));
        }

        ValidateCommandRules(context, command, diagnostics);
        ValidateCommandRequirements(context, command, diagnostics);
        ValidateOptionalCommandConcepts(context, command, diagnostics);
        if (!HasCommandOutcome(command))
        {
            diagnostics.Add(CommandCapability(command, "has no outcome; Stage cannot emit a handler without a produced event or response. Add a produces or returns clause."));
        }

        return diagnostics.Count == before;
    }

    static void ValidateCommandEventProperties(SemanticApplicationContext context, SemanticSlice slice, SemanticCommand command, List<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var @event in slice.Events)
        {
            foreach (var property in @event.Properties)
            {
                if (!TypeExists(context, property.Type))
                {
                    diagnostics.Add(CommandCapability(command, $"event '{@event.Name}' property '{property.Name}' has an unresolved type; Stage cannot emit its C# type. Declare a supported primitive, concept, or composite type."));
                }

                if (property.Type.IsOptional)
                {
                    diagnostics.Add(CommandCapability(command, $"event '{@event.Name}' property '{property.Name}' is optional; Cratis events have no optional properties. Make it required (in Studio's model board, tick 'Required' on the property) or remove it."));
                }
            }
        }
    }

    static void ValidateCommandRules(SemanticApplicationContext context, SemanticCommand command, List<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var rule in command.Validations)
        {
            var property = command.Properties.Single(property => property.Id == rule.Property);
            var label = rule.Name ?? rule.Kind.ToString();
            if (rule.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation)
            {
                diagnostics.Add(CommandCapability(command, $"validation '{label}' on property '{property.Name}' has implementation body '{rule.RequirementId}' that cannot be rendered: {ValidationBodyReason}"));
            }
            else if (!SemanticValidationRendering.CanRender(rule, context))
            {
                diagnostics.Add(CommandCapability(command, $"validation '{label}' on property '{property.Name}' cannot be rendered: {ValidationRenderingReason(rule, context)}"));
            }

            if (property.Type.Kind != SemanticTypeReferenceKind.Concept || !context.Concepts.TryGetValue(property.Type.Target, out var concept))
            {
                continue;
            }

            if (property.Type.IsOptional && concept.Primitive is SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber or SemanticPrimitiveType.Boolean &&
                (property.Type.IsCollection || rule.Kind == SemanticValidationRuleKind.NotEmpty))
            {
                diagnostics.Add(CommandCapability(command, $"validation '{label}' on optional property '{property.Name}' uses {concept.Primitive} concept '{concept.Name}'; only absence-passing scalar comparisons are supported. Collection rules and 'not empty' remain unsupported. Make the property non-optional only if absence is not part of the contract, or keep this shape unsupported (Cratis/Stage#419)."));
            }

            if (!concept.Values.IsEmpty)
            {
                diagnostics.Add(CommandCapability(command, $"validation '{label}' on property '{property.Name}' uses concept '{concept.Name}' with named values; Stage renders these values as an enum and cannot apply the underlying-value predicate faithfully. Remove the command validation if the named values already express the rule; otherwise keep this combination unsupported (Cratis/Stage#409)."));
            }
        }
    }

    static string ValidationRenderingReason(SemanticValidationRule rule, SemanticApplicationContext context)
    {
        if (!SemanticValidationRendering.CanResolve(rule.Message, context))
        {
            return MessageRenderingReason(rule.Message);
        }

        if (!SemanticValidationRendering.SafeOperand(rule))
        {
            return "the text operand cannot be preserved in the generated predicate or default message. Use text without control or surrogate characters, and an explicit safe message for braces in equality operands.";
        }

        if (rule.Operand is SemanticNullValue && rule.Kind is SemanticValidationRuleKind.Equal or SemanticValidationRuleKind.NotEqual)
        {
            return "null equality is not supported by the generated predicate. Use supported declarative validation with equivalent semantics, or keep this shape unsupported (Cratis/Stage#409).";
        }

        if (!Enum.IsDefined(rule.Severity))
        {
            return $"severity '{rule.Severity}' has no generated equivalent. Use Error, Warning, or Information severity.";
        }

        return $"rule kind '{rule.Kind}' has no faithful generated validator. Use a supported declarative rule, or keep this shape unsupported (Cratis/Stage#409).";
    }

    static string MessageRenderingReason(string? message) => message?.StartsWith("$strings.", StringComparison.Ordinal) == true
        ? $"message '{message}' has no strings catalog. Supply the companion .strings files and default locale in the render profile."
        : "the message contains formatting, control, or surrogate characters that FluentValidation cannot preserve. Use a literal message without these characters.";

    static void ValidateCommandRequirements(SemanticApplicationContext context, SemanticCommand command, List<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var (requirement, index) in command.Requirements.Select((requirement, index) => (requirement, index)))
        {
            var properties = string.Join(", ", RequirementProperties(requirement.Condition, command));
            var label = $"requirement #{index + 1} ({properties})";
            if (!SemanticRequirementRendering.CanRender(requirement, command, context))
            {
                var reason = RequirementRenderingReason(requirement, context);
                diagnostics.Add(CommandCapability(command, $"{label} cannot be rendered: {reason}"));
            }

            foreach (var property in command.Properties.Where(property => HasValidatedConcept(context, property.Type, [])))
            {
                diagnostics.Add(CommandCapability(command, $"{label} is combined with validated concept property '{property.Name}'; Arc graph validation and command guards cannot preserve the reference evaluator's failure order. Model equivalent rules at one validation level, or keep this combination unsupported (Cratis/Stage#409)."));
            }
        }
    }

    static string RequirementRenderingReason(SemanticRequirement requirement, SemanticApplicationContext context)
    {
        if (!SemanticValidationRendering.CanResolve(requirement.Message, context))
        {
            return MessageRenderingReason(requirement.Message);
        }

        if (!Enum.IsDefined(requirement.Severity))
        {
            return $"severity '{requirement.Severity}' has no generated equivalent. Use Error, Warning, or Information severity.";
        }

        return "the condition has no faithful generated guard. Use supported comparisons over non-optional scalar command properties without named concept values, or keep this shape unsupported (Cratis/Stage#409).";
    }

    static IEnumerable<string> RequirementProperties(SemanticCondition condition, SemanticCommand command) => condition switch
    {
        SemanticLogicalCondition logical => RequirementProperties(logical.Left, command).Concat(RequirementProperties(logical.Right, command)).Distinct(StringComparer.Ordinal),
        SemanticComparison comparison => command.Properties.Where(property => property.Id == comparison.Left.Property || property.Id == comparison.Right.Property).Select(property => property.Name),
        _ => ["unsupported condition"]
    };

    static void ValidateOptionalCommandConcepts(SemanticApplicationContext context, SemanticCommand command, List<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var property in command.Properties)
        {
            if (property.Type is { Kind: SemanticTypeReferenceKind.Concept, IsOptional: true } &&
                !context.Concepts[property.Type.Target].Validations.IsEmpty &&
                !HasNullPassingScalarConcept(context, property.Type))
            {
                var reason = property.Type.IsCollection || !context.Concepts[property.Type.Target].Values.IsEmpty
                    ? "only scalar concepts without named values are supported for optional validated properties. Collections and named-value concepts remain unsupported"
                    : "Arc graph validation skips null concepts and cannot preserve absence-rejecting concept rules. Only absence-rejecting rules block optional scalar concepts without named values";
                diagnostics.Add(CommandCapability(command, $"optional property '{property.Name}' uses validated concept '{context.Concepts[property.Type.Target].Name}'; {reason}. Make the property non-optional only if absence is not part of the contract, or keep this shape unsupported (Cratis/Stage#409)."));
            }

            if (property.Type.Kind == SemanticTypeReferenceKind.CompositeType && HasOptionalRequiredConcept(context, property.Type, []))
            {
                var nested = string.Join(", ", OptionalValidatedConceptProperties(context, property.Type, property.Name, []));
                diagnostics.Add(CommandCapability(command, $"composite property '{property.Name}' contains optional validated concept properties '{nested}'; Arc graph validation skips null concepts and cannot preserve the reference evaluator's nested concept rules. Make the nested validated concept non-optional only if absence is not part of the contract, or keep this shape unsupported (Cratis/Stage#409)."));
            }
        }
    }

    static bool HasNullPassingScalarConcept(SemanticApplicationContext context, SemanticTypeReference type) =>
        !type.IsCollection && context.Concepts[type.Target].Values.IsEmpty &&
        context.Concepts[type.Target].Validations.All(rule => rule.Kind is
            SemanticValidationRuleKind.Maximum or SemanticValidationRuleKind.Minimum or
            SemanticValidationRuleKind.Equal or SemanticValidationRuleKind.NotEqual or
            SemanticValidationRuleKind.GreaterThan or SemanticValidationRuleKind.GreaterThanOrEqual or
            SemanticValidationRuleKind.LessThan or SemanticValidationRuleKind.LessThanOrEqual or
            SemanticValidationRuleKind.Length or SemanticValidationRuleKind.Matches);

    static IEnumerable<string> OptionalValidatedConceptProperties(SemanticApplicationContext context, SemanticTypeReference type, string path, HashSet<SemanticId> visited)
    {
        if (type.Kind != SemanticTypeReferenceKind.CompositeType || !visited.Add(type.Target))
        {
            yield break;
        }

        foreach (var property in context.Types[type.Target].Properties)
        {
            var nestedPath = $"{path}.{property.Name}";
            if (property.Type is { Kind: SemanticTypeReferenceKind.Concept, IsOptional: true } && !context.Concepts[property.Type.Target].Validations.IsEmpty)
            {
                yield return $"{nestedPath} ({context.Concepts[property.Type.Target].Name})";
            }

            foreach (var nested in OptionalValidatedConceptProperties(context, property.Type, nestedPath, [.. visited]))
            {
                yield return nested;
            }
        }
    }

    static ArtifactRenderDiagnostic CommandCapability(SemanticCommand command, string reason) =>
        Error("STAGE-ESM-005", $"Command '{command.Name}' {reason}", command.Id);
}
