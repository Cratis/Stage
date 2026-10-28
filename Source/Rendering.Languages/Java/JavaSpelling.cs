// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// The spelling of every top-level type a Java file mentions, and the single-type imports that spelling needs.
/// </summary>
/// <param name="Heads">The spelling of each top-level type, short or package-qualified.</param>
/// <param name="Imports">The canonical names to import.</param>
sealed record JavaSpelling(ImmutableDictionary<TemplateSymbol, string> Heads, ImmutableArray<string> Imports)
{
    internal static JavaSpelling Compute(
        JavaFileAnalysis file,
        ImmutableHashSet<string> closure,
        ImmutableHashSet<string> introduced,
        ImmutableHashSet<string> packageTypes)
    {
        var heads = file.Symbols.Select(JavaFileAnalysis.Head).Distinct().ToArray();
        var samePackage = packageTypes.Union(heads.Where(head => head.Namespace == file.Package).Select(head => head.Name));
        var firstSegments = heads.Select(head => FirstSegment(head.Namespace)).ToImmutableHashSet(StringComparer.Ordinal);
        var shortHeads = ChooseShort(file, heads, closure, firstSegments);
        var claims = shortHeads.Select(head => head.Name).ToImmutableHashSet(StringComparer.Ordinal);
        RejectGuards(file, new(closure, introduced, samePackage, claims));
        var obscured = closure.Union(claims).Union(samePackage).Union(JavaLangTypes.Names);
        var spellings = ImmutableDictionary.CreateBuilder<TemplateSymbol, string>();
        foreach (var head in heads)
        {
            spellings[head] = shortHeads.Contains(head) ? head.Name : Qualified(head, obscured);
        }

        var imports = shortHeads.Where(head => head.Namespace != file.Package).Select(head => head.Namespace + "." + head.Name);
        return new(spellings.ToImmutable(), [.. imports]);
    }

    static ImmutableHashSet<TemplateSymbol> ChooseShort(JavaFileAnalysis file, TemplateSymbol[] heads, ImmutableHashSet<string> closure, ImmutableHashSet<string> firstSegments)
    {
        var result = ImmutableHashSet.CreateBuilder<TemplateSymbol>();
        foreach (var group in heads.GroupBy(head => head.Name, StringComparer.Ordinal))
        {
            var free = !closure.Contains(group.Key) && !firstSegments.Contains(group.Key);
            var local = group.FirstOrDefault(head => head.Namespace == file.Package);
            if (local is not null)
            {
                if (free)
                {
                    result.Add(local);
                }
            }
            else if (CanImport(file, group, free))
            {
                result.Add(group.First());
            }
        }

        return result.ToImmutable();
    }

    static bool CanImport(JavaFileAnalysis file, IEnumerable<TemplateSymbol> group, bool free) =>
        free && !group.Skip(1).Any() && !file.Words.All.Contains(group.First().Name);

    static void RejectGuards(JavaFileAnalysis file, Binders binders)
    {
        if (file.Words.Qualifiers.Contains("java") && binders.Captures("java"))
        {
            throw new LanguagePackRefusal(JavaRefusalCodes.LiteralShadowed, $"A binder named 'java' would capture the java-rooted names in '{file.File.Path}'.");
        }

        var shadowed = file.Words.All.FirstOrDefault(word => JavaLangTypes.Names.Contains(word) && binders.Shadows(word));
        if (shadowed is not null)
        {
            throw new LanguagePackRefusal(JavaRefusalCodes.LiteralShadowed, $"The literal java.lang name '{shadowed}' in '{file.File.Path}' is shadowed by a plan or inherited type.");
        }
    }

    static string Qualified(TemplateSymbol head, ImmutableHashSet<string> obscured)
    {
        var first = FirstSegment(head.Namespace);
        if (obscured.Contains(first))
        {
            throw new LanguagePackRefusal(JavaRefusalCodes.Unspellable, $"The qualified name '{head.Namespace}.{head.Name}' cannot be written because '{first}' can be obscured.");
        }

        return head.Namespace + "." + head.Name;
    }

    static string FirstSegment(string package) => package.Split('.')[0];

    sealed record Binders(ImmutableHashSet<string> Closure, ImmutableHashSet<string> Introduced, ImmutableHashSet<string> SamePackage, ImmutableHashSet<string> Claims)
    {
        internal bool Captures(string name) => Closure.Contains(name) || Claims.Contains(name) || SamePackage.Contains(name);

        internal bool Shadows(string name) => Introduced.Contains(name) || SamePackage.Contains(name);
    }
}
