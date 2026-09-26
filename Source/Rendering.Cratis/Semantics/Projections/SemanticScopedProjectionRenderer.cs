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
        builder.Summary($"Projects events onto {modelName}.")
            .OpenBlock($"public class {Identifiers.ToPascalCase(projection.Name)} : Cratis.Chronicle.Projections.IProjectionFor<{modelName}>")
            .Summary("Defines the event transitions for this projection.")
            .Line("/// <param name=\"builder\">The projection builder.</param>")
            .OpenBlock($"public void Define(Cratis.Chronicle.Projections.IProjectionBuilderFor<{modelName}> builder)")
            .Line("builder.NoAutoMap();");
        RenderScope(builder, scope, context, readModel.Properties, "builder");
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
        string receiver)
    {
        foreach (var from in scope.From)
        {
            var @event = context.Events[from.EventContract];
            var eventName = Identifiers.ToPascalCase(@event.Name);
            code.OpenBlock($"{receiver}.From<{eventName}>(from =>")
                .Line("// Explicit mappings preserve the bound projection; no event-to-model AutoMap is inferred.");
            RenderKey(code, from.Key, @event, context, "from", "UsingKey");
            if (from.ParentKey is not null)
            {
                RenderKey(code, from.ParentKey, @event, context, "from", "UsingParentKey");
            }

            RenderMappings(code, from.Mappings, properties, @event, context, "from");
            code.EndBlock().Line(");");
        }

        foreach (var join in scope.Joins)
        {
            var @event = context.Events[join.EventContract];
            var on = properties.Single(_ => _.Id == join.On);
            code.OpenBlock($"{receiver}.Join<{Identifiers.ToPascalCase(@event.Name)}>(join =>")
                .Line($"join.On(model => model.{Identifiers.ToPascalCase(on.Name)});");
            RenderMappings(code, join.Mappings, properties, @event, context, "join");
            code.EndBlock().Line(");");
        }

        if (scope.Every is { } every)
        {
            code.OpenBlock($"{receiver}.{(every.SubscribesToAllEvents ? "FromAll" : "FromEvery")}(every =>");
            if (!every.IncludeChildren)
            {
                code.Line("every.ExcludeChildProjections();");
            }

            foreach (var mapping in every.Mappings)
            {
                var target = Path(mapping.Target, properties, context);
                var expression = mapping.Source is SemanticProjectionLiteral literal
                    ? $"ToValue({Literal(literal, mapping.Target, properties, context)})"
                    : "ToEventSourceId()";
                code.Line($"every.Set(model => model.{target}).{expression};");
            }

            code.EndBlock().Line(");");
        }

        foreach (var removal in scope.Removals)
        {
            var @event = context.Events[removal.EventContract];
            code.OpenBlock($"{receiver}.RemovedWith<{Identifiers.ToPascalCase(@event.Name)}>(removed =>");
            RenderKey(code, removal.Key, @event, context, "removed", "UsingKey");
            if (removal.ParentKey is not null)
            {
                RenderKey(code, removal.ParentKey, @event, context, "removed", "UsingParentKey");
            }

            code.EndBlock().Line(");");
        }

        foreach (var removal in scope.JoinRemovals)
        {
            var @event = context.Events[removal.EventContract];
            code.OpenBlock($"{receiver}.RemovedWithJoin<{Identifiers.ToPascalCase(@event.Name)}>(removed =>");
            RenderKey(code, removal.Key, @event, context, "removed", "UsingKey");
            code.EndBlock().Line(");");
        }

        foreach (var nested in scope.Nested)
        {
            var property = properties.Single(_ => _.Id == nested.Property);
            var type = context.Types[property.Type.Target];
            code.OpenBlock($"{receiver}.Nested<{Identifiers.ToPascalCase(type.Name)}>(model => model.{Identifiers.ToPascalCase(property.Name)}, nested =>")
                .Line("nested.NoAutoMap();");
            RenderScope(code, nested.Scope, context, type.Properties, "nested");
            code.EndBlock().Line(");");
        }

        foreach (var children in scope.Children)
        {
            var property = properties.Single(_ => _.Id == children.Property);
            var type = context.Types[property.Type.Target];
            var elementName = Identifiers.ToPascalCase(type.Name);
            var identity = type.Properties.Single(_ => _.Id == children.IdentifiedBy);
            code.OpenBlock($"{receiver}.Children<{elementName}>(model => model.{Identifiers.ToPascalCase(property.Name)}, children =>")
                .Line("children.NoAutoMap();")
                .Line($"children.IdentifiedBy(item => item.{Identifiers.ToPascalCase(identity.Name)});");
            RenderScope(code, children.Scope, context, type.Properties, "children");
            code.EndBlock().Line(");");
        }
    }

    static void RenderKey(CSharpCodeBuilder code, SemanticProjectionKey key, SemanticEventContract @event, SemanticApplicationContext context, string receiver, string method)
    {
        if (key is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty property })
        {
            code.Line($"{receiver}.{method}(evt => evt.{Path(property.Path, @event.Properties, context)});");
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
            var target = Path(mapping.Target, targets, context);
            var source = mapping.Source is SemanticProjectionEventProperty property
                ? $"evt => evt.{Path(property.Path, @event.Properties, context)}"
                : string.Empty;
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

    static string Literal(SemanticProjectionLiteral literal, System.Collections.Immutable.ImmutableArray<SemanticId> path, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context)
    {
        SemanticProperty? target = null;
        foreach (var id in path)
        {
            target = properties.Single(property => property.Id == id);
            properties = target.Type.Kind == SemanticTypeReferenceKind.CompositeType && context.Types.TryGetValue(target.Type.Target, out var composite)
                ? composite.Properties : [];
        }

        return new SemanticTypeSystem(context).Value(literal.Value, target!.Type)
            .Replace("CultureInfo.InvariantCulture", "System.Globalization.CultureInfo.InvariantCulture", StringComparison.Ordinal);
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
}
