// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Decides the using directives of a generated C# file and spells each type it names by its short name wherever C#
/// provably binds that name to the intended type, falling back to the fully qualified <c language="csharp">global::</c>
/// name everywhere else. An import is added only when it cannot change what anything else in the file binds to: it may
/// not bring a type named like a name the resolver does not spell, nor an extension member named like a member the file
/// accesses. Spellings are decided against the final import set, and imports no short name needs are dropped again, so
/// the result is deterministic for a given request.
/// </summary>
internal static class ImportResolver
{
    // Members C# binds by pattern rather than by an explicit call: awaiting, iterating, deconstructing, collection
    // initializers and query expressions can each pick up an extension member a new import brings into scope.
    static readonly ImmutableHashSet<string> _patternMembers =
    [
        "Add", "Cast", "Deconstruct", "Dispose", "DisposeAsync", "GetAsyncEnumerator", "GetAwaiter", "GetEnumerator", "GetPinnableReference",
        "GroupBy", "GroupJoin", "Join", "OrderBy", "OrderByDescending", "Select", "SelectMany", "ThenBy", "ThenByDescending", "Where"
    ];

    // Contextual keywords a generated file writes as identifiers; a type of one of these names would rebind them.
    static readonly ImmutableHashSet<string> _contextualNames = ["dynamic", "field", "managed", "nameof", "nint", "notnull", "nuint", "unmanaged", "value", "var"];

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
        if (request.HasUnknownScope || catalog.HasUnmodeledUsings(request.Project))
        {
            return new([.. required.Order(StringComparer.Ordinal)], references.ToImmutableDictionary(reference => reference, reference => reference.Qualified));
        }

        var scope = new NameScope(request, catalog);
        var imported = catalog.GlobalUsings(request.Project).Concat(required).ToHashSet(StringComparer.Ordinal);
        var unspelled = request.PinnedNames.Concat(_contextualNames).ToHashSet(StringComparer.Ordinal);
        var accessed = request.MemberAccessNames.Concat(_patternMembers).ToHashSet(StringComparer.Ordinal);
        var chosen = references.Select(reference => reference.Namespace).Distinct(StringComparer.Ordinal)
            .Where(@namespace => CanImport(scope, @namespace, imported, unspelled, accessed))
            .Order(StringComparer.Ordinal)
            .ToList();
        while (true)
        {
            var imports = imported.Concat(chosen).ToArray();
            var spellings = references.ToImmutableDictionary(reference => reference, reference => Spell(scope, reference, imports));
            var used = chosen.Where(@namespace => references.Any(reference => reference.Namespace == @namespace && spellings[reference] != reference.Qualified)).ToList();
            if (used.Count == chosen.Count)
            {
                return new([.. required.Concat(chosen).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)], spellings);
            }

            chosen = used;
        }
    }

    static bool CanImport(NameScope scope, string @namespace, HashSet<string> imported, HashSet<string> unspelled, HashSet<string> accessed)
    {
        if (@namespace.Length == 0 || imported.Contains(@namespace) || scope.Encloses(@namespace) || !scope.Knows(@namespace)) return false;

        // The directive itself is looked up from the global namespace, where a type would hide its first segment.
        if (scope.TypeNames(string.Empty).Contains(@namespace.Split('.')[0], StringComparer.Ordinal)) return false;
        if (scope.ExtensionNames(@namespace).Any(accessed.Contains)) return false;
        return !scope.TypeNames(@namespace).Any(name => unspelled.Contains(name) ||
            (name.EndsWith("Attribute", StringComparison.Ordinal) && unspelled.Contains(name[..^"Attribute".Length])));
    }

    static string Spell(NameScope scope, TypeReference reference, IReadOnlyCollection<string> imports)
    {
        if (SyntaxFacts.GetContextualKeywordKind(reference.Name) != SyntaxKind.None) return reference.Qualified;
        return reference.Context switch
        {
            ReferenceContext.Attribute => Attribute(scope, reference, imports),
            ReferenceContext.NameOf when scope.ValueNames.Contains("nameof") => reference.Qualified,
            ReferenceContext.Expression or ReferenceContext.NameOf when scope.ValueNames.Contains(reference.Name) => reference.Qualified,
            _ => Binds(scope, reference.Name, reference, imports) ? Identifiers.EscapeKeyword(reference.Name) : reference.Qualified
        };
    }

    // [Key] is looked up both as Key and as KeyAttribute; [KeyAttribute] both as KeyAttribute and as KeyAttributeAttribute.
    // A spelling is used only when its other candidate binds to nothing at all.
    static string Attribute(NameScope scope, TypeReference reference, IReadOnlyCollection<string> imports)
    {
        const string suffix = "Attribute";
        if (reference.Name.Length > suffix.Length && reference.Name.EndsWith(suffix, StringComparison.Ordinal))
        {
            var stem = reference.Name[..^suffix.Length];
            if (SyntaxFacts.GetKeywordKind(stem) == SyntaxKind.None && SyntaxFacts.GetContextualKeywordKind(stem) == SyntaxKind.None &&
                scope.Find(stem, reference.Arity, imports, reference) == NameLookup.Nothing && Binds(scope, reference.Name, reference, imports))
            {
                return stem;
            }
        }

        return scope.Find($"{reference.Name}{suffix}", reference.Arity, imports, reference) == NameLookup.Nothing && Binds(scope, reference.Name, reference, imports)
            ? Identifiers.EscapeKeyword(reference.Name)
            : reference.Qualified;
    }

    static bool Binds(NameScope scope, string name, TypeReference reference, IReadOnlyCollection<string> imports) =>
        scope.Find(name, reference.Arity, imports, reference) == NameLookup.Target;
}
