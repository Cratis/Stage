// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Allocates one Java type namespace symmetrically, including a collision pass over generated suffixes.
/// Case folding protects source and class files on case-insensitive file systems.
/// </summary>
public static class JavaDeclarationNames
{
    /// <summary>
    /// Allocates the declarations in one package and enclosing type scope.
    /// </summary>
    /// <param name="declarations">The declarations sharing a namespace.</param>
    /// <returns>The allocated declarations, preserving external names.</returns>
    /// <exception cref="LanguagePackRefusal">A unique identifier cannot fit the java@1 identifier policy.</exception>
    public static IEnumerable<DeclaredType> Allocate(IEnumerable<DeclaredType> declarations)
    {
        var members = declarations.ToArray();
        var enclosing = members[0].AllocatedScope.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var duplicates = members.GroupBy(member => member.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var reserved = members.Select(member => member.Name).Concat(enclosing).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var renamed = members.Where(member => !member.External && (duplicates.Contains(member.Name) || enclosing.Contains(member.Name, StringComparer.OrdinalIgnoreCase)))
            .ToDictionary(member => member, member => Rename(member, 0));
        var attempts = 0;
        while (renamed.Count > 0)
        {
            var collisions = renamed.Values.GroupBy(member => member.Name, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var retry = renamed.Where(pair => reserved.Contains(pair.Value.Name) || collisions.Contains(pair.Value.Name)).Select(pair => pair.Key).ToArray();
            if (retry.Length == 0) break;
            attempts++;
            foreach (var member in retry) renamed[member] = Rename(member, attempts);
        }

        return [.. members.Select(member => renamed.GetValueOrDefault(member, member))];
    }

    static DeclaredType Rename(DeclaredType declaration, int attempt)
    {
        var identity = declaration.Owner + "\n" + declaration.File;
        var suffix = Hash(identity);
        if (attempt > 0)
        {
            // Include the full source identity when even owner/file suffixes collide. Each retry grows,
            // so adversarial input eventually refuses at the identifier limit rather than looping.
            suffix += "_" + Hash(identity + "\n" + declaration.Scope + "\n" + declaration.Preferred) + new string('_', attempt);
        }

        var name = declaration.Name + "_" + suffix;
        if (!JavaIdentifiers.IsIdentifier(name))
        {
            throw new LanguagePackRefusal(JavaRefusalCodes.InvalidIdentifier, $"Cannot allocate a unique Java identifier for '{declaration.Preferred}' (attempt {attempt.ToString(CultureInfo.InvariantCulture)}).");
        }

        return declaration with { Name = name };
    }

    static string Hash(string identity) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity)).AsSpan(0, 4));
}
