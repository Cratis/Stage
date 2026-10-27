// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Proves absence of inherited member-type shadowing through superclass and interface edges.
/// Missing or ambiguous supertypes, cycles, and unknown external member sets prevent shortening;
/// callers can still use qualified names. Only known member names obscure a qualified package prefix.
/// </summary>
public sealed class JavaInheritance
{
    readonly DeclaredType[] _types;
    readonly Dictionary<(string File, string Owner), DeclaredType[]> _owners;
    readonly Dictionary<DeclaredType, (bool Known, DeclaredType[] Types)> _edges;
    readonly Dictionary<string, SymbolFile[]> _files;
    readonly ConcurrentDictionary<DeclaredType, (bool Known, HashSet<string> Names)> _inherited = new();
    readonly ConcurrentDictionary<string, IReadOnlySet<string>> _literalNames = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="JavaInheritance"/> class.
    /// </summary>
    /// <param name="types">The allocated declarations, including external member metadata.</param>
    /// <param name="files">The generated files, whose literal identifiers are conservatively reserved in superclasses.</param>
    public JavaInheritance(IEnumerable<DeclaredType> types, IEnumerable<SymbolFile> files)
    {
        _types = [.. types];
        _owners = _types.Where(type => !type.External).GroupBy(type => (type.File, type.Owner)).ToDictionary(group => group.Key, group => group.ToArray());
        _files = files.GroupBy(file => file.Id, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var preferred = _types.GroupBy(type => (type.Package, type.Scope, type.Preferred)).ToDictionary(group => group.Key, group => group.ToArray());
        _edges = _types.ToDictionary(type => type, type => ResolveEdges(type, preferred));
    }

    /// <summary>
    /// Determines whether a simple type name lacks a proven binding in an owner's lexical chain.
    /// </summary>
    /// <param name="file">The stable file identity.</param>
    /// <param name="owner">The lexical declaration identity, or empty to check every declaration in the file.</param>
    /// <param name="target">The allocated target type.</param>
    /// <returns>True for a conflicting member type or incomplete inheritance evidence.</returns>
    public bool BlocksShortening(string file, string owner, TypeName target)
    {
        var scope = LexicalOwners(file, owner);

        return !scope.Known || scope.Types.Any(type => OwnMemberShadows(type, target) || Inherited(type).Names.Contains(target.SimpleName) || !Inherited(type).Known);
    }

    /// <summary>
    /// Determines whether a known member type obscures the first segment of a qualified spelling.
    /// </summary>
    /// <param name="file">The stable file identity.</param>
    /// <param name="owner">The lexical declaration identity, or empty to check every declaration in the file.</param>
    /// <param name="name">The first segment of the qualification.</param>
    /// <returns>Whether a known lexical or inherited member uses that name.</returns>
    public bool ObscuresName(string file, string owner, string name) => LexicalOwners(file, owner).Types.Any(type =>
        DirectMembers(type).Contains(name, StringComparer.Ordinal) || Inherited(type).Names.Contains(name));

    static bool HasKnownMembers(DeclaredType type) => !type.External || type.DeclaredMemberTypes.IsKnown;

    static (bool Known, DeclaredType[] Types) ResolveEdges(DeclaredType type, Dictionary<(string Package, string Scope, string Name), DeclaredType[]> preferred)
    {
        if (!type.Supertypes.IsKnown) return (false, []);
        var matches = type.Supertypes.Values.Select(supertype => preferred.GetValueOrDefault((supertype.Package, supertype.EnclosingScope, supertype.SimpleName), [])).ToArray();

        return (matches.All(candidates => candidates.Length == 1), [.. matches.Where(candidates => candidates.Length == 1).Select(candidates => candidates[0])]);
    }

    (bool Known, DeclaredType[] Types) LexicalOwners(string file, string owner)
    {
        if (owner.Length == 0)
        {
            var declarations = _types.Where(type => !type.External && type.File == file).ToArray();

            return (declarations.Length > 0, declarations);
        }

        var result = new HashSet<DeclaredType>();
        var candidates = _owners.GetValueOrDefault((file, owner), []);
        while (candidates.Length == 1 && result.Add(candidates[0]))
        {
            var type = candidates[0];
            if (type.ParentOwner.Length == 0) return (true, [.. result]);
            candidates = _owners.GetValueOrDefault((file, type.ParentOwner), []);
        }

        return (false, [.. result]);
    }

    bool OwnMemberShadows(DeclaredType owner, TypeName target) => _types.Any(member => !member.External && member.File == owner.File &&
        member.ParentOwner == owner.Owner && member.Name == target.SimpleName &&
        (member.Package != target.Package || member.Final.EnclosingScope != target.EnclosingScope));

    IEnumerable<string> DirectMembers(DeclaredType type)
    {
        if (type.External) return type.DeclaredMemberTypes.IsKnown ? type.DeclaredMemberTypes.Values : [];

        return _types.Where(member => !member.External && member.File == type.File && member.ParentOwner == type.Owner).Select(member => member.Name);
    }

    (bool Known, HashSet<string> Names) Inherited(DeclaredType type) => _inherited.GetOrAdd(type, CollectInherited);

    (bool Known, HashSet<string> Names) CollectInherited(DeclaredType type)
    {
        var known = _edges[type].Known;
        var names = new HashSet<string>(StringComparer.Ordinal);
        var reachable = new HashSet<DeclaredType>();
        var pending = new Queue<DeclaredType>(_edges[type].Types);
        while (pending.TryDequeue(out var current))
        {
            if (!reachable.Add(current)) continue;
            known &= _edges[current].Known && HasKnownMembers(current);
            names.UnionWith(DirectMembers(current));
            if (!current.External) names.UnionWith(LiteralNames(current.File));
            foreach (var supertype in _edges[current].Types) pending.Enqueue(supertype);
        }

        return (known && IsAcyclic(reachable), names);
    }

    IReadOnlySet<string> LiteralNames(string file) => _literalNames.GetOrAdd(
        file,
        static (id, files) => files.GetValueOrDefault(id, []).SelectMany(InlineBodyScanner.Identifiers).ToHashSet(StringComparer.Ordinal),
        _files);

    Dictionary<DeclaredType, int> Indegrees(HashSet<DeclaredType> reachable)
    {
        var indegrees = reachable.ToDictionary(type => type, _ => 0);
        foreach (var type in reachable)
        {
            foreach (var supertype in _edges[type].Types) indegrees[supertype]++;
        }

        return indegrees;
    }

    bool IsAcyclic(HashSet<DeclaredType> reachable)
    {
        var indegrees = Indegrees(reachable);
        var pending = new Queue<DeclaredType>(indegrees.Where(pair => pair.Value == 0).Select(pair => pair.Key));
        var removed = 0;
        while (pending.TryDequeue(out var type))
        {
            removed++;
            foreach (var supertype in _edges[type].Types)
            {
                if (--indegrees[supertype] == 0) pending.Enqueue(supertype);
            }
        }

        return removed == reachable.Count;
    }
}
