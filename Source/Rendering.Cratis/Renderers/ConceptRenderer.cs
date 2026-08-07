// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Expressions;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Validation;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Renders a Screenplay <see cref="ConceptSyntax"/> into a C# <see langword="enum"/>, or a <c>ConceptAs&lt;T&gt;</c> /
/// <c>EventSourceId&lt;T&gt;</c> record paired with its <c>ConceptValidator&lt;T&gt;</c>.
/// </summary>
public static class ConceptRenderer
{
    static readonly Dictionary<string, string> _notSetLiterals = new(StringComparer.Ordinal)
    {
        ["Guid"] = "Guid.Empty",
        ["string"] = "string.Empty",
        ["int"] = "0",
        ["decimal"] = "0m",
        ["bool"] = "false",
        ["DateOnly"] = "DateOnly.MinValue",
        ["DateTimeOffset"] = "DateTimeOffset.MinValue",
    };

    /// <summary>
    /// Renders a concept into its generated file, placed at the folder level computed by
    /// <see cref="ApplicationSet.ConceptPlacements"/> — the lowest folder every slice using it can see.
    /// </summary>
    /// <param name="concept">The concept to render.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> the concept was declared in.</param>
    /// <param name="rootNamespace">The root namespace of the target application.</param>
    /// <returns>The <see cref="RenderedFile"/>.</returns>
    public static RenderedFile Render(ConceptSyntax concept, ApplicationSet applicationSet, string rootNamespace)
    {
        var typeName = Identifiers.ToPascalCase(concept.Name);
        var placement = applicationSet.ConceptPlacements.GetValueOrDefault(concept.Name, []);
        var folderSegments = placement.Count == 0 ? ["Common"] : SliceNaming.FolderPath(placement);
        var @namespace = ReferencedNamespaces.ForPlacement(rootNamespace, placement);

        var builder = new CSharpCodeBuilder().Namespace(@namespace);

        if (concept.IsEnum)
        {
            RenderEnum(builder, concept, typeName);
        }
        else
        {
            RenderConceptType(builder, concept, typeName, applicationSet);
        }

        var path = new List<string>(folderSegments) { $"{typeName}.cs" };
        return new RenderedFile(Path.Combine([.. path]), builder.ToString());
    }

    static void RenderEnum(CSharpCodeBuilder builder, ConceptSyntax concept, string typeName)
    {
        builder.Summary($"Represents the possible values of {typeName}.").OpenBlock($"public enum {typeName}");
        foreach (var value in concept.Values)
        {
            builder.Line($"{Identifiers.ToPascalCase(value)},");
        }

        builder.EndBlock();
    }

    static void RenderConceptType(CSharpCodeBuilder builder, ConceptSyntax concept, string typeName, ApplicationSet applicationSet)
    {
        var clrType = PrimitiveClrType(concept.Type);
        var isIdentifier = applicationSet.IdentifierConceptNames.Contains(concept.Name);
        var baseType = isIdentifier ? $"EventSourceId<{clrType}>" : $"ConceptAs<{clrType}>";

        builder.Using("Cratis.Concepts").Using("Cratis.Arc.Validation");
        if (isIdentifier)
        {
            builder.Using("Cratis.Chronicle.Events");
        }

        if (HasAttribute(concept, "pii") || HasAttribute(concept, "sensitive"))
        {
            builder.Using("Cratis.Chronicle.Compliance.GDPR").Attribute("PII");
        }

        builder.Summary($"Represents {typeName}.")
            .OpenBlock($"public record {typeName}({clrType} Value) : {baseType}(Value)")
            .Line($"public static readonly {typeName} NotSet = new({NotSetLiteral(clrType)});")
            .BlankLine();

        if (isIdentifier)
        {
            builder.Line($"public static {typeName} New() => new({NewValueExpression(clrType)});").BlankLine();
        }
        else
        {
            builder.Line($"public static implicit operator {clrType}({typeName} value) => value.Value;");
        }

        builder.Line($"public static implicit operator {typeName}({clrType} value) => new(value);").EndBlock();

        RenderValidator(builder, concept, typeName, clrType);
    }

