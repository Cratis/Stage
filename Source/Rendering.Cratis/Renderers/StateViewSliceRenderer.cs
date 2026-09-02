// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Stage.Rendering.Cratis.Authorization;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Types;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Renders a <see cref="SliceType.StateView"/> slice: the <c>[EventType]</c> records the slice declares, plus the
/// <c>[ReadModel]</c> record inferred from its <see cref="ProjectionSyntax"/>'s mappings, using model-bound
/// projection attributes for the blocks this renderer understands — <c>from</c>, <c>join</c>, <c>all</c>,
/// <c>every</c>, <c>remove with</c>, <c>remove via join</c>, and <c>nested</c> together with the <c>clear with</c>
/// that is only meaningful inside one. Constructs it can't express as attributes (composite keys, and the
/// <c>children</c> blocks that need a generated child record type) are reported as diagnostics and called out in
/// the file rather than silently dropped, as is everything else the slice declares that nothing renders (see
/// <see cref="UnrenderedConstructs"/>).
/// </summary>
/// <remarks>
/// Each declared query that returns the rendered read model becomes a static method with its own exact Arc
/// authorization attribute. A read model receives the synthesized all/by-id pair only when no declared query
/// returns it; only that pair shares a type-level authorization fallback from <see cref="ReadModelAuthorization"/>.
/// </remarks>
public class StateViewSliceRenderer : ISliceRenderer
{
    /// <inheritdoc/>
    public RenderedFile Render(LocatedSlice slice, ApplicationSet applicationSet, string rootNamespace)
    {
        var diagnostics = new List<string>();
        var ownNamespace = SliceNaming.Namespace(rootNamespace, slice.FullPath);
        var builder = new CSharpCodeBuilder().Namespace(ownNamespace);

        // A slice may declare several projections. Only the first is rendered; the ones left out are reported by
        // UnrenderedConstructs rather than dropped in silence. A query names the read model it reads with its
        // return type, which decides both whether this file can hold its method and where that method's own
        // authorization belongs. The read model's name is therefore known before anything is reported.
        var projection = slice.Slice.Projections.FirstOrDefault();
        var readModel = projection is null ? null : ReadModelName(projection);

        UnrenderedConstructs.Report(builder, slice.Slice, RenderedConstructs.ReadModel, diagnostics, readModel);

        foreach (var @event in slice.Slice.Events)
        {
            EventRenderer.Render(builder, @event, applicationSet, diagnostics, slice.Slice.Constraints);
        }

        var referenced = new List<string>(EventRenderer.ReferencedNames(slice.Slice.Events));

        if (projection is not null)
        {
            RenderReadModel(builder, projection, readModel!, slice.Slice.Queries, applicationSet, referenced, diagnostics);
        }

        foreach (var @namespace in ReferencedNamespaces.Resolve(referenced, applicationSet, rootNamespace, ownNamespace))
        {
            builder.Using(@namespace);
        }

        var path = new List<string>(SliceNaming.FolderPath(slice.FullPath)) { SliceNaming.FileName(slice.Slice.Name) };
        return new RenderedFile(Path.Combine([.. path]), builder.ToString()) { Diagnostics = diagnostics };
    }

    // The C# type name the read model rendered from a projection takes — what a query's return type has to name
    // for its method to belong on that read model.
    static string ReadModelName(ProjectionSyntax projection) =>
        Identifiers.ToPascalCase(projection.ReadModel ?? projection.Name);

