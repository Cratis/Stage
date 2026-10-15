// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneStructure = Cratis.Scene.Model.Structure;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Builds Scene's <see cref="SceneStructure.ApplicationStructure"/> - the read-only list of modules, features and slices (and each
/// slice's kind) that navigation targets and toolbar buttons resolve against - from a compiled Screenplay application.
/// </summary>
/// <remarks>
/// <para>
/// The structure is a reference input to Scene's validation, never document content. A node's id is the Screenplay name, so
/// the qualified id of a slice is the dotted path a <c language="csharp">button ... slice</c> or a navigation <c language="csharp">target</c> is written
/// with: <c language="csharp">Sales.Orders.PlaceOrder</c>.
/// </para>
/// <para>
/// Scene knows three slice kinds. A Screenplay <c language="csharp">Translate</c> slice converts external data into events and
/// has no user input, exactly like an automation, so it is an <see cref="SceneStructure.SliceKind.Automation"/> here - which is
/// what keeps a button from invoking it. A slice's <c language="csharp">Command</c> is its first command; a slice that declares none carries none.
/// </para>
/// </remarks>
public static class StructureConverter
{
    /// <summary>
    /// Converts the modules, features and slices of an application.
    /// </summary>
    /// <param name="application">The compiled <see cref="ScreenplaySyntax.ApplicationSyntax"/>.</param>
    /// <returns>The <see cref="SceneStructure.ApplicationStructure"/>.</returns>
    public static SceneStructure.ApplicationStructure Convert(ScreenplaySyntax.ApplicationSyntax application) =>
        new([.. application.Modules.Select(module => new SceneStructure.ModuleNode(
            module.Name,
            module.Name,
            [.. module.Features.Select(Convert)]))]);

    /// <summary>
    /// Resolves the slice a toolbar button names to its qualified id.
    /// </summary>
    /// <param name="structure">The structure to resolve against.</param>
    /// <param name="reference">The dotted slice reference as authored - the full path or an unambiguous suffix of it.</param>
    /// <returns>The qualified id, or <paramref name="reference"/> as written when it names no single slice.</returns>
    public static string QualifiedSlice(SceneStructure.ApplicationStructure? structure, string reference)
    {
        if (structure is null)
        {
            return reference;
        }

        var wanted = reference.Split('.');
        var matches = Paths(structure).Where(path => path.Length >= wanted.Length && path.TakeLast(wanted.Length).SequenceEqual(wanted, StringComparer.Ordinal)).ToList();
        return matches.Count == 1 ? string.Join('.', matches[0]) : reference;
    }

    static SceneStructure.FeatureNode Convert(ScreenplaySyntax.FeatureSyntax feature) =>
        new(
            feature.Name,
            feature.Name,
            [.. feature.Features.Select(Convert)],
            [.. feature.Slices.Select(Convert)]);

    static SceneStructure.SliceNode Convert(ScreenplaySyntax.SliceSyntax slice) =>
        new(slice.Name, slice.Name, KindOf(slice.Type), slice.Commands.FirstOrDefault()?.Name);

    static SceneStructure.SliceKind KindOf(ScreenplaySyntax.SliceType type) => type switch
    {
        ScreenplaySyntax.SliceType.StateChange => SceneStructure.SliceKind.StateChange,
        ScreenplaySyntax.SliceType.StateView => SceneStructure.SliceKind.StateView,
        _ => SceneStructure.SliceKind.Automation
    };

    static IEnumerable<string[]> Paths(SceneStructure.ApplicationStructure structure) =>
        structure.Modules.SelectMany(module => Paths(module.Features, [module.Id]));

    static IEnumerable<string[]> Paths(IEnumerable<SceneStructure.FeatureNode> features, string[] prefix)
    {
        foreach (var feature in features)
        {
            string[] path = [.. prefix, feature.Id];
            foreach (var slice in feature.Slices)
            {
                yield return [.. path, slice.Id];
            }

            foreach (var nested in Paths(feature.Features, path))
            {
                yield return nested;
            }
        }
    }
}
