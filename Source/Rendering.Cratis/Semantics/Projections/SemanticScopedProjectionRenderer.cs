// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Lowers admitted semantic projection scopes to explicit Chronicle projection builders.
/// </summary>
internal static class SemanticScopedProjectionRenderer
{
    /// <summary>
    /// Renders a projection definition with explicit mappings and no implicit auto-mapping.
    /// </summary>
    /// <param name="projection">The semantic projection.</param>
    /// <param name="readModel">The target read model.</param>
    /// <param name="scope">The admitted scope.</param>
    /// <param name="context">The semantic application.</param>
    /// <returns>The C# declaration.</returns>
    public static string Render(SemanticProjection projection, SemanticReadModel readModel, SemanticProjectionScope scope, SemanticApplicationContext context)
    {
        var builder = new CSharpCodeBuilder();
        var modelName = Identifiers.ToPascalCase(readModel.Name);
        var projectionName = Identifiers.ToPascalCase(projection.Name);
        var implementationName = projectionName == modelName ? $"{projectionName}Projection" : projectionName;
        builder.Summary($"Projects events onto {modelName}.")
            .OpenBlock($"public class {implementationName} : global::Cratis.Chronicle.Projections.IProjectionFor<{modelName}>")
            .Summary("Defines the event transitions for this projection.")
            .Line("/// <param name=\"builder\">The projection builder.</param>")
            .OpenBlock($"public void Define(global::Cratis.Chronicle.Projections.IProjectionBuilderFor<{modelName}> builder)")
            .Line("builder.NoAutoMap();");
        var identifiers = readModel.Properties.Where(property => property.IsIdentifier).ToArray();
        CompositeKey? composite = identifiers.Length > 1 ? new(SemanticCompositeKeyAdmission.KeyTypeName(readModel), identifiers) : null;
        RenderScope(builder, scope, context, readModel.Properties, "builder", identifiers.Length == 1 ? identifiers[0] : null, composite);
        builder.EndBlock().EndBlock();
        var source = builder.ToString();

        // NoAutoMap makes explicit same-name mappings necessary (CHR0029), and joined fields deliberately
        // take precedence over local writes when replayed (CHR0042). Both are verified by differential specs.
        return "#pragma warning disable CHR0029, CHR0042\n" +
            source[source.IndexOf("/// <summary>", StringComparison.Ordinal)..].TrimEnd() +
            "\n#pragma warning restore CHR0029, CHR0042";
    }

    static void RenderScope(
        CSharpCodeBuilder code,
        SemanticProjectionScope scope,
        SemanticApplicationContext context,
        IReadOnlyList<SemanticProperty> properties,
        string receiver,
        SemanticProperty? rootIdentifier = null,
        CompositeKey? composite = null)
    {
        foreach (var from in scope.From)
        {
            var @event = context.Events[from.EventContract];
            var eventName = new SemanticTypeSystem(context).EventType(@event);
            code.OpenBlock($"{receiver}.From<{eventName}>(from =>")
                .Line("// Explicit mappings preserve the bound projection; no event-to-model AutoMap is inferred.");
            RenderKey(code, from.Key, @event, context, "from", "UsingKey", composite, properties);
            if (from.ParentKey is not null)
            {
                RenderKey(code, from.ParentKey, @event, context, "from", "UsingParentKey");
            }

            // The key record carries the identity Chronicle stores; the declared key properties keep their own values for queries.
            if (composite is not null && from.Key is SemanticProjectionPartsKey parts)
            {
                foreach (var part in parts.Parts.Where(part => part.Value is SemanticProjectionEventProperty))
                {
                    var target = Identifiers.ToPascalCase(properties.Single(property => property.Id == part.Property).Name);
                    code.Line($"from.Set(model => model.{target}).To(evt => evt.{Path(((SemanticProjectionEventProperty)part.Value).Path, @event.Properties, context)});");
                }
            }

            // Chronicle stores the root key separately from the record's identifier property.
            // Screenplay establishes that property from the key even when auto-map is disabled.
            if (rootIdentifier is { } identifier && !from.Mappings.Any(mapping => mapping.Target.Contains(identifier.Id)))
            {
                var target = Identifiers.ToPascalCase(identifier.Name);
                var source = from.Key switch
                {
                    SemanticProjectionValueKey { Value: SemanticProjectionEventProperty property } => $"To(evt => evt.{Path(property.Path, @event.Properties, context)})",
                    SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity } => "ToEventSourceId()",
                    SemanticProjectionValueKey { Value: SemanticProjectionLiteral constant } => $"ToValue({Literal(constant, [identifier.Id], properties, context)})",
                    _ => throw UnsupportedSemanticRendering.For(nameof(SemanticProjectionKeyKind), from.Key.Kind)
                };
                code.Line($"from.Set(model => model.{target}).{source};");
            }

            RenderMappings(code, WithEveryLiterals(from.Mappings, scope), properties, @event, context, "from");
            code.EndBlock().Line(");");
        }