    static void RenderValidator(CSharpCodeBuilder builder, ConceptSyntax concept, string typeName, string clrType)
    {
        var rules = (concept.Validations ?? []).OfType<DeclarativeValidateSyntax>().SelectMany(validate => validate.Rules).ToArray();
        var codeBlocks = (concept.Validations ?? []).OfType<CodeValidateSyntax>().ToArray();
        if (rules.Length == 0 && codeBlocks.Length == 0)
        {
            return;
        }

        var validatorName = $"{typeName}Validator";
        var ruleMethods = new List<(string Name, string Code)>();

        builder.BlankLine()
            .Summary($"Validates {typeName}.")
            .OpenBlock($"public class {validatorName} : ConceptValidator<{typeName}>")
            .OpenBlock($"public {validatorName}()");

        foreach (var rule in rules)
        {
            RenderRule(builder, rule, ruleMethods, clrType == "string");
        }

        var codeBlockIndex = 0;
        foreach (var codeBlock in codeBlocks)
        {
            codeBlockIndex++;
            var methodName = codeBlockIndex == 1 ? "IsValid" : $"IsValid{codeBlockIndex}";
            builder.Line($"RuleFor(_ => _.Value).Must({methodName});");
            ruleMethods.Add((methodName, codeBlock.Code.Code));
        }

        builder.EndBlock();

        foreach (var method in ruleMethods)
        {
            builder.BlankLine().OpenBlock($"static bool {method.Name}({clrType} Value)").Raw(method.Code).EndBlock();
        }

        builder.EndBlock();
    }

    static void RenderRule(CSharpCodeBuilder builder, ValidationRuleSyntax rule, List<(string Name, string Code)> ruleMethods, bool subjectIsText)
    {
        // A concept validator has exactly one value in scope — the concept's own. A rule value that is anything but
        // a literal names something outside that scope, and rendering it would emit an identifier bound to nothing.
        if (rule.Value is not null and not LiteralExpressionSyntax)
        {
            builder.Line($"// TODO: validation rule '{rule.Rule}' on '{rule.Property}' compares against a value outside the concept's scope");
            return;
        }

        var value = rule.Value is null ? string.Empty : ExpressionRenderer.Render(rule.Value);
        var call = rule.Rule == ValidationRuleKind.Rule && rule.Code is not null
            ? RenderCustomRule(rule, ruleMethods)
            : ValidationRuleRenderer.RenderCall(rule.Rule, value, subjectIsText);

        if (call is null)
        {
            builder.Line($"// TODO: unsupported validation rule '{rule.Rule}' on '{rule.Property}'");
            return;
        }

        builder.Line($"RuleFor(_ => _.Value){call}{ValidationRuleRenderer.RenderMessage(rule)};");
    }

    static string RenderCustomRule(ValidationRuleSyntax rule, List<(string Name, string Code)> ruleMethods)
    {
        // ValidationRuleSyntax carries no distinct name for a declared `rule <Name>` — only Property/Rule/Value/Message
        // survive parsing — so the generated predicate method is synthesized rather than reproducing the authored name.
        var methodName = ruleMethods.Count == 0 ? "SatisfyCustomRule" : $"SatisfyCustomRule{ruleMethods.Count + 1}";
        ruleMethods.Add((methodName, rule.Code!.Code));
        return $".Must({methodName})";
    }

    static bool HasAttribute(ConceptSyntax concept, string name) => concept.Attributes.Any(attribute => attribute.Name == name);

    static string PrimitiveClrType(string screenplayType) => screenplayType switch
    {
        "Uuid" => "Guid",
        "String" => "string",
        "Int" => "int",
        "Decimal" => "decimal",
        "Bool" => "bool",
        "Date" => "DateOnly",
        "DateTime" => "DateTimeOffset",
        _ => "string",
    };

    static string NotSetLiteral(string clrType) => _notSetLiterals.GetValueOrDefault(clrType, "default!");

    static string NewValueExpression(string clrType) => clrType == "Guid" ? "Guid.NewGuid()" : NotSetLiteral(clrType);
}
