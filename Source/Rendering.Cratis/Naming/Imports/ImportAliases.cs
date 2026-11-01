// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>Disambiguates catalog-proved identities without importing additional ordinary names.</summary>
internal static class ImportAliases
{
    internal static void Verify(ImportRequest request, FrameworkNameCatalog catalog)
    {
        if (request.HasUnknownScope) throw new CSharpImportRejected("The generated file has an unknown C# scope.");
        foreach (var reference in request.References) VerifyReference(request, catalog, reference);
    }

    internal static (ImmutableDictionary<TypeReference, string> Spellings, ImmutableArray<string> Directives) Resolve(
        ImportRequest request, ImmutableDictionary<TypeReference, string> spellings, ScaffoldBindings scaffold)
    {
        var reserved = request.ScopeTypeNames.Concat(request.ScopeValueNames).Concat(request.PinnedNames)
            .Concat(request.GeneratedTypes.Select(type => type.Name)).Concat(request.FileNamespace.Split('.'))
            .Concat(request.GeneratedTypes.SelectMany(type => type.Namespace.Split('.'))).ToHashSet(StringComparer.Ordinal);
        var result = spellings.ToBuilder();
        var directives = ImmutableArray.CreateBuilder<string>();
        var index = 0;
        foreach (var group in spellings.Where(entry => entry.Value == entry.Key.Qualified && scaffold.NeedsAlias(entry.Key)).GroupBy(entry => entry.Key.Namespace).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            if (group.Key.Length == 0) throw new CSharpImportRejected("A colliding global-namespace type has no provable namespace alias.");
            string alias;
            do
            {
                alias = "StageImport" + index.ToString(CultureInfo.InvariantCulture);
                index++;
            }
            while (!reserved.Add(alias));
            var target = string.Join('.', group.Key.Split('.').Select(Identifiers.EscapeKeyword));
            directives.Add($"{alias} = global::{target}");
            foreach (var entry in group) result[entry.Key] = $"{alias}::{Identifiers.EscapeKeyword(entry.Key.Name)}";
        }
        return (result.ToImmutable(), directives.ToImmutable());
    }

    static void VerifyReference(ImportRequest request, FrameworkNameCatalog catalog, TypeReference reference)
    {
        var generated = request.GeneratedTypes.Any(type => type.Namespace == reference.Namespace && type.Name == reference.Name && type.Arity == reference.Arity);
        if (!generated && !catalog.TypesNamed(reference.Namespace, reference.Name, reference.Arity, request.Project).Any())
            throw new CSharpImportRejected($"The generated type '{reference.Qualified}' is not cataloged.");
        if (reference.Context == ReferenceContext.NameOf && request.ScopeValueNames.Contains("nameof", StringComparer.Ordinal))
            throw new CSharpImportRejected("A member named 'nameof' prevents proving the generated nameof binding.");
    }
}
