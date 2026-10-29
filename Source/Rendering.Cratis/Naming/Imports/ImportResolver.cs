// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Decides the using directives of a generated C# file and spells each type it names by its short name wherever C#
/// provably binds that name to the intended type, using deterministic namespace aliases for known collisions.
/// Unknown identities or scopes are refused. A type a global using already imports needs no directive; any other namespace is imported with a
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
        ImportAliases.Verify(request, catalog);

        var file = new FileImports(new NameScope(request, catalog), catalog, request, references);
        var scaffold = new ScaffoldBindings(request, catalog);
        var kept = required.Where(file.NeedsRequired).ToArray();
        var topCandidates = new List<string>([.. kept, .. references.Select(reference => reference.Namespace).Distinct(StringComparer.Ordinal).Where(file.CanImportAtTop)]);
        var top = file.Settle([], topCandidates, kept, out _);
        var spellings = file.Spell([], top);
        var candidates = references.Where(reference => spellings[reference] == reference.Qualified)
            .Select(reference => reference.Namespace).Distinct(StringComparer.Ordinal).ToArray();
        var unresolved = candidates.Where(@namespace => file.CanImportInside(@namespace, candidates, top)).ToArray();
        List<string> inner = [];
        if (unresolved.Length > 0)
        {
            (inner, top, spellings) = TryInner(file, scaffold, references, unresolved, topCandidates, kept, (inner, top, spellings));
        }

        if (!request.HasScaffoldGlobalUsings) top = [.. top.Concat(file.ScaffoldImports(required, spellings, inner)).Distinct(StringComparer.Ordinal)];

        var aliased = ImportAliases.Resolve(request, spellings, scaffold);
        return new([.. inner.Order(StringComparer.Ordinal)], aliased.Spellings)
        {
            Aliases = aliased.Directives,
            TopLevelUsings = [.. top.Order(StringComparer.Ordinal)],
            RootedUsings = [.. inner.Where(@namespace => !file.BindsFromGlobal(@namespace))]
        };
    }

    static (List<string> Inner, List<string> Top, ImmutableDictionary<TypeReference, string> Spellings) TryInner(
        FileImports file,
        ScaffoldBindings scaffold,
        TypeReference[] references,
        string[] unresolved,
        List<string> topCandidates,
        string[] kept,
        (List<string> Inner, List<string> Top, ImmutableDictionary<TypeReference, string> Spellings) current)
    {
        var innerTop = file.Settle(unresolved, current.Top, kept, out var innerImports);

        // An import inside the namespace outranks every enclosing scope, so it may hide a name that already binds
        // outside it. Keep it only when nothing spelled short before loses its short spelling.
        var withInner = file.Spell(innerImports, innerTop);
        var best = current;
        if (current.Spellings.All(entry => entry.Value == entry.Key.Qualified || withInner[entry.Key] != entry.Key.Qualified))
        {
            best = (innerImports, innerTop, withInner);
        }

        // Two namespaces that declare the same name cannot both be imported at the same level without making that
        // name ambiguous. An inner import outranks the one above the namespace, so exactly one of them, chosen by
        // a fixed rule, takes the inner level and only its colliding name stays short; the other loses just that name.
        if (!references.Any(reference => unresolved.Contains(reference.Namespace) && best.Spellings[reference] == reference.Qualified))
        {
            return best;
        }

        var winners = new List<string>();
        foreach (var @namespace in unresolved.OrderBy(name => file.IsGenerated(name) ? 0 : 1)
            .ThenByDescending(name => references.Count(reference => reference.Namespace == name))
            .ThenBy(name => name, StringComparer.Ordinal))
        {
            if (references.Any(reference => reference.Namespace == @namespace && winners.Exists(winner => file.Declares(winner, reference.Name))))
            {
                continue;
            }

            var trial = file.Settle([.. winners, @namespace], topCandidates, kept, out var trialInner);
            var spelled = file.Spell(trialInner, trial);
            var gains = references.Any(reference => reference.Namespace == @namespace && spelled[reference] != reference.Qualified);
            var keeps = KeepsShortNames(current.Spellings, spelled) && scaffold.KeepsBindings(current.Spellings, spelled);
            if (gains && keeps)
            {
                winners.Add(@namespace);
            }
        }

        if (winners.Count == 0)
        {
            return best;
        }

        var finalTop = file.Settle(winners, topCandidates, kept, out var finalInner);
        var finalSpellings = file.Spell(finalInner, finalTop);
        var shortBefore = best.Spellings.Count(entry => entry.Value != entry.Key.Qualified);
        var shortAfter = finalSpellings.Count(entry => entry.Value != entry.Key.Qualified);
        var safe = KeepsShortNames(current.Spellings, finalSpellings) && scaffold.KeepsBindings(current.Spellings, finalSpellings);
        var better = shortAfter > shortBefore || (shortAfter == shortBefore && winners.Exists(file.IsGenerated));
        return safe && better ? (finalInner, finalTop, finalSpellings) : best;
    }

    // A name that was short may become qualified only when another type of the same name took it, which is a collision.
    static bool KeepsShortNames(ImmutableDictionary<TypeReference, string> before, ImmutableDictionary<TypeReference, string> after) =>
        before.All(entry => entry.Value == entry.Key.Qualified || after[entry.Key] != entry.Key.Qualified ||
            after.Any(other => other.Key != entry.Key && other.Key.Name == entry.Key.Name && other.Value != other.Key.Qualified));
}
