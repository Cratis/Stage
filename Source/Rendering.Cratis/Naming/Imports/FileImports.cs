// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Decides, for one generated file, which namespaces it may import and how each type it names is spelled with a given set
/// of imports.
/// </summary>
/// <param name="scope">The names in scope in the file.</param>
/// <param name="catalog">The framework names in scope.</param>
/// <param name="request">The file being resolved.</param>
/// <param name="references">The distinct types the file names.</param>
internal sealed class FileImports(NameScope scope, FrameworkNameCatalog catalog, ImportRequest request, TypeReference[] references)
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

    static readonly ImmutableDictionary<string, string> _keywords = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Boolean"] = "bool",
        ["Byte"] = "byte",
        ["Char"] = "char",
        ["Decimal"] = "decimal",
        ["Double"] = "double",
        ["Int16"] = "short",
        ["Int32"] = "int",
        ["Int64"] = "long",
        ["Object"] = "object",
        ["SByte"] = "sbyte",
        ["Single"] = "float",
        ["String"] = "string",
        ["UInt16"] = "ushort",
        ["UInt32"] = "uint",
        ["UInt64"] = "ulong"
    }.ToImmutableDictionary(StringComparer.Ordinal);

    readonly NameScope _scope = scope;
    readonly TypeReference[] _references = references;
    readonly HashSet<string> _unspelled = request.PinnedNames.Concat(_contextualNames).ToHashSet(StringComparer.Ordinal);
    readonly HashSet<string> _accessed = request.MemberAccessNames.Concat(_patternMembers).ToHashSet(StringComparer.Ordinal);

    // Static and alias global usings bring names the catalog does not model, so the compilation unit is then unknown.
    readonly HashSet<string>? _global = catalog.HasUnmodeledUsings(request.Project) ? null : catalog.GlobalUsings(request.Project).ToHashSet(StringComparer.Ordinal);
    readonly HashSet<string> _enclosing = CatalogNames.SelfAndContaining(request.FileNamespace).ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Gets a value indicating whether a namespace the renderer asked to import is still needed. A directive that a global
    /// using repeats, or that brings nothing the file names, is dropped; one whose names are not all known is kept, since
    /// something in the file may rely on it.
    /// </summary>
    /// <param name="namespace">The namespace.</param>
    /// <returns><see langword="true"/> when the directive must stay.</returns>
    public bool NeedsRequired(string @namespace)
    {
        if (_global?.Contains(@namespace) == true) return false;
        if (!_scope.Knows(@namespace)) return true;
        return _scope.ExtensionNames(@namespace).Any(_accessed.Contains) || Brings(@namespace) ||
            _references.Any(reference => reference.Namespace == @namespace);
    }

    /// <summary>
    /// Gets a value indicating whether a namespace can be imported above the file-scoped namespace.
    /// </summary>
    /// <param name="namespace">The namespace.</param>
    /// <returns><see langword="true"/> when it can.</returns>
    public bool CanImportAtTop(string @namespace) =>
        _global?.Contains(@namespace) == false && !_enclosing.Contains(@namespace) && CanImport(@namespace);

    /// <summary>
    /// Gets a value indicating whether a namespace can be imported inside the file-scoped namespace.
    /// </summary>
    /// <param name="namespace">The namespace.</param>
    /// <param name="inner">Every namespace about to be imported inside the file-scoped namespace.</param>
    /// <param name="top">The imports above the namespace.</param>
    /// <returns><see langword="true"/> when it can.</returns>
    /// <remarks>
    /// A name the file writes outside the spelled references may be brought in when it already binds to that same type
    /// through the imports above the namespace and still does with the imports inside it: moving the import closer
    /// cannot change what it binds to. So may a name that is ambiguous among the compilation unit's imports and found
    /// nowhere closer: the file compiles, so every use of it binds to a member, parameter or local before the namespaces.
    /// </remarks>
    public bool CanImportInside(string @namespace, IReadOnlyCollection<string> inner, IReadOnlyCollection<string> top) =>
        !_scope.Encloses(@namespace) && CanImport(@namespace, name => KeepsBinding(name, @namespace, inner, top));

    /// <summary>
    /// Gets a value indicating whether the renderer declares a type in a namespace, which makes it win a name collision.
    /// </summary>
    /// <param name="namespace">The namespace.</param>
    /// <returns><see langword="true"/> when a generated type lives there.</returns>
    public bool IsGenerated(string @namespace) => _scope.DeclaresGeneratedTypes(@namespace);

    /// <summary>
    /// Gets a value indicating whether a namespace declares a type with a simple name.
    /// </summary>
    /// <param name="namespace">The namespace.</param>
    /// <param name="name">The simple name.</param>
    /// <returns><see langword="true"/> when it does.</returns>
    public bool Declares(string @namespace, string name) => _scope.TypeNames(@namespace).Any(candidate => candidate == name || candidate.StartsWith($"{name}`", StringComparison.Ordinal));

    /// <summary>
    /// Determines whether a directive inside the file-scoped namespace may name a namespace without <c language="csharp">global::</c>.
    /// </summary>
    /// <param name="namespace">The namespace.</param>
    /// <returns><see langword="true"/> when its first segment provably binds to the global namespace member.</returns>
    public bool BindsFromGlobal(string @namespace) => _scope.BindsFromGlobal(@namespace);

    /// <summary>
    /// Spells every reference against the final import sets and drops the imports no short spelling goes through, until
    /// nothing changes.
    /// </summary>
    /// <param name="inner">The candidate imports inside the namespace.</param>
    /// <param name="top">The candidate imports above the namespace.</param>
    /// <param name="kept">The requested imports that stay whether or not a reference uses them.</param>
    /// <param name="settledInner">The imports inside the namespace that remain.</param>
    /// <returns>The imports above the namespace that remain.</returns>
    public List<string> Settle(IReadOnlyList<string> inner, IReadOnlyList<string> top, IReadOnlyList<string> kept, out List<string> settledInner)
    {
        var currentInner = inner.Distinct(StringComparer.Ordinal).ToList();
        var currentTop = top.Distinct(StringComparer.Ordinal).ToList();
        while (true)
        {
            var spellings = Spell(currentInner, currentTop);
            bool Used(string @namespace) => _references.Any(reference => reference.Namespace == @namespace && spellings[reference] != reference.Qualified);
            var usedInner = currentInner.Where(Used).ToList();
            var usedTop = currentTop.Where(@namespace => !usedInner.Contains(@namespace, StringComparer.Ordinal) && (kept.Contains(@namespace, StringComparer.Ordinal) || Used(@namespace))).ToList();
            if (usedInner.Count == currentInner.Count && usedTop.Count == currentTop.Count)
            {
                settledInner = currentInner;
                return currentTop;
            }

            (currentInner, currentTop) = (usedInner, usedTop);
        }
    }

    /// <summary>
    /// Spells every reference with a given set of imports.
    /// </summary>
    /// <param name="inner">The imports inside the namespace.</param>
    /// <param name="top">The imports above the namespace.</param>
    /// <returns>The spelling of each reference.</returns>
    public ImmutableDictionary<TypeReference, string> Spell(IReadOnlyCollection<string> inner, IReadOnlyCollection<string> top)
    {
        IReadOnlyCollection<string>? compilationUnit = _global is null ? null : [.. _global.Concat(top).Distinct(StringComparer.Ordinal)];
        return _references.ToImmutableDictionary(reference => reference, reference => Spell(reference, inner, compilationUnit));
    }

    bool Brings(string @namespace) => Brings(@namespace, _ => false);

    bool Brings(string @namespace, Func<string, bool> tolerated) => _scope.TypeNames(@namespace).Any(name => (_unspelled.Contains(name) && !tolerated(name)) ||
        (name.EndsWith("Attribute", StringComparison.Ordinal) && _unspelled.Contains(name[..^"Attribute".Length])));

    bool KeepsBinding(string name, string @namespace, IReadOnlyCollection<string> inner, IReadOnlyCollection<string> top)
    {
        if (_global is null || _scope.TypeNames(@namespace).Count(candidate => candidate == name) != 1) return false;
        var target = new TypeReference(@namespace, name, 0, ReferenceContext.Type);
        IReadOnlyCollection<string> compilationUnit = [.. _global.Concat(top).Distinct(StringComparer.Ordinal)];
        return (_scope.Find(name, 0, [], compilationUnit, target) == NameLookup.Target || _scope.IsAmbiguousOnlyInCompilationUnit(name, compilationUnit)) &&
            _scope.Find(name, 0, inner, compilationUnit, target) == NameLookup.Target;
    }

    bool CanImport(string @namespace) => CanImport(@namespace, _ => false);

    bool CanImport(string @namespace, Func<string, bool> tolerated)
    {
        if (@namespace.Length == 0 || !_scope.Knows(@namespace)) return false;

        // A directive's first segment binds in the global namespace, where a type of that name would hide it.
        if (_scope.HidesImport(@namespace.Split('.')[0])) return false;
        if (_scope.ExtensionNames(@namespace).Any(_accessed.Contains)) return false;
        return !Brings(@namespace, tolerated);
    }

    string Spell(TypeReference reference, IReadOnlyCollection<string> inner, IReadOnlyCollection<string>? compilationUnit)
    {
        // A predefined type keyword always binds to its System type, whatever else is in scope; nameof does not accept one.
        if (string.Equals(reference.Namespace, "System", StringComparison.Ordinal) && reference.Arity == 0 &&
            reference.Context is ReferenceContext.Type or ReferenceContext.Expression && _keywords.TryGetValue(reference.Name, out var keyword))
        {
            return keyword;
        }

        if (SyntaxFacts.GetContextualKeywordKind(reference.Name) != SyntaxKind.None) return reference.Qualified;
        return reference.Context switch
        {
            ReferenceContext.Attribute => Attribute(reference, inner, compilationUnit),
            ReferenceContext.NameOf when _scope.ValueNames.Contains("nameof") => reference.Qualified,
            ReferenceContext.Expression or ReferenceContext.NameOf when _scope.ValueNames.Contains(reference.Name) => reference.Qualified,
            _ => Binds(reference.Name, reference, inner, compilationUnit) ? Identifiers.EscapeKeyword(reference.Name) : reference.Qualified
        };
    }

    // [Key] is looked up both as Key and as KeyAttribute; [KeyAttribute] both as KeyAttribute and as KeyAttributeAttribute.
    // A spelling is used only when its other candidate binds to nothing at all, or, for the short spelling, only to
    // framework types that are not attributes, which C# then passes over (an EventType record beside EventTypeAttribute).
    string Attribute(TypeReference reference, IReadOnlyCollection<string> inner, IReadOnlyCollection<string>? compilationUnit)
    {
        const string suffix = "Attribute";
        if (reference.Name.Length > suffix.Length && reference.Name.EndsWith(suffix, StringComparison.Ordinal))
        {
            var stem = reference.Name[..^suffix.Length];
            if (SyntaxFacts.GetKeywordKind(stem) == SyntaxKind.None && SyntaxFacts.GetContextualKeywordKind(stem) == SyntaxKind.None &&
                (_scope.Find(stem, reference.Arity, inner, compilationUnit, reference) == NameLookup.Nothing || _scope.BindsOnlyNonAttributes(stem, reference.Arity, inner, compilationUnit)) &&
                Binds(reference.Name, reference, inner, compilationUnit))
            {
                return stem;
            }
        }

        return _scope.Find($"{reference.Name}{suffix}", reference.Arity, inner, compilationUnit, reference) == NameLookup.Nothing && Binds(reference.Name, reference, inner, compilationUnit)
            ? Identifiers.EscapeKeyword(reference.Name)
            : reference.Qualified;
    }

    bool Binds(string name, TypeReference reference, IReadOnlyCollection<string> inner, IReadOnlyCollection<string>? compilationUnit) =>
        _scope.Find(name, reference.Arity, inner, compilationUnit, reference) == NameLookup.Target;
}
