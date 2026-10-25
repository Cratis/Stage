// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Decides the using directives of a generated C# file and spells each type it names by its short name wherever C#
/// provably binds that name to the intended type, falling back to the fully qualified <c language="csharp">global::</c>
/// name everywhere else. A type a global using already imports needs no directive; any other namespace is imported with a
/// <c language="csharp">using</c> directive above the file-scoped namespace, and only where that cannot be proven safe with
/// a globally rooted directive inside it. An import is added only when it cannot change what anything else in the file
/// binds to: it may not bring a type named like a name the resolver does not spell, nor an extension member named like a
/// member the file accesses. Spellings are decided against the final import set, and imports no short name needs are
/// dropped again, so the result is deterministic for a given request.
/// </summary>
internal static class ImportResolver
{
    /// <summary>
    /// Resolves a file against the checked-in framework name catalog.
    /// </summary>
    /// <param name="request">The file to resolve.</param>
    /// <returns>The using directives and spellings.</returns>
    public static ImportResolution Resolve(ImportRequest request) => Resolve(request, FrameworkNameCatalog.Current);

    /// <summary>
    /// Resolves a file against a framework name catalog.
    /// </summary>
    /// <param name="request">The file to resolve.</param>
    /// <param name="catalog">The framework names in scope.</param>
    /// <returns>The using directives and spellings.</returns>
    public static ImportResolution Resolve(ImportRequest request, FrameworkNameCatalog catalog)
    {
        var references = request.References.Distinct().ToArray();
        var required = request.RequiredImports.Distinct(StringComparer.Ordinal).ToArray();
        if (request.HasUnknownScope)
        {
            return new([], references.ToImmutableDictionary(reference => reference, reference => reference.Qualified))
            {
                TopLevelUsings = [.. required.Order(StringComparer.Ordinal)]
            };
        }

        var file = new FileImports(new NameScope(request, catalog), catalog, request, references);
        var kept = required.Where(file.NeedsRequired).ToArray();
        var top = file.Settle([], [.. kept, .. references.Select(reference => reference.Namespace).Distinct(StringComparer.Ordinal).Where(file.CanImportAtTop)], kept, out _);
        var spellings = file.Spell([], top);
        var unresolved = references.Where(reference => spellings[reference] == reference.Qualified)
            .Select(reference => reference.Namespace).Distinct(StringComparer.Ordinal).Where(file.CanImportInside).ToArray();
        List<string> inner = [];
        if (unresolved.Length > 0)
        {
            var innerTop = file.Settle(unresolved, top, kept, out var innerImports);

            // An import inside the namespace outranks every enclosing scope, so it may hide a name that already binds
            // outside it. Keep it only when nothing spelled short before loses its short spelling.
            var withInner = file.Spell(innerImports, innerTop);
            if (spellings.All(entry => entry.Value == entry.Key.Qualified || withInner[entry.Key] != entry.Key.Qualified))
            {
                (inner, top, spellings) = (innerImports, innerTop, withInner);
            }
        }

        return new([.. inner.Order(StringComparer.Ordinal)], spellings)
        {
            TopLevelUsings = [.. top.Order(StringComparer.Ordinal)]
        };
    }
}
