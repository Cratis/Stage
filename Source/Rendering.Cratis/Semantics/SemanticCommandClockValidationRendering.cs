// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders property rules against the one validated command receipt instant.
/// </summary>
internal static class SemanticCommandClockValidationRendering
{
    static readonly Dictionary<SemanticValidationRuleKind, string> _operators = new()
    {
        [SemanticValidationRuleKind.Equal] = "==",
        [SemanticValidationRuleKind.NotEqual] = "!=",
        [SemanticValidationRuleKind.GreaterThan] = ">",
        [SemanticValidationRuleKind.GreaterThanOrEqual] = ">=",
        [SemanticValidationRuleKind.LessThan] = "<",
        [SemanticValidationRuleKind.LessThanOrEqual] = "<="
    };

    internal static void Render(CSharpCodeBuilder builder, SemanticValidationRule rule, SemanticProperty property, SemanticApplicationContext context)
    {
        var type = new SemanticTypeSystem(context).Type(property.Type);
        var scalar = property.Type.Kind == SemanticTypeReferenceKind.Concept || property.Type.IsOptional ? "value.Value" : "value";
        var absent = property.Type.IsOptional ? "value is null || " : string.Empty;
        var clock = SemanticCommandClockRendering.Operand(rule.ClockOperand!, SemanticCommandClockRendering.ValidationInstant(context));
        builder.OpenBlock($"RuleFor<{type}>(_ => _.{context.MemberNames.Property(property)}).Must((command, value, validation) =>")
            .Line($"var operand = {clock};")
            .Line("validation.MessageFormatter.AppendArgument(\"ClockOperand\", operand.UtcDateTime.ToString(\"O\", global::System.Globalization.CultureInfo.InvariantCulture));")
            .Line($"return {absent}{scalar}.CompareTo(operand) {_operators[rule.Kind]} 0;")
            .EndBlock();
        var message = rule.Message ?? SemanticValidationRendering.DefaultMessage(rule with { Operand = SemanticValue.Text("{ClockOperand}") }, SemanticPrimitiveType.DateTime, false);
        var localized = message.StartsWith("$strings.", StringComparison.Ordinal);
        var renderedMessage = localized
            ? $"_ => global::{context.RootNamespace}.GeneratedStrings.Resolve({CSharpCodeBuilder.StringLiteral(message)})"
            : CSharpCodeBuilder.StringLiteral(message);
        var state = localized ? $".WithState({CSharpCodeBuilder.StringLiteral(message)})" : string.Empty;
        builder.Line($").WithMessage({renderedMessage}){state}{Severity(rule.Severity)};");
    }

    static string Severity(SemanticValidationSeverity severity) => severity switch
    {
        SemanticValidationSeverity.Error => string.Empty,
        SemanticValidationSeverity.Warning => ".WithSeverity(global::Cratis.Arc.Validation.ValidationResultSeverity.Warning)",
        SemanticValidationSeverity.Information => ".WithSeverity(global::Cratis.Arc.Validation.ValidationResultSeverity.Information)",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticValidationSeverity), severity)
    };
}
