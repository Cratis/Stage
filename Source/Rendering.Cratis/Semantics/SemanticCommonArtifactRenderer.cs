// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Cratis.Stage.Rendering.Cratis.Semantics.Validation;

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
    /// <param name="qualified">Whether specifications request the pre-short-name spelling oracle.</param>
    /// <param name="declarationOnly">Whether to omit compliance annotations, factory implementations and the companion validator for body analysis.</param>
    /// <returns>The generated source artifact.</returns>
    public static RenderedFile Render(SemanticConcept concept, SemanticApplicationContext context, bool qualified = false, bool declarationOnly = false)
    {
        var name = Identifiers.ToPascalCase(concept.Name);
        var builder = Builder(context, qualified);
        builder.Pin(name, $"{name}Validator");
        builder.Scope([], ["Value", "NotSet", "New", "context", "_", "value", "pattern"]);
        builder.Access("Value", "Empty", "MinValue", "RuleFor", "Must", "WithMessage", "WithState", "WithSeverity", "IsMatch", "FromSeconds");
        if (!concept.Values.IsEmpty)
        {
            RenderEnum(builder, concept, name);
        }
        else
        {
            RenderConcept(builder, concept, name, context, qualified, declarationOnly);
        }

        return new(Path.Combine(context.CommonFolder, $"{name}.cs"), builder.ToString()) { Sources = [concept.Id] };
    }

    /// <summary>
    /// Renders one composite type.
    /// </summary>
    /// <param name="type">The semantic composite type.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="qualified">Whether specifications request the pre-short-name spelling oracle.</param>
    /// <returns>The generated source artifact.</returns>
    public static RenderedFile Render(SemanticCompositeType type, SemanticApplicationContext context, bool qualified = false)
    {
        var builder = Builder(context, qualified);
        var types = new SemanticTypeSystem(context, builder);
        var name = Identifiers.ToPascalCase(type.Name);
        builder.Pin(name);
        builder.Scope([], type.Properties.Select(property => Identifiers.ToPascalCase(property.Name)));
        var parameters = string.Join(", ", type.Properties.Select(_ => $"{types.Type(_.Type)} {Identifiers.ToPascalCase(_.Name)}"));
        context.Docs(type.Id).Render(builder, $"Represents {Identifiers.ToWords(type.Name)}.")
            .Line($"public record {name}({parameters});");
        return new(Path.Combine(context.CommonFolder, $"{name}.cs"), builder.ToString()) { Sources = [type.Id] };
    }

    internal static bool IsGeneratable(SemanticConcept concept, SemanticApplicationContext context) =>
        NeedsFactory(concept, context) && (context.Request.Model.SemanticVersion.IsAtLeast(SemanticVersion.V15)
            ? SemanticGenerationShapes.Supported(concept) : concept.Primitive == SemanticPrimitiveType.Uuid);

    static CSharpCodeBuilder Builder(SemanticApplicationContext context, bool qualified) =>
        (qualified ? new CSharpCodeBuilder() : new(new ImportRequest(context.CommonNamespace, GeneratedProjects.Application, context.GeneratedTypes, [])))
            .Namespace(context.CommonNamespace);

    static void RenderEnum(CSharpCodeBuilder builder, SemanticConcept concept, string name)
    {
        builder.Summary($"Represents the possible values of {name}.").OpenBlock($"public enum {name}");
        foreach (var member in SemanticEnumMembers.Declarations(concept))
        {
            builder.Line($"{member},");
        }

        builder.EndBlock();
    }

    static void RenderConcept(CSharpCodeBuilder builder, SemanticConcept concept, string name, SemanticApplicationContext context, bool qualified, bool declarationOnly)
    {
        var types = new SemanticTypeSystem(context, builder);
        var isIdentifier = context.IdentifierConcepts.Contains(concept.Id);
        var primitive = types.Primitive(concept.Primitive);

        // These original type-only imports belong to the byte oracle, not to the extension-method scope.
        if (qualified)
        {
            builder.Using("Cratis.Concepts");
            if (isIdentifier) builder.Using("Cratis.Chronicle.Events");
        }

        builder.Inherits(isIdentifier ? "Cratis.Chronicle.Events.EventSourceId`1" : "Cratis.Concepts.ConceptAs`1");
        var baseType = isIdentifier ? $"{builder.Type(new("Cratis.Chronicle.Events", "EventSourceId", 1, ReferenceContext.Type))}<{primitive}>" : $"{builder.Type(new("Cratis.Concepts", "ConceptAs", 1, ReferenceContext.Type))}<{primitive}>";
        var generationInterface = GenerationInterface(concept, context, builder);
        builder.Summary($"Represents {Identifiers.ToWords(concept.Name)}.");
        if (!declarationOnly) ConceptComplianceRendering.Render(builder, concept.Compliance);
        builder.OpenBlock($"public record {name}({primitive} Value) : {baseType}(Value){generationInterface}")
            .Line($"public static readonly {name} NotSet = new({types.NotSet(concept.Primitive)});")
            .BlankLine();

        RenderFactory(builder, concept, context, declarationOnly);
        if (!isIdentifier)
        {
            builder.Line($"public static implicit operator {primitive}({name} value) => value.Value;");
        }

        builder.Line($"public static implicit operator {name}({primitive} value) => new(value);");
        if (!isIdentifier)
        {
            foreach (var source in SemanticProjectionConversions.Into(concept, context))
            {
                builder.Line($"public static implicit operator {name}({types.CommonType(source.Name)} value) => new(value.Value);");
            }
        }

        builder.EndBlock();
        if (!declarationOnly) RenderValidator(builder, concept, name, context);
    }

    static bool NeedsFactory(SemanticConcept concept, SemanticApplicationContext context) =>
        context.IdentifierConcepts.Contains(concept.Id) || context.Commands.Values.Any(command =>
            command.Properties.Any(property => property.IsGenerated && property.Type.Target == concept.Id));

    static string GenerationInterface(SemanticConcept concept, SemanticApplicationContext context, CSharpCodeBuilder builder) =>
        IsGeneratable(concept, context) ? $", {GeneratableType(concept, context, builder)}" : string.Empty;

    static string GeneratableType(SemanticConcept concept, SemanticApplicationContext context, CSharpCodeBuilder builder) =>
        $"{builder.Type(new("Cratis.Concepts", UsesGenerationContext(concept, context) ? "IGeneratableInContext" : "IGeneratable", 1, ReferenceContext.Type))}<{Identifiers.ToPascalCase(concept.Name)}>";

    static bool UsesGenerationContext(SemanticConcept concept, SemanticApplicationContext context) =>
        IsGeneratable(concept, context) && context.Request.Model.SemanticVersion.IsAtLeast(SemanticVersion.V15) && SemanticGenerationRendering.UsesContext(concept.Generation);

    static void RenderFactory(CSharpCodeBuilder builder, SemanticConcept concept, SemanticApplicationContext context, bool declarationOnly)
    {
        if (!NeedsFactory(concept, context)) return;
        var usesContext = UsesGenerationContext(concept, context);
        var value = string.Empty;
        if (!declarationOnly)
        {
            value = IsGeneratable(concept, context) ? SemanticGenerationRendering.Factory(concept, usesContext ? "context" : null, builder) : new SemanticTypeSystem(context, builder).NotSet(concept.Primitive);
        }
        var parameters = usesContext ? $"{builder.Type(new("Cratis.Concepts", "GenerationContext", 0, ReferenceContext.Type))} context" : string.Empty;
        var name = Identifiers.ToPascalCase(concept.Name);

        // A concept named New cannot declare a member named New (CS0542); Arc only reaches the factory through
        // IGeneratable, so the colliding factory implements the interface explicitly and the type keeps its name.
        var factory = name == "New" && IsGeneratable(concept, context) ? $"static {name} {GeneratableType(concept, context, builder)}.New" : $"public static {name} New";
        var implementation = declarationOnly ? "throw new global::System.NotImplementedException()" : $"new({value})";
        builder.Line($"{factory}({parameters}) => {implementation};").BlankLine();
        if (!declarationOnly) SemanticGenerationRendering.RenderBody(builder, concept, context);
    }

    static void RenderValidator(CSharpCodeBuilder builder, SemanticConcept concept, string name, SemanticApplicationContext context)
    {
        if (concept.Validations.IsEmpty)
        {
            return;
        }

        builder.Inherits("Cratis.Arc.Validation.ConceptValidator`1");
        builder.Using("Cratis.Arc.Validation")
            .BlankLine()
            .Summary($"Validates {name}.")
            .OpenBlock($"public class {name}Validator : {builder.Type(new("Cratis.Arc.Validation", "ConceptValidator", 1, ReferenceContext.Type))}<{name}>")
            .OpenBlock($"public {name}Validator()");
        foreach (var rule in concept.Validations)
        {
            if (SemanticValidationContextRuntime.IsBody(rule))
            {
                SemanticValidationContextRuntime.RenderRule(builder, context, rule, concept.Id, null);
            }
            else
            {
                SemanticValidationRendering.Render(builder, rule, "Value", concept.Primitive, false, true, rootNamespace: context.RootNamespace, typeBuilder: builder);
            }
        }

        builder.EndBlock();
        if (concept.Validations.Any(_ => _.Kind == SemanticValidationRuleKind.Matches))
        {
            SemanticValidationRendering.RenderMatchHelper(builder, builder);
        }

        builder.EndBlock();
    }
}
