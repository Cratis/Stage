// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scene;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// The owners a render leaves out: the roots that cannot be rendered and the slice-granular closure of everything proven to depend on them.
/// </summary>
/// <remarks>
/// A slice artifact realizes the slice, its commands and its events in one file, so a slice that loses one declaration loses all of them.
/// Concepts and composite types never join the closure through slice co-location. The empty omission is inert.
/// </remarks>
internal sealed class SemanticOwnerOmission
{
    SemanticOwnerOmission(ImmutableDictionary<SemanticId, string> roots, ImmutableHashSet<SemanticId> closure, IReadOnlyDictionary<SemanticId, ImmutableArray<SemanticId>> causes)
    {
        Roots = roots;
        Closure = closure;
        OrderedClosure = [.. closure.OrderBy(owner => owner.ToString(), StringComparer.Ordinal)];
        Causes = causes;
    }

    /// <summary>Gets the omission that leaves nothing out.</summary>
    internal static SemanticOwnerOmission None { get; } = new([], [], new Dictionary<SemanticId, ImmutableArray<SemanticId>>());

    /// <summary>Gets each root owner with the diagnostic code that explains why it cannot be rendered.</summary>
    internal ImmutableDictionary<SemanticId, string> Roots { get; }

    /// <summary>Gets every omitted owner: the roots, their transitive dependents and the declarations of every affected slice.</summary>
    internal ImmutableHashSet<SemanticId> Closure { get; }

    internal IReadOnlyList<SemanticId> OrderedClosure { get; }

    internal IReadOnlyDictionary<SemanticId, ImmutableArray<SemanticId>> Causes { get; }

    /// <summary>Gets a value indicating whether nothing is omitted.</summary>
    internal bool IsEmpty => Closure.IsEmpty;

    /// <summary>Computes the closure of the roots to a fixpoint.</summary>
    /// <param name="context">The application whose owners are omitted.</param>
    /// <param name="index">The dependency index of the same application.</param>
    /// <param name="roots">Each root owner with its root diagnostic code.</param>
    /// <returns>The omission, or <see langword="null"/> when the closure cannot be proven and the render must stay refused.</returns>
    internal static SemanticOwnerOmission? Compute(SemanticApplicationContext context, SemanticDependencyIndex index, IReadOnlyDictionary<SemanticId, string> roots)
    {
        if (!index.IsSemanticGraphComplete && !SemanticDependencyCompleteness.Tolerates(index, roots, context)) return null;
        if (roots.Count == 0) return None;

        var declarations = Declarations(context, index);
        if (roots.Keys.Any(root => !declarations.Contains(root))) return null;

        var closure = Close(index, roots.Keys);
        if (!IsProvable(context, closure)) return null;
        var byRoot = roots.Keys.OrderBy(root => root.ToString(), StringComparer.Ordinal).ToDictionary(root => root, root => Close(index, [root]));
        var causes = closure.OrderBy(owner => owner.ToString(), StringComparer.Ordinal).ToDictionary(owner => owner, owner => ImmutableArray.CreateRange(byRoot.Where(pair => pair.Value.Contains(owner)).Select(pair => pair.Key).OrderBy(root => root.ToString(), StringComparer.Ordinal)));
        return new([.. roots], [.. closure], causes);
    }

    /// <summary>Finds the artifacts that realize an omitted owner.</summary>
    /// <param name="artifacts">The planned artifacts.</param>
    /// <param name="closure">The omitted owners.</param>
    /// <returns>The artifacts whose sources intersect the closure, in the order given.</returns>
    internal static IReadOnlyList<PlannedArtifact> Overlaps(IEnumerable<PlannedArtifact> artifacts, ImmutableHashSet<SemanticId> closure) =>
        closure.IsEmpty ? [] : [.. artifacts.Where(artifact => artifact.Sources.Any(closure.Contains))];

    internal static bool IsProvable(SemanticApplicationContext context, HashSet<SemanticId> closure)
    {
        if (closure.Contains(context.Application.Id)) return false;
        if (context.AllSlices.Any(located => closure.Contains(located.Module.Id) || located.FeaturePath.Any(feature => closure.Contains(feature.Id)))) return false;

        // The default composition is generated from the remaining slices; an authored Scene is not parsed, so its closure is unknown.
        var authoredScene = SceneCompositionInput.IsCarriedBy(context.Request.Profile) ||
            context.Request.Profile.Inputs.Any(input => input.Name == $"cratis-scaffold:text:{SceneCompositionInput.RelativePath}");
        return !authoredScene || !closure.Any(owner => context.Commands.ContainsKey(owner) || context.Queries.ContainsKey(owner));
    }

    /// <summary>Removes the omitted slices, keeping the order of the rest.</summary>
    /// <param name="slices">The slices.</param>
    /// <returns>The same instance when nothing is omitted; otherwise the remaining slices.</returns>
    internal IReadOnlyList<LocatedSemanticSlice> Apply(IReadOnlyList<LocatedSemanticSlice> slices) =>
        IsEmpty ? slices : [.. slices.Where(located => !Closure.Contains(located.Slice.Id))];

    /// <summary>Removes the typed-context descriptors whose operation is omitted, keeping the order of the rest.</summary>
    /// <param name="descriptors">The descriptors.</param>
    /// <returns>The same instance when nothing is omitted; otherwise the remaining descriptors.</returns>
    internal IEnumerable<SemanticTypedContextDescriptor> Apply(IEnumerable<SemanticTypedContextDescriptor> descriptors) =>
        IsEmpty ? descriptors : descriptors.Where(descriptor => descriptor.OperationId is not { } operation || !Closure.Contains(operation));

    static HashSet<SemanticId> Declarations(SemanticApplicationContext context, SemanticDependencyIndex index) =>
    [
        .. context.AllSlices.Select(located => located.Slice.Id),
        .. index.Edges.Where(edge => edge.Kind == SemanticDependencyKind.Declaration).Select(edge => edge.Dependency),
        .. context.Concepts.Keys,
        .. context.Types.Keys
    ];

    static HashSet<SemanticId> Close(SemanticDependencyIndex index, IEnumerable<SemanticId> roots)
    {
        var siblings = index.Edges.Where(edge => edge.Kind == SemanticDependencyKind.Declaration)
            .GroupBy(edge => edge.Dependent).ToDictionary(group => group.Key, group => group.Select(edge => edge.Dependency).ToArray());
        var closure = new HashSet<SemanticId>();
        var pending = new Queue<SemanticId>(roots);
        while (pending.TryDequeue(out var owner))
        {
            if (!closure.Add(owner)) continue;
            foreach (var dependent in index.Dependents(owner)) pending.Enqueue(dependent);

            // A slice artifact cannot be split: every declaration of an affected slice goes with it.
            if (siblings.TryGetValue(owner, out var own))
            {
                foreach (var declaration in own) pending.Enqueue(declaration);
            }
        }

        return closure;
    }
}
