// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Indexes every type a slice declares — its events, commands, read models and reactors — by name, so a
/// reference from another slice can be resolved back to the folder and namespace it is rendered into.
/// </summary>
public static class Declarations
{
    /// <summary>
    /// Builds the index. The first slice to declare a name wins; Screenplay names are globally unique by
    /// convention, and a later duplicate would otherwise silently change where every reference points.
    /// </summary>
    /// <param name="slices">The slices to index.</param>
    /// <returns>The declared name to declaring slice path map.</returns>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Index(IEnumerable<LocatedSlice> slices)
    {
        var index = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

        foreach (var slice in slices)
        {
            foreach (var name in DeclaredNames(slice))
            {
                index.TryAdd(name, slice.FullPath);
            }
        }

        return index;
    }

    static IEnumerable<string> DeclaredNames(LocatedSlice slice) =>
        slice.Slice.Events.Select(@event => @event.Name)
            .Concat(slice.Slice.Commands.Select(command => command.Name))
            .Concat(slice.Slice.Reactions.Select(reaction => reaction.Name))
            .Concat(slice.Slice.Projections.Select(projection => projection.ReadModel ?? projection.Name));
}
