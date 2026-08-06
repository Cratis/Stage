// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Expressions;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Types;
using Cratis.Stage.Rendering.Cratis.Validation;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Renders the <c>CommandValidator&lt;T&gt;</c> paired with a <c>[Command]</c>.
/// </summary>
public static class CommandValidatorRenderer
{
    /// <summary>
    /// Renders the validator, or nothing when the command declares no declarative rules.
    /// </summary>
    /// <param name="builder">The <see cref="CSharpCodeBuilder"/> to render into.</param>
    /// <param name="command">The command to render the validator for.</param>
    /// <param name="typeName">The rendered command type name.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to resolve property types against.</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    public static void Render(
        CSharpCodeBuilder builder, CommandSyntax command, string typeName, ApplicationSet applicationSet, ICollection<string> diagnostics)
    {
        var rules = command.Validations.OfType<DeclarativeValidateSyntax>().SelectMany(validate => validate.Rules).ToArray();
        if (rules.Length == 0)
        {
            return;
        }

        var validatorName = $"{typeName}Validator";
        var ruleMethods = new List<(string Name, string ParameterType, string PropertyName, string Code)>();

        builder.BlankLine().OpenBlock($"public class {validatorName} : CommandValidator<{typeName}>").OpenBlock($"public {validatorName}()");

        foreach (var rule in rules)
        {
            RenderRule(builder, rule, command, applicationSet, ruleMethods, diagnostics);
        }

        builder.EndBlock();

        foreach (var method in ruleMethods)
        {
            builder.BlankLine().OpenBlock($"static bool {method.Name}({method.ParameterType} {method.PropertyName})").Raw(method.Code).EndBlock();
        }

        builder.EndBlock();
    }

    static void RenderRule(
        CSharpCodeBuilder builder,
        ValidationRuleSyntax rule,
        CommandSyntax command,
        ApplicationSet applicationSet,
        List<(string Name, string ParameterType, string PropertyName, string Code)> ruleMethods,
        ICollection<string> diagnostics)
    {
        var property = Identifiers.ToPascalCase(rule.Property);
        var subject = Property(rule.Property, command);
        var value = RenderValue(rule, subject, command, applicationSet, diagnostics);

        if (value is null)
        {
            builder.Line($"// TODO: validation rule '{rule.Rule}' on '{rule.Property}' compares against a value the command does not carry");
            return;
        }

        var call = rule.Rule == ValidationRuleKind.Rule && rule.Code is not null
            ? RenderCustomRule(rule, property, subject, applicationSet, ruleMethods)
            : ValidationRuleRenderer.RenderCall(rule.Rule, value, SubjectIsText(subject, applicationSet));

        if (call is null)
        {
            builder.Line($"// TODO: unsupported validation rule '{rule.Rule}' on '{rule.Property}'");
            return;
        }

        builder.Line($"RuleFor(_ => _.{property}){call}{ValidationRuleRenderer.RenderMessage(rule)};");
    }

    /// <summary>
    /// Renders a rule's comparison value. A value naming another command property becomes the member-access lambda
    /// FluentValidation expects — the bare identifier is not in scope inside the validator's constructor — and a
    /// string literal compared against an enum-typed property becomes the enum member.
    /// </summary>
    /// <param name="rule">The rule to render the value for.</param>
    /// <param name="subject">The command property being validated, when it resolves.</param>
    /// <param name="command">The command the rule belongs to.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to resolve property types against.</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    /// <returns>The rendered value, or <see langword="null"/> when it names nothing the command carries.</returns>
    static string? RenderValue(
        ValidationRuleSyntax rule,
        PropertySyntax? subject,
        CommandSyntax command,
        ApplicationSet applicationSet,
        ICollection<string> diagnostics)
    {
        switch (rule.Value)
        {
            case null:
                return string.Empty;

            case PathExpressionSyntax path when Property(path.Path, command) is not null:
                return $"_ => _.{Identifiers.ToPascalCase(path.Path)}";

            case PathExpressionSyntax path:
                diagnostics.Add($"Validation rule on '{rule.Property}' compares against '{path.Path}', which command '{command.Name}' does not carry — the rule is not rendered.");
                return null;

            case LiteralExpressionSyntax { Value: string text } when subject is not null && EnumTypeName(subject, applicationSet) is { } enumName:
                return $"{enumName}.{Identifiers.ToPascalCase(text)}";

            default:
                return ExpressionRenderer.Render(rule.Value);
        }
    }

    static string RenderCustomRule(
        ValidationRuleSyntax rule,
        string property,
        PropertySyntax? subject,
        ApplicationSet applicationSet,
        List<(string Name, string ParameterType, string PropertyName, string Code)> ruleMethods)
    {
        var parameterType = subject is null ? "string" : TypeResolver.Resolve(subject.Type, applicationSet).ToTypeSyntax();
        var methodName = $"Satisfy{property}Rule{ruleMethods.Count(method => method.Name.StartsWith($"Satisfy{property}Rule", StringComparison.Ordinal)) + 1}";
        ruleMethods.Add((methodName, parameterType, property, rule.Code!.Code));
        return $".Must({methodName})";
    }

    static PropertySyntax? Property(string name, CommandSyntax command) =>
        command.Properties.FirstOrDefault(candidate => string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));

    static string? EnumTypeName(PropertySyntax property, ApplicationSet applicationSet)
    {
        var resolved = TypeResolver.Resolve(property.Type, applicationSet);
        return resolved.Kind == ResolvedTypeKind.Enum ? resolved.ClrTypeName : null;
    }

    static bool SubjectIsText(PropertySyntax? subject, ApplicationSet applicationSet) =>
        subject is not null && TypeResolver.Resolve(subject.Type, applicationSet).ClrTypeName == "string";
}
