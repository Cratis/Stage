// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Checks and emits the reference evaluator's declarative validation predicates.
/// </summary>
internal static class SemanticValidationRendering
{
    internal static bool CanRender(SemanticValidationRule rule) =>
        SafeMessage(rule.Message) && SafeOperand(rule) && !(rule.Operand is SemanticNullValue && rule.Kind is SemanticValidationRuleKind.Equal or SemanticValidationRuleKind.NotEqual) && rule.Severity == SemanticValidationSeverity.Error && rule.Kind is
            SemanticValidationRuleKind.NotEmpty or SemanticValidationRuleKind.Maximum or SemanticValidationRuleKind.Minimum or
            SemanticValidationRuleKind.Equal or SemanticValidationRuleKind.NotEqual or SemanticValidationRuleKind.GreaterThan or
            SemanticValidationRuleKind.GreaterThanOrEqual or SemanticValidationRuleKind.LessThan or SemanticValidationRuleKind.LessThanOrEqual or
            SemanticValidationRuleKind.Length or SemanticValidationRuleKind.AllGreaterThan or SemanticValidationRuleKind.AllGreaterThanOrEqual or
            SemanticValidationRuleKind.Matches;

    internal static bool SafeMessage(string? message) => message is null ||
        (!message.StartsWith("$strings.", StringComparison.Ordinal) && !message.Any(_ => char.IsControl(_) || char.IsSurrogate(_) || (_ is '{' or '}')));

    internal static bool CanResolve(string? message, SemanticApplicationContext context) =>
        SafeMessage(message) || (message?.StartsWith("$strings.", StringComparison.Ordinal) == true && context.Strings is not null);

    internal static bool CanRender(SemanticValidationRule rule, SemanticApplicationContext context) =>
        (CanRender(rule) || (rule.Message?.StartsWith("$strings.", StringComparison.Ordinal) == true &&
        CanRender(rule with { Message = "Localized validation message" }))) && CanResolve(rule.Message, context);

    internal static bool SafeOperand(SemanticValidationRule rule) => rule.Operand is not SemanticTextValue text ||
        (!text.Value.Any(_ => char.IsControl(_) || char.IsSurrogate(_)) && (rule.Message is not null || rule.Kind is not (SemanticValidationRuleKind.Equal or SemanticValidationRuleKind.NotEqual) ||
        !text.Value.Any(_ => _ is '{' or '}')));

    internal static void Render(CSharpCodeBuilder builder, SemanticValidationRule rule, string property, SemanticPrimitiveType primitive, bool collection, bool concept, bool wrapped = false, bool optional = false, string? rootNamespace = null)
    {
        var value = concept ? "_.Value" : $"_.{Identifiers.ToPascalCase(property)}";
        var predicate = Predicate(rule, primitive, collection, wrapped && collection, optional);
        var message = rule.Message ?? DefaultMessage(rule, primitive, concept);
        if (message.StartsWith("$strings.", StringComparison.Ordinal))
        {
            builder.Line($"RuleFor(_ => {value}).Must(value => {predicate}).WithMessage(_ => {rootNamespace}.GeneratedStrings.Resolve({CSharpCodeBuilder.StringLiteral(message)})).WithState({CSharpCodeBuilder.StringLiteral(message)});");
        }
        else
        {
            builder.Line($"RuleFor(_ => {value}).Must(value => {predicate}).WithMessage({CSharpCodeBuilder.StringLiteral(message)});");
        }
    }

    internal static SemanticPrimitiveType UnderlyingPrimitive(SemanticTypeReference type, SemanticApplicationContext context) =>
        type.Kind == SemanticTypeReferenceKind.Concept ? context.Concepts[type.Target].Primitive : type.Primitive;

    internal static void RenderMatchHelper(CSharpCodeBuilder builder)
    {
        builder.BlankLine()
            .OpenBlock("static bool MatchesPattern(string value, string pattern)")
            .OpenBlock("try")
            .Line("return System.Text.RegularExpressions.Regex.IsMatch(value, pattern, System.Text.RegularExpressions.RegexOptions.ECMAScript | System.Text.RegularExpressions.RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));")
            .EndBlock()
            .OpenBlock("catch (System.Text.RegularExpressions.RegexMatchTimeoutException)")
            .Line("return false;")
            .EndBlock()
            .EndBlock();
    }