        foreach (var join in scope.Joins)
        {
            var @event = context.Events[join.EventContract];
            var on = properties.Single(_ => _.Id == join.On);
            code.OpenBlock($"{receiver}.Join<{new SemanticTypeSystem(context).EventType(@event)}>(join =>");
            if (receiver != "children")
            {
                // Chronicle rejects On for a child join: the join always matches the elements' own identity.
                code.Line($"join.On(model => model.{Identifiers.ToPascalCase(on.Name)});");
            }

            RenderMappings(code, join.Mappings, properties, @event, context, "join");
            code.EndBlock().Line(");");
        }

        new EveryRenderer(code, context, properties).Render(scope, receiver, rootIdentifier);

        foreach (var removal in scope.Removals)
        {
            var @event = context.Events[removal.EventContract];
            code.OpenBlock($"{receiver}.RemovedWith<{new SemanticTypeSystem(context).EventType(@event)}>(removed =>");
            RenderKey(code, removal.Key, @event, context, "removed", "UsingKey", composite, properties);
            if (removal.ParentKey is not null)
            {
                RenderKey(code, removal.ParentKey, @event, context, "removed", "UsingParentKey");
            }

            code.EndBlock().Line(");");
        }

        foreach (var removal in scope.JoinRemovals)
        {
            var @event = context.Events[removal.EventContract];
            code.OpenBlock($"{receiver}.RemovedWithJoin<{new SemanticTypeSystem(context).EventType(@event)}>(removed =>");
            RenderKey(code, removal.Key, @event, context, "removed", "UsingKey");
            code.EndBlock().Line(");");
        }

        foreach (var nested in scope.Nested)
        {
            var property = properties.Single(_ => _.Id == nested.Property);
            var type = context.Types[property.Type.Target];
            code.OpenBlock($"{receiver}.Nested<{new SemanticTypeSystem(context).CommonType(type.Name)}>(model => model.{Identifiers.ToPascalCase(property.Name)}, nested =>")
                .Line("nested.NoAutoMap();");
            RenderScope(code, nested.Scope, context, type.Properties, "nested");
            code.EndBlock().Line(");");
        }

