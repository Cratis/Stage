// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming;

/// <summary>
/// Resolves the import directives a rendered file needs for the Screenplay names it references. Every
/// renderer resolves through here, so a concept, a composite type and another slice's event are all found the
/// same way — the alternative, each renderer walking only the references it happens to know about, is what makes
/// a cross-slice event reference compile against nothing.
/// </summary>
public static class ReferencedNamespaces
{
    /// <summary>
    /// Resolves the namespaces to import for a set of referenced Screenplay names.
    /// </summary>
    /// <param name="names">The referenced Screenplay names.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to resolve against.</param>
    /// <param name="rootNamespace">The root namespace of the target application.</param>
    /// <param name="ownNamespace">The namespace of the file being rendered — never imported into itself.</param>
    /// <returns>The namespaces to import, sorted.</returns>
    public static IReadOnlyList<string> Resolve(
        IEnumerable<string> names, ApplicationSet applicationSet, string rootNamespace, string ownNamespace)
    {
        var namespaces = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var name in names)
        {
            var @namespace = For(name, applicationSet, rootNamespace);
            if (@namespace is not null && !string.Equals(@namespace, ownNamespace, StringComparison.Ordinal))
            {
                namespaces.Add(@namespace);
            }
        }

        return [.. namespaces];
    }

    /// <summary>
    /// Resolves the namespace a single Screenplay name is rendered into.
    /// </summary>
    /// <param name="name">The Screenplay name.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to resolve against.</param>
    /// <param name="rootNamespace">The root namespace of the target application.</param>
    /// <returns>The namespace, or <see langword="null"/> when the name is not declared anywhere.</returns>
    public static string? For(string name, ApplicationSet applicationSet, string rootNamespace)
    {
        if (applicationSet.Concepts.ContainsKey(name) || applicationSet.Types.ContainsKey(name))
        {
            return ForPlacement(rootNamespace, applicationSet.ConceptPlacements.GetValueOrDefault(name, []));
        }

        return applicationSet.DeclarationPlacements.TryGetValue(name, out var placement)
            ? SliceNaming.Namespace(rootNamespace, placement)
            : null;
    }

    /// <summary>
    /// Computes the namespace for a placement path — an empty path meaning the application-wide <c>Common</c>
    /// folder.
    /// </summary>
    /// <param name="rootNamespace">The root namespace of the target application.</param>
    /// <param name="placement">The module/feature path segments.</param>
    /// <returns>The namespace.</returns>
    public static string ForPlacement(string rootNamespace, IReadOnlyList<string> placement) =>
        placement.Count == 0 ? $"{rootNamespace}.Common" : SliceNaming.Namespace(rootNamespace, placement);
}