    static void RenderReadModel(
        CSharpCodeBuilder builder,
        ProjectionSyntax projection,
        string typeName,
        IEnumerable<QuerySyntax> queries,
        ApplicationSet applicationSet,
        List<string> referenced,
        List<string> diagnostics)
    {
        var blocks = projection.Blocks.ToArray();
        var fromBlocks = blocks.OfType<FromSyntax>().ToArray();
        var joinBlocks = blocks.OfType<JoinSyntax>().ToArray();
        var events = EventPropertyIndex.Build(applicationSet);

        // The properties are inferred before anything about the read model is emitted, because inferring them is
        // also what emits the sibling records its 'nested' blocks declare — they have to sit outside the record
        // this renders, not inside the block it is about to open.
        var properties = InferProperties(builder, blocks, typeName, true, events, applicationSet, referenced, diagnostics);
        if (blocks.OfType<NestedSyntax>().Any())
        {
            builder.BlankLine();
        }

        var keyProperty = ProjectionKey.Resolve(projection, fromBlocks, properties, events, applicationSet, diagnostics);

        builder.Using(AuthorizationRenderer.Namespace)
            .Using("Cratis.Arc.Queries.ModelBound")
            .Using("Cratis.Chronicle.Events")
            .Using("Cratis.Chronicle.Projections.ModelBound")
            .Using("Cratis.Chronicle.ReadModels")
            .Using("MongoDB.Driver");

        referenced.AddRange(properties.Where(property => property.Type.Kind is not ResolvedTypeKind.Unresolved).Select(property => property.Type.ClrTypeName));

        var subscriptions = Subscriptions(fromBlocks);
        var removalEvents = projection.Blocks.OfType<RemoveWithSyntax>().Select(block => block.Event).Distinct(StringComparer.Ordinal).ToArray();
        var joinedEvents = joinBlocks.SelectMany(join => join.Events).Select(joined => joined.Event).Distinct(StringComparer.Ordinal).ToArray();
        var joinRemovals = projection.Blocks.OfType<RemoveViaJoinSyntax>().ToArray();
        referenced.AddRange(subscriptions.Select(subscription => subscription.Spec.Event));
        referenced.AddRange(removalEvents);
        referenced.AddRange(joinedEvents);
        referenced.AddRange(joinRemovals.Select(block => block.Event));

        // The key is rendered onto [FromEvent] here exactly as it is on a nested record. Chronicle seeds every
        // From with the event source id and only ever overwrites it from a class-level [FromEvent]'s key — it
        // never reads [Key] for this — so a read model whose key came only from [Key] would keep routing its
        // documents on the event source id no matter what the projection declared.
        foreach (var subscription in subscriptions)
        {
            builder.Attribute(FromEvent(subscription.From, subscription.Spec, typeName, events, diagnostics));
        }

        foreach (var eventName in removalEvents)
        {
            builder.Attribute($"RemovedWith<{Identifiers.ToPascalCase(eventName)}>");
        }

        foreach (var attribute in joinRemovals.Select(block => RemovedWithJoin(block, events, diagnostics)).Distinct(StringComparer.Ordinal))
        {
            builder.Attribute(attribute);
        }

        // 'no automap' is an explicit instruction to stop AutoMap populating every name-matching event property.
        // Chronicle reads it from a class-level [NoAutoMap]; leaving it off would silently invert the author's
        // intent, because AutoMap defaults to enabled whenever the attribute is absent.
        if (projection.AutoMap == AutoMapMode.Disabled)
        {
            builder.Using("Cratis.Chronicle.Projections").Attribute("NoAutoMap");
        }

        ReportBlocksDisablingAutoMap(projection, joinBlocks, diagnostics);

        if (keyProperty is not null)
        {
            builder.Using("Cratis.Chronicle.Keys");
        }

        ReportUnrenderedChildren(builder, blocks, typeName, diagnostics);
        ReportUnrenderedClearWith(builder, blocks, typeName, diagnostics);

        var typeAuthorization = ReadModelAuthorization.Render(typeName, queries, diagnostics);

        var parameters = string.Join(", ", properties.Select(property => RenderParameter(property, keyProperty)));
        builder.Attribute("ReadModel");
        if (typeAuthorization is not null)
        {
            builder.Attribute(typeAuthorization);
        }

        builder.OpenBlock($"public record {typeName}({parameters})");

        var keyType = keyProperty is null ? "Guid" : properties.First(property => property.Name == keyProperty).Type.ToTypeSyntax();
        var idParameterName = keyProperty is null ? "id" : Identifiers.ToCamelCase(keyProperty);

        QueryRenderer.Render(builder, typeName, keyType, idParameterName, queries, applicationSet, diagnostics);
        builder.EndBlock();
    }

