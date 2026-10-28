// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// The text and structure facts of one Java file that binding decisions need.
/// </summary>
/// <param name="File">The file.</param>
/// <param name="Package">The package of the file's top-level type.</param>
/// <param name="Words">The words of the assembled literal and authored text, in which each structured segment is one space.</param>
/// <param name="NestedDeclarations">The allocated leaf names of the nested declarations.</param>
sealed record JavaFileAnalysis(
    DeclarativeLanguageFile File,
    string Package,
    JavaWordSet Words,
    ImmutableHashSet<string> NestedDeclarations)
{
    /// <summary>
    /// Gets the distinct structured references.
    /// </summary>
    internal ImmutableArray<TemplateSymbol> References => [.. File.Segments.OfType<DeclarativeSymbolSegment>().Where(segment => segment.Kind == TemplateSymbolKind.Reference).Select(segment => segment.Value!).Distinct()];

    /// <summary>
    /// Gets the distinct structured references and declarations, in file order.
    /// </summary>
    internal ImmutableArray<TemplateSymbol> Symbols => [.. File.Segments.OfType<DeclarativeSymbolSegment>().Where(segment => segment.Kind != TemplateSymbolKind.Imports).Select(segment => segment.Value!).Distinct()];

    internal static JavaFileAnalysis Create(DeclarativeLanguageFile file, JavaPlan plan)
    {
        var symbols = file.Segments.OfType<DeclarativeSymbolSegment>().Where(segment => segment.Kind != TemplateSymbolKind.Imports).ToArray();
        var declared = symbols.Where(segment => segment.Kind == TemplateSymbolKind.Declaration).Select(segment => segment.Value!).Distinct().ToArray();
        var package = declared[0].Namespace;
        if (declared.Any(symbol => symbol.Namespace != package))
        {
            throw new LanguagePackRefusal(JavaRefusalCodes.FileStructure, $"Invalid Java file '{file.Path}': every declaration must be in package '{package}'.");
        }

        var nested = declared.Where(symbol => symbol.Name.Contains('.', StringComparison.Ordinal))
            .Select(symbol => Leaf(plan.Declarations[symbol].Allocated.Name))
            .ToImmutableHashSet(StringComparer.Ordinal);
        return new(
            file,
            package,
            JavaWords.Scan(JavaFileStructure.RawText(file.Segments)),
            nested);
    }

    internal static string Leaf(string name) => name[(name.LastIndexOf('.') + 1)..];

    internal static TemplateSymbol Head(TemplateSymbol symbol)
    {
        var dot = symbol.Name.IndexOf('.', StringComparison.Ordinal);
        return dot < 0 ? symbol : symbol with { Name = symbol.Name[..dot] };
    }
}
