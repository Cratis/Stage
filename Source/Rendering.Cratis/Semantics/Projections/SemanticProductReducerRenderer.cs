// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Lowers an admitted projection that adds or subtracts a product of two values (ESM v29) to a Chronicle reducer.
/// </summary>
/// <remarks>
/// The reducer reproduces the reference evaluator: a missing target counts as zero, the product is taken exactly in
/// <see cref="decimal"/>, and a whole-number target rounds both operands half to even before the operation, as Chronicle
/// converts an operand for an integral target. Mappings apply in model order, and a removal deletes the instance.
/// </remarks>
internal static class SemanticProductReducerRenderer
{
    const string State = "state";

    /// <summary>
    /// Renders the reducer that replaces the projection definition.
    /// </summary>
    /// <param name="projection">The admitted semantic projection.</param>
    /// <param name="readModel">The target read model.</param>
    /// <param name="scope">The projection scope.</param>
    /// <param name="context">The semantic application.</param>
    /// <returns>The C# declaration.</returns>
    public static string Render(SemanticProjection projection, SemanticReadModel readModel, SemanticProjectionScope scope, SemanticApplicationContext context)
    {
        var builder = new CSharpCodeBuilder();
        var types = new SemanticTypeSystem(context, builder);
        var modelName = Identifiers.ToPascalCase(readModel.Name);
        var projectionName = Identifiers.ToPascalCase(projection.Name);
        var implementationName = projectionName == modelName ? $"{projectionName}Projection" : projectionName;
        builder.Summary($"Reduces events onto {modelName}; Chronicle projections cannot add or subtract a product of two values.")
            .OpenBlock($"public class {implementationName} : global::Cratis.Chronicle.Reducers.IReducerFor<{modelName}>");
        foreach (var from in scope.From)
        {
            RenderFrom(builder, from, readModel, modelName, types, context);
        }

        foreach (var removal in scope.Removals)
        {
            Method(builder, types.EventType(context.Events[removal.EventContract]), modelName, "Removes the instance.")
                .Line("return null;")
                .EndBlock();
        }

        builder.EndBlock();
        var source = builder.ToString();
        return source[source.IndexOf("/// <summary>", StringComparison.Ordinal)..].TrimEnd();
    }

    static CSharpCodeBuilder Method(CSharpCodeBuilder builder, string eventType, string modelName, string summary) =>
        builder.BlankLine().Summary(summary)
            .Line("/// <param name=\"event\">The event.</param>")
            .Line("/// <param name=\"current\">The current state, or <see langword=\"null\"/> when there is none.</param>")
            .Line("/// <param name=\"eventContext\">The event context.</param>")
            .Line("/// <returns>The next state.</returns>")
            .OpenBlock($"public {modelName}? On({eventType} @event, {modelName}? current, global::Cratis.Chronicle.Events.EventContext eventContext)");

    static void RenderFrom(CSharpCodeBuilder builder, SemanticProjectionFrom from, SemanticReadModel readModel, string modelName, SemanticTypeSystem types, SemanticApplicationContext context)
    {
        var @event = context.Events[from.EventContract];
        var identifier = readModel.Properties.Single(property => property.IsIdentifier);
        var arguments = SemanticStateViewArtifactRenderer.OrderedProperties(readModel.Properties).Select(property =>
            $"{Identifiers.ToPascalCase(property.Name)}: {(property.IsIdentifier ? Identity(identifier.Type, types, context) : "default!")}");
        Method(builder, types.EventType(@event), modelName, $"Applies {Identifiers.ToPascalCase(@event.Name)}.")
            .Line($"var {State} = current ?? new {modelName}({string.Join(", ", arguments)});");
        var targets = readModel.Properties.ToDictionary(property => property.Id);
        foreach (var mapping in from.Mappings)
        {
            var target = targets[mapping.Target[0]];
            builder.Line($"{State} = {State} with {{ {Identifiers.ToPascalCase(target.Name)} = {Value(mapping, target, @event, identifier, types, context)} }};");
        }

        builder.Line($"return {State};").EndBlock();
    }

