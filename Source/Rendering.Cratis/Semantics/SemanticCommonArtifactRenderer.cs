// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders application concepts and composite types directly from ESM.
/// </summary>
internal static class SemanticCommonArtifactRenderer
{
    /// <summary>
    /// Renders one concept.
    /// </summary>
    /// <param name="concept">The semantic concept.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The generated source artifact.</returns>
    public static RenderedFile Render(SemanticConcept concept, SemanticApplicationContext context)
    {
        var name = Identifiers.ToPascalCase(concept.Name);
        var builder = new CSharpCodeBuilder().Namespace($"{context.RootNamespace}.Common");
        if (!concept.Values.IsEmpty)
        {
            RenderEnum(builder, concept, name);
        }
        else
        {
            RenderConcept(builder, concept, name, context.IdentifierConcepts.Contains(concept.Id));
        }

        return new(Path.Combine("Common", $"{name}.cs"), builder.ToString());
    }

    /// <summary>
    /// Renders one composite type.
    /// </summary>
    /// <param name="type">The semantic composite type.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The generated source artifact.</returns>
    public static RenderedFile Render(SemanticCompositeType type, SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        var name = Identifiers.ToPascalCase(type.Name);
        var parameters = string.Join(", ", type.Properties.Select(_ => $"{types.Type(_.Type)} {Identifiers.ToPascalCase(_.Name)}"));
        var builder = new CSharpCodeBuilder()
            .Namespace($"{context.RootNamespace}.Common")
            .Summary($"Represents {Identifiers.ToWords(type.Name)}.")
            .Line($"public record {name}({parameters});");
        return new(Path.Combine("Common", $"{name}.cs"), builder.ToString());
    }

    static void RenderEnum(CSharpCodeBuilder builder, SemanticConcept concept, string name)
    {
        builder.Summary($"Represents the possible values of {name}.").OpenBlock($"public enum {name}");
        foreach (var value in concept.Values)
        {
            builder.Line($"{Identifiers.ToPascalCase(value)},");
        }

        builder.EndBlock();
    }

    static void RenderConcept(CSharpCodeBuilder builder, SemanticConcept concept, string name, bool isIdentifier)
    {
        var primitive = SemanticTypeSystem.Primitive(concept.Primitive);
        builder.Using("Cratis.Concepts");
        if (isIdentifier)
        {
            builder.Using("Cratis.Chronicle.Events");
        }

        var baseType = isIdentifier ? $"EventSourceId<{primitive}>" : $"ConceptAs<{primitive}>";
        builder.Summary($"Represents {Identifiers.ToWords(concept.Name)}.")
            .OpenBlock($"public record {name}({primitive} Value) : {baseType}(Value)")
            .Line($"public static readonly {name} NotSet = new({SemanticTypeSystem.NotSet(concept.Primitive)});")
            .BlankLine();

        if (isIdentifier)
        {
            var value = concept.Primitive == SemanticPrimitiveType.Uuid ? "Guid.NewGuid()" : SemanticTypeSystem.NotSet(concept.Primitive);
            builder.Line($"public static {name} New() => new({value});").BlankLine();
        }
        else
        {
            builder.Line($"public static implicit operator {primitive}({name} value) => value.Value;");
        }

        builder.Line($"public static implicit operator {name}({primitive} value) => new(value);").EndBlock();
        RenderValidator(builder, concept, name);
    }

    static void RenderValidator(CSharpCodeBuilder builder, SemanticConcept concept, string name)
    {
        if (concept.Validations.IsEmpty)
        {
            return;
        }

        builder.Using("Cratis.Arc.Validation")
            .BlankLine()
            .Summary($"Validates {name}.")
            .OpenBlock($"public class {name}Validator : ConceptValidator<{name}>")
            .OpenBlock($"public {name}Validator()");
        foreach (var rule in concept.Validations)
        {
            var message = string.IsNullOrWhiteSpace(rule.Message)
                ? string.Empty
                : $".WithMessage({CSharpCodeBuilder.StringLiteral(rule.Message)})";
            builder.Line($"RuleFor(_ => _.Value).NotEmpty(){message};");
        }

        builder.EndBlock().EndBlock();
    }
}
