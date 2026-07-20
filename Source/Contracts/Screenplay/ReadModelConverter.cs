// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Stage.Contracts.Projections;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Builds a Stage <see cref="ReadModelDefinition"/> for a slice from its Screenplay projection and queries. Screenplay
/// does not declare read-model property types, so the schema is synthesized from the union of the projection's mapping
/// target property names and the query parameters, with each property's type inferred from a same-named event property
/// where one exists and left open otherwise.
/// </summary>
public static class ReadModelConverter
{
    /// <summary>
    /// Builds the read-model definition for a slice, or <see langword="null"/> when the slice declares neither a
    /// projection nor a query.
    /// </summary>
    /// <param name="slice">The slice to build from.</param>
    /// <param name="schema">The schema synthesizer.</param>
    /// <param name="eventPropertyTypes">The global map of event property name to Screenplay type name, used to infer read-model property types.</param>
    /// <param name="slicePath">The fully-qualified slice path, used to derive a stable identifier.</param>
    /// <returns>The read-model definition, or <see langword="null"/>.</returns>
    public static ReadModelDefinition? Convert(
        SliceSyntax slice,
        SchemaSynthesizer schema,
        IReadOnlyDictionary<string, string> eventPropertyTypes,
        string slicePath)
    {
        var queries = slice.Queries.ToArray();
        if (slice.Projection is null && queries.Length == 0)
        {
            return null;
        }

        var name = slice.Projection?.ReadModel
            ?? queries.Select(query => query.ReturnType.Name).FirstOrDefault()
            ?? slice.Projection?.Name
            ?? slice.Name;

        var properties = CollectProperties(slice.Projection, queries, eventPropertyTypes);

        return new ReadModelDefinition(
            DeterministicId.From($"{slicePath}.readmodel.{name}"),
            name,
            schema.ForReadModel(properties),
            slice.Projection is { } projection ? ProjectionConverter.Convert(projection) : null);
    }

    static List<KeyValuePair<string, string?>> CollectProperties(
        ProjectionSyntax? projection,
        IEnumerable<QuerySyntax> queries,
        IReadOnlyDictionary<string, string> eventPropertyTypes)
    {
        var properties = new List<KeyValuePair<string, string?>>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Add(string propertyName, string? hint)
        {
            if (seen.Add(propertyName))
            {
                properties.Add(new(propertyName, hint));
            }
        }

        if (projection is not null)
        {
            foreach (var block in projection.Blocks)
            {
                CollectFromBlock(block, eventPropertyTypes, Add);
            }
        }

        foreach (var query in queries)
        {
            if (query.By is { } by)
            {
                Add(by.Name, by.Type.Name);
            }

            foreach (var filter in query.Filters)
            {
                Add(filter.Name, filter.Type.Name);
            }
        }

        return properties;
    }

    static void CollectFromBlock(ProjectionBlockSyntax block, IReadOnlyDictionary<string, string> eventPropertyTypes, Action<string, string?> add)
    {
        switch (block)
        {
            case FromSyntax from:
                CollectMappings(from.Mappings, eventPropertyTypes, add);
                break;
            case EverySyntax every:
                CollectMappings(every.Mappings, eventPropertyTypes, add);
                break;
            case AllSyntax all:
                CollectMappings(all.Mappings, eventPropertyTypes, add);
                break;
            case JoinSyntax join:
                foreach (var joinEvent in join.Events)
                {
                    CollectMappings(joinEvent.Mappings, eventPropertyTypes, add);
                }

                break;
            case ChildrenSyntax children:
                add(children.Property, null);
                break;
            case NestedSyntax nested:
                add(nested.Property, null);
                break;
        }
    }

    static void CollectMappings(IEnumerable<MappingSyntax> mappings, IReadOnlyDictionary<string, string> eventPropertyTypes, Action<string, string?> add)
    {
        foreach (var mapping in mappings)
        {
            var hint = mapping switch
            {
                IncrementMappingSyntax or DecrementMappingSyntax or CountMappingSyntax => "Int",
                AddMappingSyntax or SubtractMappingSyntax => "Decimal",
                _ => eventPropertyTypes.GetValueOrDefault(mapping.Property)
            };

            add(mapping.Property, hint);
        }
    }
}
