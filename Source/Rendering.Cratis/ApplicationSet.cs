// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

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
        Policies = BuildLookup(applications.SelectMany(application => application.Policies), policy => policy.Name);
        Types = BuildLookup(applications.SelectMany(application => application.Types ?? []), type => type.Name);
        Slices = [.. applications.SelectMany(application => application.Locate())];
        ImportedNames = new HashSet<string>(
            applications.SelectMany(application => application.Imports).Select(import => import.Name), StringComparer.Ordinal);
        DeclarationPlacements = Declarations.Index(Slices);
        Events = BuildLookup(Slices.SelectMany(slice => slice.Slice.Events), @event => @event.Name);
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
    /// Gets every policy declared across the applications, keyed by name. An <c>authorize</c> names policies rather
    /// than roles, so every rendered authorization attribute resolves through this.
    /// </summary>
    public IReadOnlyDictionary<string, PolicySyntax> Policies { get; }

    /// <summary>
    /// Gets every slice declared across the applications, located by its module/feature path.
    /// </summary>
    public IReadOnlyList<LocatedSlice> Slices { get; }

    /// <summary>
    /// Gets every event declared across the applications, keyed by name. A command producing an event declared in
    /// another slice needs its full property list to construct it, not just the properties it maps.
    /// </summary>
    public IReadOnlyDictionary<string, EventSyntax> Events { get; }

    /// <summary>
    /// Gets, for every type a slice declares — its events, commands, read models and reactors — the module/feature
    /// path of the slice declaring it. A State View projecting another slice's event resolves its import
    /// directive through this.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> DeclarationPlacements { get; }

    /// <summary>
    /// Gets the short names of every <c>import</c> declared across the applications — constructs owned by another
    /// domain, which this application references but does not declare and therefore cannot render.
    /// </summary>
    public IReadOnlySet<string> ImportedNames { get; }

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

    static IReadOnlyList<string> ResolvePlacement(Dictionary<string, LocatedSlice>.ValueCollection? slices)
    {
        if (slices is null || slices.Count == 0)
        {
            return [];
        }

        return slices.Count == 1 ? slices.Single().FullPath : Placement.LowestCommonAncestor(slices.Select(slice => slice.Path));
    }

    /// <summary>
    /// Finds the concepts that are entity identities rather than plain values. A concept used by a command
    /// property marked <c>identifier</c> is one; so is a concept a projection uses as its <c>key</c>, since a read
    /// model's key is the event source it is built from — rendering it as a plain <c>ConceptAs&lt;T&gt;</c> leaves
    /// it with no conversion to <c>EventSourceId</c>, which every generated by-id query needs.
    /// </summary>
    /// <returns>The names of the identity concepts.</returns>
    HashSet<string> FindIdentifierConceptNames()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        var identifierProperties = Slices
            .SelectMany(slice => slice.Slice.Commands)
            .SelectMany(command => command.Properties)
            .Where(property => property.IsIdentifier)
            .Select(property => property.Type.Name);

        foreach (var name in identifierProperties.Concat(ProjectionKeyConceptNames()).Where(Concepts.ContainsKey))
        {
            names.Add(name);
        }

        return names;
    }

    IEnumerable<string> ProjectionKeyConceptNames() =>
        Slices.SelectMany(slice => slice.Slice.Projections)
            .SelectMany(projection => projection.Blocks.OfType<FromSyntax>())
            .Select(KeyConceptName)
            .OfType<string>();

    string? KeyConceptName(FromSyntax from)
    {
        if (from.Key is not ExpressionKeySyntax { Expression: PathExpressionSyntax path })
        {
            return null;
        }

        var eventName = from.Events.FirstOrDefault()?.Event;
        var declared = eventName is null ? null : Events.GetValueOrDefault(eventName);
        return declared?.Properties
            .FirstOrDefault(property => string.Equals(property.Name, path.Path, StringComparison.OrdinalIgnoreCase))?.Type.Name;
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
