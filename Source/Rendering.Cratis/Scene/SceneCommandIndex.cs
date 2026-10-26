// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Scene;

/// <summary>Indexes command identity and stable Scene keys once for the whole application.</summary>
internal sealed class SceneCommandIndex
{
    public SceneCommandIndex(IReadOnlyList<LocatedSemanticSlice> slices)
    {
        var occurrences = slices.SelectMany(slice => slice.Slice.Commands.Select(command =>
            (Command: command, Slice: slice, Path: Path(slice, command)))).ToArray();
        var identities = occurrences.GroupBy(entry => entry.Command.Id).ToArray();
        var commands = identities.Select(group => group.First()).ToArray();
        var keys = StructureConverter.CommandKeys(commands.Select(entry => entry.Path));
        ById = commands.ToFrozenDictionary(
            entry => entry.Command.Id,
            entry => new SceneCommandEntry(entry.Command, entry.Slice, [.. entry.Path], keys[StructureConverter.CommandDescriptor(entry.Path)]));
        ByName = ById.Values.GroupBy(entry => entry.Command.Name, StringComparer.Ordinal)
            .ToFrozenDictionary(group => group.Key, group => group.Select(entry => entry.Command.Id).ToImmutableArray(), StringComparer.Ordinal);
        AmbiguousNames = [.. ByName.Where(pair => pair.Value.Length > 1).Select(pair => pair.Key).Order(StringComparer.Ordinal)];
        Collisions = [.. ById.Values.GroupBy(entry => entry.Key, StringComparer.Ordinal).Where(group => group.Count() > 1).SelectMany(group => group),
            .. identities.Where(group => group.Select(entry => StructureConverter.CommandDescriptor(entry.Path)).Distinct(StringComparer.Ordinal).Skip(1).Any())
                .Select(group => ById[group.Key])];
        ByKey = ById.Values.GroupBy(entry => entry.Key, StringComparer.Ordinal).Where(group => group.Count() == 1)
            .ToFrozenDictionary(group => group.Key, group => group.Single().Command.Id, StringComparer.Ordinal);
    }

    public FrozenDictionary<SemanticId, SceneCommandEntry> ById { get; }
    public FrozenDictionary<string, ImmutableArray<SemanticId>> ByName { get; }
    public FrozenDictionary<string, SemanticId> ByKey { get; }
    public ImmutableArray<string> AmbiguousNames { get; }
    public ImmutableArray<SceneCommandEntry> Collisions { get; }

    static string[] Path(LocatedSemanticSlice slice, SemanticCommand command) =>
        [slice.Module.Name, .. slice.FeaturePath.Select(feature => feature.Name), slice.Slice.Name, command.Name];
}
