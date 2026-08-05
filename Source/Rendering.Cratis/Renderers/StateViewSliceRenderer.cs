// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Types;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Renders a <see cref="SliceType.StateView"/> slice: the <c>[ReadModel]</c> record inferred from its
/// <see cref="ProjectionSyntax"/>'s mappings, using model-bound projection attributes for the <c>from</c> blocks
/// this renderer understands. Constructs it can't express as attributes (composite keys, <c>join</c>,
/// <c>children</c>, <c>nested</c>) are called out with a comment rather than silently dropped — see the
/// project's rendering plan for the documented v1 scope.
/// </summary>
public class StateViewSliceRenderer : ISliceRenderer
{
    /// <inheritdoc/>
    public RenderedFile Render(LocatedSlice slice, ApplicationSet applicationSet, string rootNamespace)
    {
        var builder = new CSharpCodeBuilder().Namespace(SliceNaming.Namespace(rootNamespace, slice.FullPath));
        var path = new List<string>(SliceNaming.FolderPath(slice.FullPath)) { SliceNaming.FileName(slice.Slice.Name) };

        if (slice.Slice.Projection is { } projection)
        {
            RenderReadModel(builder, projection, applicationSet, rootNamespace, slice.FullPath);
        }

        return new RenderedFile(Path.Combine([.. path]), builder.ToString());
    }

    static void RenderReadModel(
        CSharpCodeBuilder builder, ProjectionSyntax projection, ApplicationSet applicationSet, string rootNamespace, IReadOnlyList<string> ownPath)
    {
        var typeName = Identifiers.ToPascalCase(projection.ReadModel ?? projection.Name);
        var fromBlocks = projection.Blocks.OfType<FromSyntax>().ToArray();
        var eventPropertyTypes = BuildEventPropertyTypes(applicationSet);
        var properties = InferProperties(fromBlocks, eventPropertyTypes, applicationSet);
        var keyProperty = ResolveKeyProperty(projection, fromBlocks);

        builder.Using("Cratis.Arc.Queries.ModelBound")
            .Using("Cratis.Chronicle.Events")
            .Using("Cratis.Chronicle.Projections.ModelBound")
            .Using("Cratis.Chronicle.ReadModels")
            .Using("MongoDB.Driver");

        var ownNamespace = SliceNaming.Namespace(rootNamespace, ownPath);
        foreach (var conceptNamespace in ConceptUsings(properties, applicationSet, rootNamespace, ownNamespace))
        {
            builder.Using(conceptNamespace);
        }

        if (keyProperty is not null)
        {
            builder.Using("Cratis.Chronicle.Keys");
        }

        foreach (var eventName in fromBlocks.SelectMany(from => from.Events).Select(spec => spec.Event).Distinct(StringComparer.Ordinal))
        {
            builder.Attribute($"FromEvent<{Identifiers.ToPascalCase(eventName)}>");
        }

        if (projection.Blocks.OfType<AllSyntax>().Any() || projection.Blocks.OfType<EverySyntax>().Any())
        {
            builder.Attribute("FromAll");
        }

        var unsupportedBlocks = projection.Blocks.Where(block => block is JoinSyntax or ChildrenSyntax or NestedSyntax).ToArray();
        if (unsupportedBlocks.Length > 0)
        {
            builder.Line($"// TODO: {unsupportedBlocks.Length} join/children/nested block(s) not yet rendered — add via fluent IProjectionFor<{typeName}>");
        }

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

    static IReadOnlyList<string> ConceptUsings(
        List<InferredProperty> properties, ApplicationSet applicationSet, string rootNamespace, string ownNamespace)
    {
        var namespaces = new HashSet<string>(StringComparer.Ordinal);
        var referencedNames = properties
            .Where(property => property.Type.Kind is ResolvedTypeKind.Concept or ResolvedTypeKind.Enum or ResolvedTypeKind.Composite)
            .Select(property => property.Type.ClrTypeName);

        foreach (var name in referencedNames)
        {
            var placement = applicationSet.ConceptPlacements.GetValueOrDefault(name, []);
            var @namespace = placement.Count == 0 ? $"{rootNamespace}.Common" : SliceNaming.Namespace(rootNamespace, placement);
            if (!string.Equals(@namespace, ownNamespace, StringComparison.Ordinal))
            {
                namespaces.Add(@namespace);
            }
        }

        return [.. namespaces];
    }

    static string RenderParameter(InferredProperty property, string? keyProperty)
    {
        var attributes = new List<string>();
        if (property.Name == keyProperty)
        {
            attributes.Add("[Key]");
        }

        if (property.EventType is not null)
        {
            var attribute = property.Mapping switch
            {
                IncrementMappingSyntax => $"[Increment<{property.EventType}>]",
                DecrementMappingSyntax => $"[Decrement<{property.EventType}>]",
                CountMappingSyntax => $"[Count<{property.EventType}>]",
                AddMappingSyntax add => $"[AddFrom<{property.EventType}>(nameof({property.EventType}.{RenderSourcePropertyName(add.Value)}))]",
                SubtractMappingSyntax subtract => $"[SubtractFrom<{property.EventType}>(nameof({property.EventType}.{RenderSourcePropertyName(subtract.Value)}))]",
                SetMappingSyntax set => $"[SetFrom<{property.EventType}>(nameof({property.EventType}.{RenderSourcePropertyName(set.Source)}))]",
                _ => null,
            };

            if (attribute is not null)
            {
                attributes.Add(attribute);
            }
        }

        var prefix = attributes.Count > 0 ? $"{string.Join(' ', attributes)} " : string.Empty;
        return $"{prefix}{property.Type.ToTypeSyntax()} {property.Name}";
    }

    static string RenderSourcePropertyName(ExpressionSyntax source) =>
        source is PathExpressionSyntax path ? Identifiers.ToPascalCase(path.Path) : "Value";

    static List<InferredProperty> InferProperties(
        IReadOnlyList<FromSyntax> fromBlocks,
        IReadOnlyDictionary<string, Dictionary<string, TypeRefSyntax>> eventPropertyTypes,
        ApplicationSet applicationSet)
    {
        var properties = new List<InferredProperty>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var from in fromBlocks)
        {
            var eventName = from.Events.FirstOrDefault()?.Event;
            var eventTypeName = eventName is null ? null : Identifiers.ToPascalCase(eventName);

            foreach (var mapping in from.Mappings)
            {
                var pascalName = Identifiers.ToPascalCase(mapping.Property);
                if (!seen.Add(pascalName))
                {
                    continue;
                }

                var resolved = ResolvePropertyType(mapping, eventName, eventPropertyTypes, applicationSet);
                properties.Add(new InferredProperty(pascalName, resolved, eventTypeName, mapping));
            }
        }

        return properties;
    }