        foreach (var children in scope.Children)
        {
            var property = properties.Single(_ => _.Id == children.Property);
            var type = context.Types[property.Type.Target];
            var elementName = new SemanticTypeSystem(context).CommonType(type.Name);
            var identity = type.Properties.Single(_ => _.Id == children.IdentifiedBy);
            code.OpenBlock($"{receiver}.Children<{elementName}>(model => model.{Identifiers.ToPascalCase(property.Name)}, children =>")
                .Line("children.NoAutoMap();")
                .Line($"children.IdentifiedBy(item => item.{Identifiers.ToPascalCase(identity.Name)});");
            RenderScope(code, children.Scope, context, type.Properties, "children");
            code.EndBlock().Line(");");
        }
    }

    // Native every/all must preserve original from targets: Chronicle uses them to select join backfills.
    // At join-free levels, lowering instead replaces exact colliding Sets so every wins after from mappings.
    static IEnumerable<SemanticProjectionMapping> WithEveryLiterals(IEnumerable<SemanticProjectionMapping> mappings, SemanticProjectionScope scope)
    {
        if (SemanticScopedProjectionSupport.RendersEveryLiteralsNatively(scope))
        {
            return mappings;
        }

        var literals = scope.Every?.Mappings.Where(mapping => mapping.Source is SemanticProjectionLiteral).ToArray() ?? [];
        return mappings.Where(mapping => !literals.Any(literal => literal.Target.SequenceEqual(mapping.Target))).Concat(literals);
    }

    static void RenderKey(
        CSharpCodeBuilder code,
        SemanticProjectionKey key,
        SemanticEventContract @event,
        SemanticApplicationContext context,
        string receiver,
        string method,
        CompositeKey? composite = null,
        IReadOnlyList<SemanticProperty>? properties = null)
    {
        if (key is SemanticProjectionPartsKey partsKey && composite is not null && properties is not null)
        {
            code.OpenBlock($"{receiver}.UsingCompositeKey<{composite.TypeName}>(key =>");
            var ordered = SemanticStateViewArtifactRenderer.OrderedProperties(properties.Where(property => property.IsIdentifier)).Select(property => property.Id).ToList();
            foreach (var part in partsKey.Parts.OrderBy(part => ordered.IndexOf(part.Property)))
            {
                var target = Identifiers.ToPascalCase(properties.Single(property => property.Id == part.Property).Name);
                var source = (SemanticProjectionEventProperty)part.Value;
                code.Line($"key.Set(part => part.{target}).To(evt => evt.{Path(source.Path, @event.Properties, context)});");
            }

            code.EndBlock().Line(");");
        }
        else if (key is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty property })
        {
            code.Line($"{receiver}.{method}(evt => evt.{Path(property.Path, @event.Properties, context)});");
        }
        else if (key is SemanticProjectionValueKey { Value: SemanticProjectionLiteral { Value: SemanticTextValue constant } })
        {
            code.Line($"{receiver}.UsingConstantKey(\"{constant.Value}\");");
        }
    }

    static void RenderMappings(
        CSharpCodeBuilder code,
        IEnumerable<SemanticProjectionMapping> mappings,
        IReadOnlyList<SemanticProperty> targets,
        SemanticEventContract @event,
        SemanticApplicationContext context,
        string receiver)
    {
        foreach (var mapping in mappings)
        {
            if (SemanticScopedProjectionSupport.IsZeroTotalStart(mapping, SemanticScopedProjectionSupport.Target(mapping.Target, targets, context), context))
            {
                continue;
            }

            var target = Path(mapping.Target, targets, context);
            var source = mapping.Source is SemanticProjectionEventProperty property
                ? $"evt => evt.{Path(property.Path, @event.Properties, context)}"
                : string.Empty;

            // A product of two event values is added through the member the event carries for it (SemanticProjectionProducts).
            if (SemanticProjectionProducts.Of(mapping, @event, targets, context) is { } product)
            {
                source = $"evt => evt.{product.Name}";
            }

            var expression = mapping.Operation switch
            {
                SemanticProjectionOperation.Set when mapping.Source is SemanticProjectionEventSourceIdentity => $"Set(model => model.{target}).ToEventSourceId()",
                SemanticProjectionOperation.Set when mapping.Source is SemanticProjectionLiteral literal => $"Set(model => model.{target}).ToValue({Literal(literal, mapping.Target, targets, context)})",
                SemanticProjectionOperation.Set => $"Set(model => model.{target}).To({source})",
                SemanticProjectionOperation.Add => $"Add(model => model.{target}).With({source})",
                SemanticProjectionOperation.Subtract => $"Subtract(model => model.{target}).With({source})",
                SemanticProjectionOperation.Increment => $"Increment(model => model.{target})",
                SemanticProjectionOperation.Decrement => $"Decrement(model => model.{target})",
                SemanticProjectionOperation.Clear => $"Clear(model => model.{target})",
                _ => throw UnsupportedSemanticRendering.For(nameof(SemanticProjectionOperation), mapping.Operation)
            };
            code.Line($"{receiver}.{expression};");
        }
    }

    // IAllSetBuilder.ToValue infers its type from the argument and is not checked against the property,
    // so a null literal needs the property type spelled out.
    static string NativeToValue(SemanticProjectionLiteral literal, System.Collections.Immutable.ImmutableArray<SemanticId> path, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context)
    {
        var value = Literal(literal, path, properties, context);
        if (literal.Value is not SemanticNullValue)
        {
            return $"ToValue({value})";
        }

        var type = new SemanticTypeSystem(context).Type(TargetProperty(path, properties, context).Type with { IsOptional = true });
        return $"ToValue<{type}>({value})";
    }

    static string Literal(SemanticProjectionLiteral literal, System.Collections.Immutable.ImmutableArray<SemanticId> path, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context) =>
        new SemanticTypeSystem(context).Value(literal.Value, TargetProperty(path, properties, context).Type);

    static SemanticProperty TargetProperty(System.Collections.Immutable.ImmutableArray<SemanticId> path, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context)
    {
        SemanticProperty? target = null;
        foreach (var id in path)
        {
            target = properties.Single(property => property.Id == id);
            properties = target.Type.Kind == SemanticTypeReferenceKind.CompositeType && context.Types.TryGetValue(target.Type.Target, out var composite)
                ? composite.Properties : [];
        }

        return target!;
    }

    static string Path(System.Collections.Immutable.ImmutableArray<SemanticId> path, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context)
    {
        var segments = new List<string>();
        foreach (var id in path)
        {
            var property = properties.Single(_ => _.Id == id);
            var optionalIntermediate = property.Type.IsOptional && id != path[^1];
            segments.Add($"{Identifiers.ToPascalCase(property.Name)}{(optionalIntermediate ? "!" : string.Empty)}");
            properties = property.Type.Kind == SemanticTypeReferenceKind.CompositeType && context.Types.TryGetValue(property.Type.Target, out var type)
                ? type.Properties : [];
        }

        return string.Join('.', segments);
    }

    sealed class EveryRenderer(CSharpCodeBuilder code, SemanticApplicationContext context, IReadOnlyList<SemanticProperty> properties)
    {
        public void Render(SemanticProjectionScope scope, string receiver, SemanticProperty? rootIdentifier)
        {
            SemanticProjectionMapping[] mappings = [.. SemanticScopedProjectionSupport.NativeEveryMappings(scope)];
            if (mappings.Length == 0)
            {
                return;
            }

            var every = scope.Every!;
            code.OpenBlock($"{receiver}.{(every.SubscribesToAllEvents ? "FromAll" : "FromEvery")}(every =>");
            if (!every.IncludeChildren)
            {
                code.Line("every.ExcludeChildProjections();");
            }

            EstablishAllIdentifier(every, rootIdentifier);
            foreach (var mapping in mappings)
            {
                var target = Path(mapping.Target, properties, context);
                var value = mapping.Source is SemanticProjectionLiteral literal ? NativeToValue(literal, mapping.Target, properties, context) : "ToEventSourceId()";
                code.Line($"every.Set(model => model.{target}).{value};");
            }

            code.EndBlock().Line(");");
        }

        void EstablishAllIdentifier(SemanticProjectionEvery every, SemanticProperty? identifier)
        {
            if (!every.SubscribesToAllEvents || identifier is null)
            {
                return;
            }

            // FromAll creates instances even when no from block establishes their identifier.
            if (!every.Mappings.Any(mapping => mapping.Target.Contains(identifier.Id)))
            {
                code.Line($"every.Set(model => model.{Identifiers.ToPascalCase(identifier.Name)}).ToEventSourceId();");
            }
        }
    }

    sealed record CompositeKey(string TypeName, IReadOnlyList<SemanticProperty> Parts);
}
