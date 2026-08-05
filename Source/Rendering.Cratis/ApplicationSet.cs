// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Merges one or more compiled Screenplay applications into the lookup tables rendering needs — concepts and
/// composite types are shared across every application, since a slice in one file may reference a concept
/// declared in another.
/// </summary>
public class ApplicationSet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationSet"/> class.
    /// </summary>
    /// <param name="applications">The compiled applications to merge.</param>
    public ApplicationSet(IReadOnlyList<ApplicationSyntax> applications)
    {
        Applications = applications;
        Concepts = BuildLookup(applications.SelectMany(application => application.Concepts), concept => concept.Name);
        Types = BuildLookup(applications.SelectMany(application => application.Types ?? []), type => type.Name);
        Slices = [.. applications.SelectMany(LocateSlices)];
        IdentifierConceptNames = FindIdentifierConceptNames();
        ConceptPlacements = BuildConceptPlacements();
    }

    /// <summary>
    /// Gets the applications the set was built from.
    /// </summary>
    public IReadOnlyList<ApplicationSyntax> Applications { get; }

    /// <summary>
    /// Gets every concept declared across the applications, keyed by name.
    /// </summary>
    public IReadOnlyDictionary<string, ConceptSyntax> Concepts { get; }

    /// <summary>
    /// Gets every composite type declared across the applications, keyed by name.
    /// </summary>
    public IReadOnlyDictionary<string, TypeSyntax> Types { get; }

    /// <summary>
    /// Gets every slice declared across the applications, located by its module/feature path.
    /// </summary>
    public IReadOnlyList<LocatedSlice> Slices { get; }

    /// <summary>
    /// Gets the names of every concept used by at least one command property marked <c>identifier</c> — these
    /// concepts render as <c>EventSourceId&lt;T&gt;</c> rather than <c>ConceptAs&lt;T&gt;</c>.
    /// </summary>
    public IReadOnlySet<string> IdentifierConceptNames { get; }

    /// <summary>
    /// Gets, for every concept and composite type, the module/feature path segments it should be rendered
    /// under — the lowest folder level at which every slice that uses it can see it. A concept used by exactly
    /// one slice is placed in that slice's own folder; used by several slices in one feature, the feature
    /// folder; across features of one module, the module folder; across modules (or unused), an empty path,
    /// meaning the application-wide <c>Common</c> folder.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ConceptPlacements { get; }

    static Dictionary<string, TSyntax> BuildLookup<TSyntax>(IEnumerable<TSyntax> items, Func<TSyntax, string> name)
    {
        var lookup = new Dictionary<string, TSyntax>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            lookup[name(item)] = item;
        }

        return lookup;
    }

    static IEnumerable<LocatedSlice> LocateSlices(ApplicationSyntax application) =>
        application.Modules.SelectMany(module => LocateSlices(module.Features, [module.Name]));

    static IEnumerable<LocatedSlice> LocateSlices(IEnumerable<FeatureSyntax> features, IReadOnlyList<string> path) =>
        features.SelectMany(feature =>
        {
            IReadOnlyList<string> featurePath = [.. path, feature.Name];
            return feature.Slices
                .Select(slice => new LocatedSlice(slice, featurePath))
                .Concat(LocateSlices(feature.Features, featurePath));
        });

    static IReadOnlyList<string> ResolvePlacement(Dictionary<string, LocatedSlice>.ValueCollection? slices)
    {
        if (slices is null || slices.Count == 0)
        {
            return [];
        }

        return slices.Count == 1 ? slices.Single().FullPath : Placement.LowestCommonAncestor(slices.Select(slice => slice.Path));
    }

    HashSet<string> FindIdentifierConceptNames()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        var identifierProperties = Slices
            .SelectMany(slice => slice.Slice.Commands)
            .SelectMany(command => command.Properties)
            .Where(property => property.IsIdentifier);

        foreach (var property in identifierProperties)
        {
            if (Concepts.ContainsKey(property.Type.Name))
            {
                names.Add(property.Type.Name);
            }
        }

        return names;
    }

    Dictionary<string, IReadOnlyList<string>> BuildConceptPlacements()
    {
        var usage = new Dictionary<string, Dictionary<string, LocatedSlice>>(StringComparer.Ordinal);

        void Track(string name, LocatedSlice slice)
        {
            if (!Concepts.ContainsKey(name) && !Types.ContainsKey(name))
            {
                return;
            }

            if (!usage.TryGetValue(name, out var slices))
            {
                slices = new Dictionary<string, LocatedSlice>(StringComparer.Ordinal);
                usage[name] = slices;
            }

            slices[string.Join('/', slice.FullPath)] = slice;
        }

        foreach (var slice in Slices)
        {
            var referencedNames = slice.Slice.Commands.SelectMany(command => command.Properties)
                .Concat(slice.Slice.Events.SelectMany(@event => @event.Properties))
                .Select(property => property.Type.Name)
                .Distinct(StringComparer.Ordinal);

            foreach (var name in referencedNames)
            {
                Track(name, slice);
            }
        }

        // Propagate one level: a composite type's own usage scope extends to concepts/types its properties reference.
        foreach (var name in usage.Keys.ToArray())
        {
            if (!Types.TryGetValue(name, out var type))
            {
                continue;
            }

            var referencingSlices = usage[name].Values.ToArray();
            foreach (var nestedName in type.Properties.Select(property => property.Type.Name).Distinct(StringComparer.Ordinal))
            {
                foreach (var slice in referencingSlices)
                {
                    Track(nestedName, slice);
                }
            }
        }

        var placements = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var name in Concepts.Keys.Concat(Types.Keys))
        {
            placements[name] = ResolvePlacement(usage.GetValueOrDefault(name)?.Values);
        }

        return placements;
    }
}
