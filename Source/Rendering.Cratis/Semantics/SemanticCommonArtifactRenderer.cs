// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Renderers;

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
        var builder = new CSharpCodeBuilder().Namespace(context.CommonNamespace);
        if (!concept.Values.IsEmpty)
        {
            RenderEnum(builder, concept, name);
        }
        else
        {
            RenderConcept(builder, concept, name, context);
        }

        return new(Path.Combine(context.CommonFolder, $"{name}.cs"), builder.ToString()) { Sources = [concept.Id] };
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
            .Namespace(context.CommonNamespace);
        context.Docs(type.Id).Render(builder, $"Represents {Identifiers.ToWords(type.Name)}.")
            .Line($"public record {name}({parameters});");
        return new(Path.Combine(context.CommonFolder, $"{name}.cs"), builder.ToString()) { Sources = [type.Id] };
    }

    static void RenderEnum(CSharpCodeBuilder builder, SemanticConcept concept, string name)
    {
        builder.Summary($"Represents the possible values of {name}.").OpenBlock($"public enum {name}");
        foreach (var member in SemanticEnumMembers.Declarations(concept))
        {
            builder.Line($"{member},");
        }

        builder.EndBlock();
    }

    static void RenderConcept(CSharpCodeBuilder builder, SemanticConcept concept, string name, SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        var isIdentifier = context.IdentifierConcepts.Contains(concept.Id);
        var primitive = types.Primitive(concept.Primitive);
        builder.Using("Cratis.Concepts");
        if (isIdentifier)
        {
            builder.Using("Cratis.Chronicle.Events");
        }

        var baseType = isIdentifier ? $"global::Cratis.Chronicle.Events.EventSourceId<{primitive}>" : $"global::Cratis.Concepts.ConceptAs<{primitive}>";
        var generationInterface = GenerationInterface(concept, context);
        builder.Summary($"Represents {Identifiers.ToWords(concept.Name)}.");
        ConceptComplianceRendering.Render(builder, concept.Compliance);
        builder.OpenBlock($"public record {name}({primitive} Value) : {baseType}(Value){generationInterface}")
            .Line($"public static readonly {name} NotSet = new({types.NotSet(concept.Primitive)});")
            .BlankLine();

        RenderFactory(builder, concept, context);
        if (!isIdentifier)
        {
            builder.Line($"public static implicit operator {primitive}({name} value) => value.Value;");
        }

        builder.Line($"public static implicit operator {name}({primitive} value) => new(value);").EndBlock();
        RenderValidator(builder, concept, name, context.RootNamespace);
    }

    static bool NeedsFactory(SemanticConcept concept, SemanticApplicationContext context) =>
        context.IdentifierConcepts.Contains(concept.Id) || context.Commands.Values.Any(command =>
            command.Properties.Any(property => property.IsGenerated && property.Type.Target == concept.Id));

    static bool IsGeneratable(SemanticConcept concept, SemanticApplicationContext context) =>
        concept.Primitive == SemanticPrimitiveType.Uuid && NeedsFactory(concept, context);

    static string GenerationInterface(SemanticConcept concept, SemanticApplicationContext context) =>
        IsGeneratable(concept, context) ? $", global::Cratis.Concepts.IGeneratable<{Identifiers.ToPascalCase(concept.Name)}>" : string.Empty;

    static void RenderFactory(CSharpCodeBuilder builder, SemanticConcept concept, SemanticApplicationContext context)
    {
        if (!NeedsFactory(concept, context)) return;
        var value = IsGeneratable(concept, context) ? "global::Cratis.Concepts.GenerateValue.Uuid()" : new SemanticTypeSystem(context).NotSet(concept.Primitive);
        builder.Line($"public static {Identifiers.ToPascalCase(concept.Name)} New() => new({value});").BlankLine();
    }

    static void RenderValidator(CSharpCodeBuilder builder, SemanticConcept concept, string name, string rootNamespace)
    {
        if (concept.Validations.IsEmpty)
        {
            return;
        }

        builder.Using("Cratis.Arc.Validation")
            .BlankLine()
            .Summary($"Validates {name}.")
            .OpenBlock($"public class {name}Validator : global::Cratis.Arc.Validation.ConceptValidator<{name}>")
            .OpenBlock($"public {name}Validator()");
        foreach (var rule in concept.Validations)
        {
            SemanticValidationRendering.Render(builder, rule, "Value", concept.Primitive, false, true, rootNamespace: rootNamespace);
        }

        builder.EndBlock();
        if (concept.Validations.Any(_ => _.Kind == SemanticValidationRuleKind.Matches))
        {
            SemanticValidationRendering.RenderMatchHelper(builder);
        }

        builder.EndBlock();
    }
}
