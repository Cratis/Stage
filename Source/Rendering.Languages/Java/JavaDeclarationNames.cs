// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Allocates nested names per original sibling scope, with ancestors allocated before descendants and no visit-order winners.
/// </summary>
static class JavaDeclarationNames
{
    internal static ImmutableDictionary<TemplateSymbol, JavaDeclaration> Allocate(
        ImmutableDictionary<TemplateSymbol, JavaDeclaration> declarations,
        ImmutableDictionary<string, ImmutableHashSet<string>> words)
    {
        var allocated = declarations.Where(entry => !entry.Key.Name.Contains('.', StringComparison.Ordinal)).ToImmutableDictionary().ToBuilder();
        var scopes = declarations.Values.Where(entry => entry.Symbol.Name.Contains('.', StringComparison.Ordinal))
            .GroupBy(entry => Parent(entry.Symbol))
            .OrderBy(scope => scope.Key.Name.Count(character => character == '.'))
            .ThenBy(scope => scope.Key.Namespace, StringComparer.Ordinal)
            .ThenBy(scope => scope.Key.Name, StringComparer.Ordinal);
        foreach (var scope in scopes)
        {
            var parent = allocated[scope.Key].Allocated;
            var siblings = scope.OrderBy(entry => entry.Symbol.Name, StringComparer.Ordinal).ToArray();
            var names = AllocateScope(siblings, parent, words[siblings[0].Path]);
            foreach (var entry in siblings)
            {
                allocated.Add(entry.Symbol, entry with { Allocated = new(parent.Namespace, parent.Name + "." + names[entry.Symbol]) });
            }
        }

        return allocated.ToImmutable();
    }

    static ImmutableDictionary<TemplateSymbol, string> AllocateScope(JavaDeclaration[] siblings, TemplateSymbol parent, ImmutableHashSet<string> words)
    {
        var enclosers = parent.Name.Split('.').Concat(Parent(siblings[0].Symbol).Name.Split('.')).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        var collisions = siblings.GroupBy(entry => Leaf(entry.Symbol), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1 || enclosers.Contains(group.Key))
            .SelectMany(group => group).ToArray();
        var names = siblings.ToImmutableDictionary(entry => entry.Symbol, entry => Leaf(entry.Symbol)).ToBuilder();
        var reserved = names.Values.Concat(enclosers).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (symbol, name) in AllocateSuffixes(collisions, reserved, words))
        {
            names[symbol] = name;
        }

        return names.ToImmutable();
    }

    static ImmutableDictionary<TemplateSymbol, string> AllocateSuffixes(JavaDeclaration[] collisions, ImmutableHashSet<string> reserved, ImmutableHashSet<string> words)
    {
        var lengths = collisions.ToDictionary(entry => entry.Symbol, _ => 8);
        while (true)
        {
            var candidates = lengths.ToImmutableDictionary(entry => entry.Key, entry => Suffixed(entry.Key, entry.Value));
            var duplicates = candidates.Values.GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1).Select(group => group.Key).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
            var contested = candidates.Where(entry => reserved.Contains(entry.Value) || words.Contains(entry.Value) || duplicates.Contains(entry.Value)).ToArray();
            if (contested.Length == 0)
            {
                return candidates;
            }

            foreach (var entry in contested)
            {
                lengths[entry.Key] += 4;
            }
        }
    }

    static string Suffixed(TemplateSymbol symbol, int digits)
    {
        var leaf = Leaf(symbol);
        if (leaf.Length + 1 + digits > 255)
        {
            throw new LanguagePackRefusal(JavaRefusalCodes.InvalidIdentifier, $"The allocated Java name for '{symbol.Namespace}.{symbol.Name}' exceeds 255 bytes.");
        }

        var identity = symbol.Namespace + "\n" + symbol.Name;
        var hash = Hash(identity);
        while (hash.Length < digits)
        {
            hash += Hash(identity + "\n" + hash.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return leaf + "_" + hash[..digits];
    }

    static string Hash(string identity) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));

    static string Leaf(TemplateSymbol symbol) => symbol.Name[(symbol.Name.LastIndexOf('.') + 1)..];

    static TemplateSymbol Parent(TemplateSymbol symbol) => symbol with { Name = symbol.Name[..symbol.Name.LastIndexOf('.')] };
}