    static ResolvedType ResolvePropertyType(
        MappingSyntax mapping,
        string? eventName,
        IReadOnlyDictionary<string, Dictionary<string, TypeRefSyntax>> eventPropertyTypes,
        ApplicationSet applicationSet)
    {
        if (mapping is IncrementMappingSyntax or DecrementMappingSyntax or CountMappingSyntax)
        {
            return new ResolvedType("int", false, false, ResolvedTypeKind.Primitive);
        }

        var sourcePropertyName = mapping switch
        {
            SetMappingSyntax set when set.Source is PathExpressionSyntax path => path.Path,
            AddMappingSyntax add when add.Value is PathExpressionSyntax path => path.Path,
            SubtractMappingSyntax subtract when subtract.Value is PathExpressionSyntax path => path.Path,
            _ => mapping.Property,
        };

        if (eventName is not null && eventPropertyTypes.TryGetValue(eventName, out var eventProperties) &&
            eventProperties.TryGetValue(sourcePropertyName, out var typeRef))
        {
            return TypeResolver.Resolve(typeRef, applicationSet);
        }

        return new ResolvedType("object", false, false, ResolvedTypeKind.Unresolved);
    }

    static string? ResolveKeyProperty(ProjectionSyntax projection, IReadOnlyList<FromSyntax> fromBlocks)
    {
        var key = projection.Key ?? fromBlocks.Select(from => from.Key).FirstOrDefault(candidate => candidate is not null);
        return key is ExpressionKeySyntax { Expression: PathExpressionSyntax path } ? Identifiers.ToPascalCase(path.Path) : null;
    }

    static Dictionary<string, Dictionary<string, TypeRefSyntax>> BuildEventPropertyTypes(ApplicationSet applicationSet)
    {
        var result = new Dictionary<string, Dictionary<string, TypeRefSyntax>>(StringComparer.Ordinal);
        foreach (var @event in applicationSet.Slices.SelectMany(slice => slice.Slice.Events))
        {
            var properties = new Dictionary<string, TypeRefSyntax>(StringComparer.Ordinal);
            foreach (var property in @event.Properties)
            {
                properties[property.Name] = property.Type;
            }

            result[@event.Name] = properties;
        }

        return result;
    }

    sealed record InferredProperty(string Name, ResolvedType Type, string? EventType, MappingSyntax Mapping);
}
