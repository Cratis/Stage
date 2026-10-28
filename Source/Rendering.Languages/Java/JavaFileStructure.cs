// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Validates structured Java handles without parsing authored Java or mistaking constructor repetition for multiple declarations.
/// </summary>
static class JavaFileStructure
{
    internal static ImmutableHashSet<TemplateSymbol> Declarations(DeclarativeLanguageFile file)
    {
        var symbols = file.Segments.OfType<DeclarativeSymbolSegment>().ToArray();
        foreach (var symbol in symbols.Where(symbol => symbol.Kind != TemplateSymbolKind.Imports))
        {
            ValidateSymbol(symbol.Value!);
        }

        if (!file.Path.EndsWith(".java", StringComparison.Ordinal))
        {
            if (symbols.Any(symbol => symbol.Kind != TemplateSymbolKind.Reference))
            {
                throw new LanguagePackRefusal(JavaRefusalCodes.NotJavaSource, $"A declaration or imports block appears in non-Java file '{file.Path}'.");
            }

            return [];
        }

        var declarations = symbols.Where(symbol => symbol.Kind == TemplateSymbolKind.Declaration).ToArray();
        ValidateHeader(file, declarations);
        var identities = declarations.Select(symbol => symbol.Value!).ToImmutableHashSet();
        ValidatePrefixes(file.Path, identities);
        return identities;
    }

    internal static string RawText(IEnumerable<DeclarativeSegment> segments) => string.Concat(segments.Select(segment => segment switch
    {
        DeclarativeTextSegment text => text.Value,
        DeclarativeAuthoredSegment authored => authored.Body,
        _ => " "
    }));

    static void ValidateSymbol(TemplateSymbol symbol)
    {
        if (symbol.Namespace.Length == 0)
        {
            throw new LanguagePackRefusal(JavaRefusalCodes.DefaultPackage, "A Java symbol must have a named package.");
        }

        if (!symbol.Namespace.Split('.').All(JavaIdentifiers.IsSymbolSegment) || !symbol.Name.Split('.').All(JavaIdentifiers.IsSymbolSegment))
        {
            throw new LanguagePackRefusal(JavaRefusalCodes.InvalidIdentifier, $"Invalid structured Java symbol '{symbol.Namespace}.{symbol.Name}'.");
        }
    }

    static void ValidateHeader(DeclarativeLanguageFile file, DeclarativeSymbolSegment[] declarations)
    {
        var first = declarations.FirstOrDefault();
        if (!IsFileType(first?.Value, file.Path))
        {
            Refuse(file.Path, "The first declaration must be the file's top-level type.");
        }

        var prefix = file.Segments.TakeWhile(segment => segment != first).ToArray();
        var imports = file.Segments.OfType<DeclarativeSymbolSegment>().Count(symbol => symbol.Kind == TemplateSymbolKind.Imports);
        if (imports != 1 || !prefix.OfType<DeclarativeSymbolSegment>().Any(symbol => symbol.Kind == TemplateSymbolKind.Imports))
        {
            Refuse(file.Path, "Exactly one imports block must precede the first declaration.");
        }

        var words = JavaWords.Scan(RawText(prefix)).All;
        if (words.Contains("package") || words.Contains("import"))
        {
            Refuse(file.Path, "Literal text before the first declaration contains a package or import word.");
        }
    }

    static bool IsFileType(TemplateSymbol? symbol, string path)
    {
        if (symbol is null)
        {
            return false;
        }

        return !symbol.Name.Contains('.', StringComparison.Ordinal) && symbol.Name == Path.GetFileNameWithoutExtension(path);
    }

    static void ValidatePrefixes(string path, ImmutableHashSet<TemplateSymbol> declarations)
    {
        if (declarations.Count(symbol => !symbol.Name.Contains('.', StringComparison.Ordinal)) != 1)
        {
            Refuse(path, "Exactly one distinct top-level symbol must be declared.");
        }

        foreach (var symbol in declarations)
        {
            var dot = symbol.Name.LastIndexOf('.');
            if (dot >= 0 && !declarations.Contains(symbol with { Name = symbol.Name[..dot] }))
            {
                Refuse(path, $"The prefix of nested declaration '{symbol.Name}' is not declared in this file.");
            }
        }
    }

    static void Refuse(string path, string reason) => throw new LanguagePackRefusal(JavaRefusalCodes.FileStructure, $"Invalid Java file '{path}': {reason}");
}
