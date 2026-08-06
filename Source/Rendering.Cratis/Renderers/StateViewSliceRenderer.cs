// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Types;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Renders a <see cref="SliceType.StateView"/> slice: the <c>[EventType]</c> records the slice declares, plus the
/// <c>[ReadModel]</c> record inferred from its <see cref="ProjectionSyntax"/>'s mappings, using model-bound
/// projection attributes for the blocks this renderer understands. Constructs it can't express as attributes
/// (composite keys, <c>join</c>, <c>children</c>, <c>nested</c>, <c>every</c>/<c>all</c>) are reported as
/// diagnostics and called out in the file rather than silently dropped.
/// </summary>
public class StateViewSliceRenderer : ISliceRenderer
{
    /// <inheritdoc/>
    public RenderedFile Render(LocatedSlice slice, ApplicationSet applicationSet, string rootNamespace)
    {
        var diagnostics = new List<string>();
        var ownNamespace = SliceNaming.Namespace(rootNamespace, slice.FullPath);
        var builder = new CSharpCodeBuilder().Namespace(ownNamespace);

        foreach (var @event in slice.Slice.Events)
        {
            EventRenderer.Render(builder, @event, applicationSet, diagnostics);
        }

        var referenced = new List<string>(EventRenderer.ReferencedNames(slice.Slice.Events));

        if (slice.Slice.Projection is { } projection)
        {
            RenderReadModel(builder, projection, applicationSet, referenced, diagnostics);
        }

        foreach (var @namespace in ReferencedNamespaces.Resolve(referenced, applicationSet, rootNamespace, ownNamespace))
        {
            builder.Using(@namespace);
        }

        var path = new List<string>(SliceNaming.FolderPath(slice.FullPath)) { SliceNaming.FileName(slice.Slice.Name) };
        return new RenderedFile(Path.Combine([.. path]), builder.ToString()) { Diagnostics = diagnostics };
    }

    static void RenderReadModel(
        CSharpCodeBuilder builder,
        ProjectionSyntax projection,
        ApplicationSet applicationSet,
        List<string> referenced,
        List<string> diagnostics)
    {
        var typeName = Identifiers.ToPascalCase(projection.ReadModel ?? projection.Name);
        var fromBlocks = projection.Blocks.OfType<FromSyntax>().ToArray();
        var events = EventPropertyIndex.Build(applicationSet);
        var properties = InferProperties(fromBlocks, events, applicationSet, diagnostics);
        var keyProperty = ProjectionKey.Resolve(projection, fromBlocks, properties, events, applicationSet, diagnostics);

        builder.Using("Cratis.Arc.Queries.ModelBound")
            .Using("Cratis.Chronicle.Events")
            .Using("Cratis.Chronicle.Projections.ModelBound")
            .Using("Cratis.Chronicle.ReadModels")
            .Using("MongoDB.Driver");

        referenced.AddRange(properties.Where(property => property.Type.Kind is not ResolvedTypeKind.Unresolved).Select(property => property.Type.ClrTypeName));

        var subscribedEvents = fromBlocks.SelectMany(from => from.Events).Select(spec => spec.Event).Distinct(StringComparer.Ordinal).ToArray();
        var removalEvents = projection.Blocks.OfType<RemoveWithSyntax>().Select(block => block.Event).Distinct(StringComparer.Ordinal).ToArray();
        referenced.AddRange(subscribedEvents);
        referenced.AddRange(removalEvents);

        foreach (var eventName in subscribedEvents)
        {
            builder.Attribute($"FromEvent<{Identifiers.ToPascalCase(eventName)}>");
        }

        foreach (var eventName in removalEvents)
        {
            builder.Attribute($"RemovedWith<{Identifiers.ToPascalCase(eventName)}>");
        }

        if (keyProperty is not null)
        {
            builder.Using("Cratis.Chronicle.Keys");
        }

        ReportUnrenderedBlocks(builder, projection, typeName, diagnostics);

        var parameters = string.Join(", ", properties.Select(property => RenderParameter(property, keyProperty)));
        builder.Attribute("ReadModel").OpenBlock($"public record {typeName}({parameters})");

        var keyType = keyProperty is null ? "Guid" : properties.First(property => property.Name == keyProperty).Type.ToTypeSyntax();
        var idParameterName = keyProperty is null ? "id" : Identifiers.ToCamelCase(keyProperty);

        builder.BlankLine()
            .Line($"public static IQueryable<{typeName}> All{Pluralizer.Pluralize(typeName)}(IMongoCollection<{typeName}> collection) => collection.AsQueryable();")
            .Line($"public static Task<{typeName}?> {typeName}ById(IReadModels readModels, {keyType} {idParameterName}) => " +
                  $"readModels.GetInstanceById<{typeName}>((EventSourceId){idParameterName});")
            .EndBlock();
    }

    static void ReportUnrenderedBlocks(CSharpCodeBuilder builder, ProjectionSyntax projection, string typeName, List<string> diagnostics)
    {
        var unrendered = projection.Blocks
            .Where(block => block is JoinSyntax or ChildrenSyntax or NestedSyntax or AllSyntax or EverySyntax or ClearWithSyntax or RemoveViaJoinSyntax)
            .GroupBy(BlockKeyword)
            .Select(group => $"{group.Count()} {group.Key}")
            .ToArray();

        if (unrendered.Length == 0)
        {
            return;
        }

        var summary = string.Join(", ", unrendered);
        builder.Line($"// TODO: {summary} block(s) not yet rendered — add via fluent IProjectionFor<{typeName}>");
        diagnostics.Add($"Projection '{typeName}' declares {summary} block(s) with no model-bound equivalent — they are not rendered.");
    }

    static string BlockKeyword(ProjectionBlockSyntax block) => block switch
    {
        JoinSyntax => "join",
        ChildrenSyntax => "children",
        NestedSyntax => "nested",
        AllSyntax => "all",
        EverySyntax => "every",
        ClearWithSyntax => "clear with",
        _ => "remove via join",
    };

    static string RenderParameter(MappedProperty property, string? keyProperty)
    {
        var attributes = new List<string>();
        if (property.Name == keyProperty)
        {
            attributes.Add("[Key]");
        }

        if (property.Attribute is not null)
        {
            attributes.Add(property.Attribute);
        }

        var prefix = attributes.Count > 0 ? $"{string.Join(' ', attributes)} " : string.Empty;
        return $"{prefix}{property.Type.ToTypeSyntax()} {property.Name}";
    }

    static List<MappedProperty> InferProperties(
        IReadOnlyList<FromSyntax> fromBlocks,
        EventPropertyIndex events,
        ApplicationSet applicationSet,
        List<string> diagnostics)
    {
        var properties = new List<MappedProperty>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var from in fromBlocks)
        {
            var eventName = from.Events.FirstOrDefault()?.Event;
            foreach (var mapping in from.Mappings)
            {
                var property = ProjectionMapping.Resolve(mapping, eventName, events, applicationSet, diagnostics);
                if (seen.Add(property.Name))
                {
                    properties.Add(property);
                }
            }
        }

        return properties;
    }
}