    // A 'no automap' written on a single block has no model-bound expression: [NoAutoMap] is scoped to the read
    // model or to one property, never to a block. Such a block therefore renders under the projection's own
    // setting, which inverts the instruction, so the loss is named. Children and nested are excluded because they
    // are already reported wholesale as unrendered.
    static void ReportBlocksDisablingAutoMap(
        ProjectionSyntax projection,
        IReadOnlyList<JoinSyntax> joinBlocks,
        List<string> diagnostics)
    {
        var blocks = projection.Blocks.OfType<AllSyntax>().Where(block => block.AutoMap == AutoMapMode.Disabled).Select(_ => "all")
            .Concat(projection.Blocks.OfType<EverySyntax>().Where(block => block.AutoMap == AutoMapMode.Disabled).Select(_ => "every"))
            .Concat(joinBlocks.SelectMany(join => join.Events).Where(joined => joined.AutoMap == AutoMapMode.Disabled).Select(_ => "join"))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (blocks.Length == 0)
        {
            return;
        }

        diagnostics.Add(
            $"'no automap' on the {string.Join(", ", blocks)} block(s) is not rendered — a model-bound [NoAutoMap] is scoped to the " +
            "read model or a single property, never to one block, so those blocks map under the projection's own setting.");
    }

    // The type-level attribute a 'remove via join' block renders to. Its key names a property on the removing
    // event, so it is referenced the same way every other event property reference is.
    static string RemovedWithJoin(RemoveViaJoinSyntax block, EventPropertyIndex events, List<string> diagnostics)
    {
        var eventTypeName = Identifiers.ToPascalCase(block.Event);

        switch (block.Key)
        {
            case null:
                return $"RemovedWithJoin<{eventTypeName}>";

            case PathExpressionSyntax path:
                return $"RemovedWithJoin<{eventTypeName}>(key: {ProjectionMapping.EventPropertyReference(block.Event, path.Path, events)})";

            default:
                diagnostics.Add(
                    $"Key of type '{block.Key.GetType().Name}' on the 'remove via join' for '{block.Event}' is not an event property " +
                    "— rendered as a removal on the event source id.");
                return $"RemovedWithJoin<{eventTypeName}>";
        }
    }

    // A 'children' block projects into its own child record type, which nothing generates yet. It is the last
    // block left without a rendering; composite keys are reported separately by ProjectionKey because they have
    // no model-bound equivalent at all.
    static void ReportUnrenderedChildren(
        CSharpCodeBuilder builder,
        IReadOnlyList<ProjectionBlockSyntax> blocks,
        string typeName,
        List<string> diagnostics)
    {
        var count = blocks.OfType<ChildrenSyntax>().Count();
        if (count == 0)
        {
            return;
        }

        builder.Line($"// TODO: {count} children block(s) not yet rendered — they project into a child record type nothing generates yet");
        diagnostics.Add(
            $"Projection '{typeName}' declares {count} children block(s) that project into a child record type nothing generates yet — they are not rendered.");
    }

    // A 'clear with' renders only inside a 'nested' block, as the class-level [ClearWith] on the nested type.
    // Anywhere else Chronicle reads nothing from it — the attribute would compile on the root read model and then
    // be discarded — so emitting it there would be a silent no-op dressed up as a rendering.
    static void ReportUnrenderedClearWith(
        CSharpCodeBuilder builder,
        IReadOnlyList<ProjectionBlockSyntax> blocks,
        string typeName,
        List<string> diagnostics)
    {
        var count = blocks.OfType<ClearWithSyntax>().Count();
        if (count == 0)
        {
            return;
        }

        builder.Line($"// TODO: {count} clear with block(s) not yet rendered — a class-level [ClearWith] is only read on a nested type");
        diagnostics.Add(
            $"Projection '{typeName}' declares {count} 'clear with' block(s) outside a 'nested' block — Chronicle only reads a " +
            "class-level [ClearWith] on a nested type, so they are not rendered.");
    }