    static string Predicate(SemanticValidationRule rule, SemanticPrimitiveType primitive, bool collection, bool wrapped, bool optional)
    {
        var operand = rule.Operand;
        var number = operand is SemanticNumberValue numeric ? numeric.Value.ToString(CultureInfo.InvariantCulture) + "m" : string.Empty;
        var equality = operand switch
        {
            SemanticNullValue => "null",
            SemanticTextValue text => CSharpCodeBuilder.StringLiteral(text.Value),
            SemanticBooleanValue boolean => boolean.Value ? "true" : "false",
            SemanticNumberValue => number,
            _ => string.Empty
        };
        var scalar = wrapped ? "value.Value" : "value";
        var absentPasses = wrapped || optional || collection || primitive == SemanticPrimitiveType.Text ? "value is null || " : string.Empty;
        var element = wrapped ? "element.Value" : "element";
        var measured = primitive == SemanticPrimitiveType.Text && !collection ? $"{scalar}.Length" : scalar;
        var comparison = rule.Kind switch
        {
            SemanticValidationRuleKind.Maximum or SemanticValidationRuleKind.LessThanOrEqual => "<=",
            SemanticValidationRuleKind.Minimum or SemanticValidationRuleKind.GreaterThanOrEqual or SemanticValidationRuleKind.AllGreaterThanOrEqual => ">=",
            SemanticValidationRuleKind.GreaterThan or SemanticValidationRuleKind.AllGreaterThan => ">",
            SemanticValidationRuleKind.LessThan => "<",
            _ => string.Empty
        };
        return rule.Kind switch
        {
            SemanticValidationRuleKind.NotEmpty when collection => "value is { Count: > 0 }",
            SemanticValidationRuleKind.NotEmpty => wrapped ? "value is not null && !string.IsNullOrEmpty(value.Value)" : "!string.IsNullOrEmpty(value)",
            SemanticValidationRuleKind.Equal when primitive is SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber => $"{absentPasses}{scalar} == {number}",
            SemanticValidationRuleKind.NotEqual when primitive is SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber => $"{absentPasses}{scalar} != {number}",
            SemanticValidationRuleKind.Equal => $"{absentPasses}object.Equals({scalar}, {equality})",
            SemanticValidationRuleKind.NotEqual => $"{absentPasses}!object.Equals({scalar}, {equality})",
            SemanticValidationRuleKind.Length => $"{absentPasses}{scalar}.Length == {number}",
            SemanticValidationRuleKind.Matches => $"{absentPasses}MatchesPattern({scalar}, {equality})",
            SemanticValidationRuleKind.AllGreaterThan or SemanticValidationRuleKind.AllGreaterThanOrEqual => $"{absentPasses}value.All(element => {element} {comparison} {number})",
            _ => $"{absentPasses}{measured} {comparison} {number}"
        };
    }

    static string DefaultMessage(SemanticValidationRule rule, SemanticPrimitiveType primitive, bool concept)
    {
        var operand = rule.Operand switch
        {
            SemanticTextValue text => $"'{text.Value}'",
            SemanticNumberValue number => number.Value.ToString(CultureInfo.InvariantCulture),
            SemanticBooleanValue boolean => boolean.Value ? "true" : "false",
            _ => "null"
        };
        return rule.Kind switch
        {
            SemanticValidationRuleKind.NotEmpty => concept ? "A required concept value is empty." : "A required value is empty.",
            SemanticValidationRuleKind.Maximum when primitive == SemanticPrimitiveType.Text => $"A value must be at most {operand} characters long.",
            SemanticValidationRuleKind.Minimum when primitive == SemanticPrimitiveType.Text => $"A value must be at least {operand} characters long.",
            SemanticValidationRuleKind.Maximum or SemanticValidationRuleKind.LessThanOrEqual => $"A value must be at most {operand}.",
            SemanticValidationRuleKind.Minimum or SemanticValidationRuleKind.GreaterThanOrEqual => $"A value must be at least {operand}.",
            SemanticValidationRuleKind.Equal => $"A value must equal {operand}.",
            SemanticValidationRuleKind.NotEqual => $"A value must not equal {operand}.",
            SemanticValidationRuleKind.GreaterThan => $"A value must be greater than {operand}.",
            SemanticValidationRuleKind.LessThan => $"A value must be less than {operand}.",
            SemanticValidationRuleKind.Length => $"A value must be exactly {operand} characters long.",
            SemanticValidationRuleKind.Matches => "A value must match the required pattern.",
            SemanticValidationRuleKind.AllGreaterThan => $"Every value must be greater than {operand}.",
            SemanticValidationRuleKind.AllGreaterThanOrEqual => $"Every value must be at least {operand}.",
            _ => string.Empty
        };
    }
}
