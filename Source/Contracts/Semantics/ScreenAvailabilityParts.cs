// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Semantics;

/// <summary>
/// The exact Arc part keys of gated Scene screens, shared by rendered and live routes.
/// </summary>
public static class ScreenAvailabilityParts
{
    /// <summary>
    /// Maps screen names to marker keys, including screens with inherited module, feature or slice gates.
    /// </summary>
    /// <param name="application">The semantic application.</param>
    /// <returns>The screens that must wait for an availability decision before being shown.</returns>
    public static IReadOnlyDictionary<string, string> For(SemanticApplication application) =>
        application.Modules.SelectMany(module => module.Features.SelectMany(feature => GatedScreens(feature, !module.Available.IsDefaultOrEmpty)))
            .OrderBy(screen => screen.Name, StringComparer.Ordinal)
            .ToDictionary(screen => screen.Name, Key, StringComparer.Ordinal);

    /// <summary>
    /// Finds the screen elements that carry an explicit <c language="text">available</c> gate: an action bound to a command and a data element bound to a query.
    /// </summary>
    /// <param name="application">The semantic application.</param>
    /// <returns>The gated elements in a stable order, each with the exact marker key that carries its gate.</returns>
    public static IReadOnlyList<ScreenElementGate> Elements(SemanticApplication application)
    {
        var slices = application.Modules.SelectMany(module => module.Features.SelectMany(AllSlices)).ToArray();
        var commands = slices.SelectMany(slice => slice.Commands).GroupBy(command => command.Id).ToDictionary(group => group.Key, group => group.First().Name);
        var queries = slices.SelectMany(slice => slice.Queries).GroupBy(query => query.Id).ToDictionary(group => group.Key, group => group.First().Name);
        var gates = new List<ScreenElementGate>();
        foreach (var screen in slices.SelectMany(slice => slice.Screens))
        {
            foreach (var action in screen.Actions.Where(action => !action.Available.IsDefaultOrEmpty && commands.ContainsKey(action.Command)))
            {
                gates.Add(new(screen.Name, ScreenElementKind.Action, commands[action.Command], ElementKey(screen, "action", action.Command), action.Available));
            }

            foreach (var data in screen.Data.Where(data => !data.Available.IsDefaultOrEmpty && queries.ContainsKey(data.Query)))
            {
                gates.Add(new(screen.Name, ScreenElementKind.Data, queries[data.Query], ElementKey(screen, "data", data.Query), data.Available));
            }
        }

        return [.. gates.OrderBy(gate => gate.Key, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Maps each gated screen element to its marker key. The lookup key is <see cref="ElementLookup"/>.
    /// </summary>
    /// <param name="application">The semantic application.</param>
    /// <returns>The marker key of every gated element; an element two gates share the lookup of keeps the first.</returns>
    public static IReadOnlyDictionary<string, string> ElementKeys(SemanticApplication application)
    {
        var keys = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var gate in Elements(application)) keys.TryAdd(ElementLookup(gate.Screen, gate.Kind, gate.Name), gate.Key);
        return keys;
    }

    /// <summary>
    /// The key a client finds an element's marker by: the screen, the kind of element and the name of the command or query it is bound to.
    /// </summary>
    /// <param name="screen">The screen name.</param>
    /// <param name="kind">The element kind.</param>
    /// <param name="name">The bound command or query name.</param>
    /// <returns>The lookup key.</returns>
    public static string ElementLookup(string screen, ScreenElementKind kind, string name) => $"{screen}/{(kind == ScreenElementKind.Action ? "action" : "data")}/{name}";

    static IEnumerable<SemanticSlice> AllSlices(SemanticFeature feature) =>
        feature.Slices.Concat(feature.Features.SelectMany(AllSlices));

    static string ElementKey(SemanticScreen screen, string kind, SemanticId bound) => $"StageElement_{screen.Id.ToString().Replace(':', '_')}_{kind}_{bound.ToString().Replace(':', '_')}";

    static IEnumerable<SemanticScreen> GatedScreens(SemanticFeature feature, bool inherited)
    {
        var gated = inherited || !feature.Available.IsDefaultOrEmpty;
        return feature.Slices.SelectMany(slice => slice.Screens.Where(screen => gated || !slice.Available.IsDefaultOrEmpty || !screen.Available.IsDefaultOrEmpty))
            .Concat(feature.Features.SelectMany(child => GatedScreens(child, gated)));
    }

    static string Key(SemanticScreen screen) => $"StageScreen_{screen.Id.ToString().Replace(':', '_')}";
}
