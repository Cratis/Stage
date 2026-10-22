// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Publishes a per-source projection to the outbox and compares against its last durable public fact.
/// </summary>
internal static class SemanticPublicationArtifactRenderer
{
    internal static RenderedFile Render(LocatedSemanticSlice located, SemanticProjection projection, SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        var target = context.Events[projection.ReadModel];
        var name = Identifiers.ToPascalCase(projection.Name);
        var builder = new CSharpCodeBuilder().Using("System.Linq").Namespace(SliceNaming.Namespace(context.RootNamespace, located.Path));
        context.Docs(projection.Id).Render(builder)
            .Attribute($"global::Cratis.Chronicle.Reactors.ReactorAttribute({CSharpCodeBuilder.StringLiteral(projection.Id.ToString())})")
            .OpenBlock($"public class {name}(global::Cratis.Chronicle.IEventStore eventStore) : global::Cratis.Chronicle.Reactors.IReactor");
        foreach (var group in projection.Scope!.From.GroupBy(from => from.EventContract))
        {
            var source = context.Events[group.Key];
            builder.OpenBlock($"public async global::System.Threading.Tasks.Task On{Identifiers.ToPascalCase(source.Name)}({types.EventType(source)} @event, global::Cratis.Chronicle.Events.EventContext context)");
            var arguments = target.Properties.Select(property => group.SelectMany(from => from.Mappings).LastOrDefault(candidate => candidate.Target[0] == property.Id) is { } mapping
                ? Value(mapping.Source!, property.Type, source, context, types)
                : $"previous?.{Identifiers.ToPascalCase(property.Name)}");
            builder.Line("var outbox = eventStore.GetEventSequence(global::Cratis.Chronicle.EventSequences.EventSequenceId.Outbox);")
                .Line($"var history = await outbox.GetForEventSourceIdAndEventTypes(context.EventSourceId, filterEventTypes: [eventStore.EventTypes.GetEventTypeFor(typeof({types.EventType(target)}))]);")
                .Line($"var previous = history.OrderBy(fact => fact.Context.SequenceNumber.Value).LastOrDefault()?.Content as {types.EventType(target)};")
                .Line($"var published = new {types.EventType(target)}({string.Join(", ", arguments)});")
                .OpenBlock("if (previous is null || !SamePublicValue(previous, published))");
            var tags = target.Tags.Select(CSharpCodeBuilder.StringLiteral).ToArray();
            builder.Line($"var result = await outbox.Append(context.EventSourceId, published{(tags.Length == 0 ? string.Empty : $", tags: [{string.Join(", ", tags)}]")});")
                .OpenBlock("if (!result.IsSuccess)")
                .Line("throw new global::System.InvalidOperationException(\"The public event could not be appended to the outbox.\");")
                .EndBlock().EndBlock().EndBlock().BlankLine();
        }
        RenderComparison(builder);
        builder.EndBlock();
        return new(Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{name}.cs"]), builder.ToString()) { Sources = [projection.Id] };
    }

    static void RenderComparison(CSharpCodeBuilder builder)
    {
        builder.OpenBlock("static bool SamePublicValue(object? left, object? right)")
            .Line("if (global::System.Object.ReferenceEquals(left, right)) return true;")
            .Line("if (left is null || right is null) return false;")
            .OpenBlock("if (left is not string && left is global::System.Collections.IEnumerable leftItems && right is global::System.Collections.IEnumerable rightItems)")
            .Line("var first = leftItems.Cast<object?>().ToArray();")
            .Line("var second = rightItems.Cast<object?>().ToArray();")
            .Line("return first.Length == second.Length && first.Zip(second).All(pair => SamePublicValue(pair.First, pair.Second));")
            .EndBlock()
            .Line("if (left.GetType() != right.GetType()) return false;")
            .Line("if (left is string || left.GetType().IsValueType) return left.Equals(right);")
            .Line("return left.GetType().GetProperties().All(property => SamePublicValue(property.GetValue(left), property.GetValue(right)));")
            .EndBlock().BlankLine();
    }

    static string Value(SemanticProjectionValue value, SemanticTypeReference target, SemanticEventContract source, SemanticApplicationContext context, SemanticTypeSystem types) => value switch
    {
        SemanticProjectionEventProperty property => Property(property, source, context),
        SemanticProjectionLiteral literal => types.Value(literal.Value, target),
        SemanticProjectionEventSourceIdentity or SemanticProjectionEventContextValue { Path: "eventSourceId" } => Identity(target, context, types),
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticProjectionValue), value.Kind)
    };

    static string Property(SemanticProjectionEventProperty reference, SemanticEventContract source, SemanticApplicationContext context)
    {
        var expression = "@event";
        var optional = false;
        IReadOnlyList<SemanticProperty> properties = source.Properties;
        foreach (var id in reference.Path)
        {
            var property = properties.Single(property => property.Id == id);
            expression += $"{(optional ? "?." : ".")}{Identifiers.ToPascalCase(property.Name)}";
            optional = property.Type.IsOptional;
            if (property.Type.Kind == SemanticTypeReferenceKind.CompositeType) properties = context.Types[property.Type.Target].Properties;
        }
        return expression;
    }

    static string Identity(SemanticTypeReference target, SemanticApplicationContext context, SemanticTypeSystem types)
    {
        if (target.IsCollection || target.Kind == SemanticTypeReferenceKind.CompositeType)
        {
            throw UnsupportedSemanticRendering.For(nameof(SemanticProjectionEventSourceIdentity), "the destination is not a scalar");
        }
        if (target.Kind == SemanticTypeReferenceKind.Concept && !context.Concepts[target.Target].Values.IsEmpty)
        {
            return $"global::System.Enum.Parse<{types.Type(target with { IsOptional = false })}>(context.EventSourceId.Value)";
        }
        var primitive = target.Kind == SemanticTypeReferenceKind.Concept ? context.Concepts[target.Target].Primitive : target.Primitive;
        var value = primitive switch
        {
            SemanticPrimitiveType.Text => "context.EventSourceId.Value",
            SemanticPrimitiveType.Boolean => "bool.Parse(context.EventSourceId.Value)",
            SemanticPrimitiveType.Uuid => "global::System.Guid.Parse(context.EventSourceId.Value)",
            _ => $"{types.Primitive(primitive)}.Parse(context.EventSourceId.Value, global::System.Globalization.CultureInfo.InvariantCulture)"
        };
        return target.Kind == SemanticTypeReferenceKind.Concept ? $"new {types.Type(target with { IsOptional = false })}({value})" : value;
    }
}
