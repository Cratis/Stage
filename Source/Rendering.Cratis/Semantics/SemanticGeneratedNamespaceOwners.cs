// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Locates modeled namespaces that collide with application-owned runtime declarations.
/// The runtime must remain available to independently admitted slices.
/// </summary>
internal static class SemanticGeneratedNamespaceOwners
{
    internal static SemanticId[] For(
        IReadOnlyList<LocatedSemanticSlice> slices,
        IReadOnlyList<GeneratedTypeDeclaration> declarations,
        SemanticId application,
        (SemanticId Artifact, string Kind, string Name) collision)
    {
        var (artifact, kind, name) = collision;
        if (artifact != application || !string.Equals(kind, "Namespace", StringComparison.Ordinal)) return [artifact];
        var qualified = declarations.Where(declaration => declaration.Artifact == artifact && declaration.Name == name)
            .Select(declaration => string.Join('.', new[] { declaration.Namespace, declaration.Name }.Where(part => part.Length > 0)))
            .ToHashSet(StringComparer.Ordinal);
        var owners = slices.SelectMany(located => Namespaces(located).Where(part => qualified.Contains(part.Name)).Select(part => part.Owner)).Distinct().ToArray();
        return owners.Length > 0 ? owners : [artifact];
    }

    static IEnumerable<(string Name, SemanticId Owner)> Namespaces(LocatedSemanticSlice located)
    {
        var moduleLength = located.Path.Count - located.FeaturePath.Count - 1;
        yield return (Namespace(located.Path.Take(moduleLength)), located.Module.Id);
        for (var index = 0; index < located.FeaturePath.Count; index++)
        {
            yield return (Namespace(located.Path.Take(moduleLength + index + 1)), located.FeaturePath[index].Id);
        }
        yield return (Namespace(located.Path), located.Slice.Id);
    }

    static string Namespace(IEnumerable<string> path) => string.Join('.', path.Select(GeneratedPascalCase.From));
}