    static string Value(SemanticProjectionMapping mapping, SemanticProperty target, SemanticEventContract @event, SemanticProperty identifier, SemanticTypeSystem types, SemanticApplicationContext context) =>
        mapping.Operation switch
        {
            SemanticProjectionOperation.Set => mapping.Source switch
            {
                SemanticProjectionEventProperty property => $"@event.{EventPropertyName(property, @event)}",
                SemanticProjectionLiteral literal => types.Value(literal.Value, target.Type),
                _ => Identity(identifier.Type, types, context)
            },
            SemanticProjectionOperation.Clear => "null",
            _ => Arithmetic(mapping, target, @event, types, context)
        };

    // The reference seeds a missing target with zero, takes the product exactly, then rounds both sides for a whole target.
    static string Arithmetic(SemanticProjectionMapping mapping, SemanticProperty target, SemanticEventContract @event, SemanticTypeSystem types, SemanticApplicationContext context)
    {
        var primitive = SemanticProductReducerAdmission.NumberPrimitive(target.Type, context)!.Value;
        var whole = primitive == SemanticPrimitiveType.WholeNumber;
        var isConcept = target.Type.Kind == SemanticTypeReferenceKind.Concept;
        var current = $"(decimal)({State}.{Identifiers.ToPascalCase(target.Name)}{(isConcept ? "?.Value" : string.Empty)}{(isConcept || target.Type.IsOptional ? " ?? 0" : string.Empty)})";
        var operand = mapping.Operation is SemanticProjectionOperation.Increment or SemanticProjectionOperation.Decrement
            ? "1m"
            : Number(mapping.Source!, @event, context);
        if (mapping.Factor is { } factor)
        {
            operand = $"{operand} * {Number(factor, @event, context)}";
        }

        if (whole)
        {
            operand = $"global::System.Math.Round({operand}, global::System.MidpointRounding.ToEven)";
        }

        var sign = mapping.Operation is SemanticProjectionOperation.Add or SemanticProjectionOperation.Increment ? "+" : "-";
        var result = $"{current} {sign} {operand}";
        var primitiveType = types.Primitive(primitive);
        var converted = whole ? $"({primitiveType})({result})" : $"({result})";
        return isConcept ? $"new {types.Type(target.Type with { IsOptional = false })}({converted})" : converted;
    }

    static string Number(SemanticProjectionValue value, SemanticEventContract @event, SemanticApplicationContext context)
    {
        if (value is SemanticProjectionLiteral { Value: SemanticNumberValue number })
        {
            return $"{number.Value.ToString(CultureInfo.InvariantCulture)}m";
        }

        var property = (SemanticProjectionEventProperty)value;
        var eventProperty = @event.Properties.Single(candidate => candidate.Id == property.Path[0]);
        var access = $"@event.{Identifiers.ToPascalCase(eventProperty.Name)}";
        return eventProperty.Type.Kind == SemanticTypeReferenceKind.Concept ? $"(decimal){access}.Value" : $"(decimal){access}";
    }

    static string EventPropertyName(SemanticProjectionEventProperty property, SemanticEventContract @event) =>
        Identifiers.ToPascalCase(@event.Properties.Single(candidate => candidate.Id == property.Path[0]).Name);

    static string Identity(SemanticTypeReference type, SemanticTypeSystem types, SemanticApplicationContext context)
    {
        const string source = "eventContext.EventSourceId.Value";
        var primitive = type.Kind == SemanticTypeReferenceKind.Concept ? context.Concepts[type.Target].Primitive : type.Primitive;
        var value = primitive == SemanticPrimitiveType.Uuid ? $"global::System.Guid.Parse({source})" : source;
        return type.Kind == SemanticTypeReferenceKind.Concept ? $"new {types.Type(type)}({value})" : value;
    }
}
