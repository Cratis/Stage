// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Checks capture declarations against C# identities without changing unrelated declaration naming.
/// </summary>
internal static class SemanticCaptureDeclarationCollisions
{
    internal static IEnumerable<(SemanticId Artifact, string Kind, string Name)> Find(
        IReadOnlyList<LocatedSemanticSlice> slices,
        IReadOnlyList<GeneratedTypeDeclaration> declarations)
    {
        var namespaces = slices.SelectMany(located =>
        {
            var path = located.Path.Select(Identifiers.ToPascalCase).ToArray();
            return Enumerable.Range(0, path.Length).Select(index => SemanticCaptureIdentifier.Qualified(string.Join('.', path.Take(index + 1))));
        }).ToHashSet(StringComparer.Ordinal);
        foreach (var capture in declarations.Where(declaration => declaration.Kind == "Capture"))
        {
            var qualified = SemanticCaptureIdentifier.Qualified($"{capture.Namespace}.{capture.Name}");
            if (declarations.Any(other => IsDecodedAlias(capture, other, qualified)))
            {
                yield return (capture.Artifact, capture.Kind, capture.Name);
            }
            if (namespaces.Contains(qualified)) yield return (capture.Artifact, "Namespace", capture.Name);
        }
    }

    static bool IsDecodedAlias(GeneratedTypeDeclaration capture, GeneratedTypeDeclaration other, string qualified)
    {
        if (other.Namespace == capture.Namespace && other.Name == capture.Name) return false;
        return SemanticCaptureIdentifier.Qualified($"{other.Namespace}.{other.Name}") == qualified;
    }
}