    // Inside a 'nested' block only 'from', 'clear with' and further 'nested' blocks have an established meaning on
    // the nested type. What Chronicle's nested definition does with the rest is unverified, so they are named
    // rather than rendered as attributes whose behavior there nobody has confirmed.
    static void ReportBlocksUnrenderedInNested(
        CSharpCodeBuilder builder,
        IReadOnlyList<ProjectionBlockSyntax> blocks,
        string typeName,
        List<string> diagnostics)
    {
        var unrendered = blocks
            .Where(block => block is JoinSyntax or AllSyntax or EverySyntax or RemoveWithSyntax or RemoveViaJoinSyntax)
            .GroupBy(NestedBlockKeyword)
            .Select(group => $"{group.Count()} {group.Key}")
            .ToArray();

        if (unrendered.Length == 0)
        {
            return;
        }

        var summary = string.Join(", ", unrendered);
        builder.Line($"// TODO: {summary} block(s) not yet rendered — their meaning on a nested type is not established");
        diagnostics.Add(
            $"Nested record '{typeName}' declares {summary} block(s) whose meaning on a nested type is not established — they are not rendered.");
    }

    static string NestedBlockKeyword(ProjectionBlockSyntax block) => block switch
    {
        JoinSyntax => "join",
        AllSyntax => "all",
        EverySyntax => "every",
        RemoveWithSyntax => "remove with",
        RemoveViaJoinSyntax => "remove via join",
        _ => throw new ArgumentOutOfRangeException(nameof(block), $"'{block.GetType().Name}' is not one of the blocks a nested record leaves unrendered.")
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

    // Every block that maps a value contributes properties to the record being rendered, not just 'from': a
    // join's 'with' mappings, the mappings an 'all' or 'every' block applies across events, and the single
    // nullable property a 'nested' block hangs its own generated record off. They are walked in that order so a
    // join key mapped by a 'from' block is already a known property when the join's 'on:' reference is built.
    // The root read model and every nested record run through here with the same blocks-and-a-type-name shape;
    // only the root renders the blocks whose meaning on a nested type is unestablished.
    static List<MappedProperty> InferProperties(
        CSharpCodeBuilder builder,
        IReadOnlyList<ProjectionBlockSyntax> blocks,
        string typeName,
        bool root,
        EventPropertyIndex events,
        ApplicationSet applicationSet,
        List<string> referenced,
        List<string> diagnostics)
    {
        var fromBlocks = blocks.OfType<FromSyntax>().ToArray();
        JoinSyntax[] joinBlocks = root ? [.. blocks.OfType<JoinSyntax>()] : [];
        var properties = new List<MappedProperty>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        // A property already mapped by an earlier block keeps that first mapping. The later one is reported
        // rather than dropped in silence, so the file never quietly loses a mapping its author wrote.
        void Add(MappedProperty property, string scope)
        {
            if (seen.Add(property.Name))
            {
                properties.Add(property);
                return;
            }

            if (property.Attribute is not null)
            {
                diagnostics.Add(
                    $"Mapping for '{property.Name}' in the '{scope}' block is not rendered — an earlier block already maps that property.");
            }
        }

        foreach (var from in fromBlocks)
        {
            var fromEvents = from.Events.ToArray();
            var eventName = fromEvents.FirstOrDefault()?.Event;

            // Mappings resolve against the first event a 'from' names. The rest are still subscribed through
            // [FromEvent], but nothing maps their properties onto the record, so the shortfall is named rather
            // than left looking like the block mapped every event it lists.
            if (fromEvents.Length > 1)
            {
                diagnostics.Add(
                    $"The 'from' block naming {string.Join(", ", fromEvents.Select(spec => $"'{spec.Event}'"))} maps only from '{eventName}' — " +
                    "the other events are subscribed but contribute no mapped property.");
            }

            foreach (var mapping in from.Mappings)
            {
                Add(ProjectionMapping.Resolve(mapping, eventName, events, applicationSet, diagnostics), "from");
            }
        }

        foreach (var nested in blocks.OfType<NestedSyntax>())
        {
            Add(RenderNestedRecord(builder, nested, typeName, events, applicationSet, referenced, diagnostics), "nested");
        }

        if (!root)
        {
            return properties;
        }

        AddJoined(joinBlocks, properties, seen, events, applicationSet, diagnostics);

        // Chronicle's model-bound surface cannot carry 'subscribes to all events': its client rewrites [FromAll]
        // into [FromEvery], and the flag that separates the two lives only in the definition path. The mappings
        // survive, the system-wide subscription does not, so the loss is reported instead of implied.
        if (blocks.OfType<AllSyntax>().Any())
        {
            diagnostics.Add(
                "An 'all' block subscribes to every event type in the system, which no model-bound attribute expresses — " +
                "its mappings are rendered, but the projection only observes the events its 'from' blocks name.");
        }

        foreach (var mapping in blocks.OfType<AllSyntax>().SelectMany(all => all.Mappings))
        {
            Add(ProjectionMapping.ResolveGlobal(mapping, GlobalMappingScope.All, diagnostics), "all");
        }

        foreach (var mapping in blocks.OfType<EverySyntax>().SelectMany(every => every.Mappings))
        {
            Add(ProjectionMapping.ResolveGlobal(mapping, GlobalMappingScope.Every, diagnostics), "every");
        }

        return properties;
    }

    // A 'nested' block renders as a sibling top-level record — never a [ReadModel] of its own — carrying its own
    // class-level [FromEvent] for every event its 'from' blocks name, the [ClearWith] a 'clear with' inside it
    // finally makes renderable, and its own [NoAutoMap] when it disables automapping independently of the read
    // model. The parent holds it through a [Nested] property that has to be nullable: Chronicle clears the whole
    // object by writing null to it. Its type name is the enclosing type's name suffixed with the property, so a
    // nested block inside a nested block still lands on a name nothing else can take.
    static MappedProperty RenderNestedRecord(
        CSharpCodeBuilder builder,
        NestedSyntax nested,
        string parentTypeName,
        EventPropertyIndex events,
        ApplicationSet applicationSet,
        List<string> referenced,
        List<string> diagnostics)
    {
        var propertyName = Identifiers.ToPascalCase(nested.Property);
        var typeName = $"{parentTypeName}{propertyName}";
        var blocks = nested.Blocks.ToArray();
        var properties = InferProperties(builder, blocks, typeName, false, events, applicationSet, referenced, diagnostics);

        var subscriptions = Subscriptions([.. blocks.OfType<FromSyntax>()]);
        var clearingEvents = blocks.OfType<ClearWithSyntax>().Select(block => block.Event).Distinct(StringComparer.Ordinal).ToArray();

        referenced.AddRange(subscriptions.Select(subscription => subscription.Spec.Event));
        referenced.AddRange(clearingEvents);
        referenced.AddRange(properties.Where(property => property.Type.Kind is not ResolvedTypeKind.Unresolved).Select(property => property.Type.ClrTypeName));

        builder.BlankLine();
        ReportUnrenderedChildren(builder, blocks, typeName, diagnostics);
        ReportBlocksUnrenderedInNested(builder, blocks, typeName, diagnostics);

        foreach (var subscription in subscriptions)
        {
            builder.Attribute(FromEvent(subscription.From, subscription.Spec, typeName, events, diagnostics));
        }

        foreach (var eventName in clearingEvents)
        {
            builder.Attribute($"ClearWith<{Identifiers.ToPascalCase(eventName)}>");
        }

        if (nested.AutoMap == AutoMapMode.Disabled)
        {
            builder.Using("Cratis.Chronicle.Projections").Attribute("NoAutoMap");
        }

        var parameters = string.Join(", ", properties.Select(property => RenderParameter(property, null)));
        builder.Line($"public record {typeName}({parameters});");

        return new MappedProperty(propertyName, new ResolvedType(typeName, false, true, ResolvedTypeKind.Composite), "[Nested]", null);
    }

    // The key a 'from' declares is what routes its events to a document, and [FromEvent] is what carries it, on
    // the read model and on a nested record alike: Chronicle seeds every From with the event source id and only
    // overwrites it from a class-level [FromEvent]'s key, never from [Key], which it reads solely to identify a
    // child. Dropping the key would not lose a detail, it would write the document keyed on whatever the event
    // source id points at. A key written on one event of a 'from' wins over the block's, matching the order the
    // kernel resolves the same syntax in.
    // One subscription per event, keeping the 'from' block it came from so the key it declares travels with it.
    static (FromSyntax From, EventSpecSyntax Spec)[] Subscriptions(IReadOnlyList<FromSyntax> fromBlocks) =>
        [.. fromBlocks
            .SelectMany(from => from.Events.Select(spec => (From: from, Spec: spec)))
            .GroupBy(subscription => subscription.Spec.Event, StringComparer.Ordinal)
            .Select(group => group.First())];

    static string FromEvent(
        FromSyntax from,
        EventSpecSyntax spec,
        string typeName,
        EventPropertyIndex events,
        List<string> diagnostics)
    {
        var eventTypeName = Identifiers.ToPascalCase(spec.Event);
        var arguments = new List<string>();
        var key = spec.Key ?? (from.Key as ExpressionKeySyntax)?.Expression;

        switch (key)
        {
            case PathExpressionSyntax path:
                arguments.Add($"key: {ProjectionMapping.EventPropertyReference(spec.Event, path.Path, events)}");
                break;

            case not null:
                diagnostics.Add(
                    $"The key of type '{key.GetType().Name}' on '{spec.Event}' in nested record '{typeName}' does not read a property of " +
                    "that event, so it is not rendered and the event routes on the event source id.");
                break;

            case null when from.Key is CompositeKeySyntax composite:
                diagnostics.Add(
                    $"The composite key '{composite.Type}' on '{spec.Event}' in nested record '{typeName}' has no model-bound " +
                    "equivalent, so it is not rendered and the event routes on the event source id.");
                break;
        }

        switch (from.ParentKey)
        {
            case PathExpressionSyntax parentPath:
                arguments.Add($"parentKey: {ProjectionMapping.EventPropertyReference(spec.Event, parentPath.Path, events)}");
                break;

            case not null:
                diagnostics.Add(
                    $"The parent key of type '{from.ParentKey.GetType().Name}' on '{spec.Event}' in nested record '{typeName}' does not " +
                    "read a property of that event, so it is not rendered.");
                break;
        }

        return arguments.Count == 0 ? $"FromEvent<{eventTypeName}>" : $"FromEvent<{eventTypeName}>({string.Join(", ", arguments)})";
    }

    static void AddJoined(
        IReadOnlyList<JoinSyntax> joinBlocks,
        List<MappedProperty> properties,
        HashSet<string> seen,
        EventPropertyIndex events,
        ApplicationSet applicationSet,
        List<string> diagnostics)
    {
        var joined = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var join in joinBlocks)
        {
            foreach (var joinEvent in join.Events)
            {
                foreach (var mapping in joinEvent.Mappings)
                {
                    var property = ProjectionMapping.ResolveJoined(mapping, joinEvent.Event, join.On, seen, events, applicationSet, diagnostics);

                    // Several joined events commonly feed one property — a name and the event that renames it.
                    // [Join] allows multiple, so the later event joins onto the property already declared
                    // instead of being dropped by the name it shares.
                    if (joined.TryGetValue(property.Name, out var index) && property.Attribute is not null)
                    {
                        var existing = properties[index];
                        properties[index] = existing with
                        {
                            Attribute = existing.Attribute is null ? property.Attribute : $"{existing.Attribute} {property.Attribute}"
                        };
                        continue;
                    }

                    if (!seen.Add(property.Name))
                    {
                        diagnostics.Add(
                            $"Mapping for '{property.Name}' in the join on '{join.On}' is not rendered — an earlier block already maps that property.");
                        continue;
                    }

                    joined[property.Name] = properties.Count;
                    properties.Add(property);
                }
            }
        }
    }
}
