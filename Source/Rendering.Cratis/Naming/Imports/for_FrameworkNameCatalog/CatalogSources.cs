// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog;

/// <summary>
/// Reads the names C# source files add to a scaffold project: package-contributed compile items such as the Cratis
/// package's global usings, and the output of the source generators that run in its build.
/// </summary>
static class CatalogSources
{
    /// <summary>
    /// Reads the global using directives of a source file.
    /// </summary>
    /// <param name="projects">The projects compiling the file.</param>
    /// <param name="source">Where the file comes from.</param>
    /// <param name="path">The file path.</param>
    /// <returns>The global using directives.</returns>
    public static IEnumerable<CatalogUsing> Usings(GeneratedProjects projects, string source, string path) =>
        Parse(path).GetCompilationUnitRoot().Usings
            .Where(@using => @using.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword))
            .Select(@using => new CatalogUsing(
                projects,
                Kind(@using),
                Unqualified(@using.NamespaceOrType.ToString()),
                @using.Alias?.Name.Identifier.ValueText ?? string.Empty,
                source));

    /// <summary>
    /// Reads the top-level types a source file declares that other files of the compilation can see.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The namespace, metadata name and kind of each type.</returns>
    public static IEnumerable<(string Namespace, string MetadataName, string Kind)> Types(string path) =>
        Parse(path).GetCompilationUnitRoot().DescendantNodes()
            .OfType<MemberDeclarationSyntax>()
            .Where(declaration => declaration is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax &&
                declaration.Parent is not TypeDeclarationSyntax &&
                !declaration.Modifiers.Any(SyntaxKind.FileKeyword))
            .Select(declaration => (Namespace(declaration), MetadataName(declaration), Kind(declaration)));

    static CatalogUsingKind Kind(UsingDirectiveSyntax @using) => @using switch
    {
        { StaticKeyword.RawKind: (int)SyntaxKind.StaticKeyword } => CatalogUsingKind.Static,
        { Alias: not null } => CatalogUsingKind.Alias,
        _ => CatalogUsingKind.Namespace
    };

    static SyntaxTree Parse(string path) => CSharpSyntaxTree.ParseText(File.ReadAllText(path), new CSharpParseOptions(preprocessorSymbols: ["DEBUG"]), path);

    static string Unqualified(string name) => name.StartsWith("global::", StringComparison.Ordinal) ? name["global::".Length..] : name;

    static string Namespace(SyntaxNode declaration) => string.Join('.', declaration.Ancestors()
        .OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(@namespace => @namespace.Name.ToString()));

    static string MetadataName(MemberDeclarationSyntax declaration) => declaration switch
    {
        TypeDeclarationSyntax type => CatalogNames.MetadataName(type.Identifier.ValueText, type.TypeParameterList?.Parameters.Count ?? 0),
        DelegateDeclarationSyntax @delegate => CatalogNames.MetadataName(@delegate.Identifier.ValueText, @delegate.TypeParameterList?.Parameters.Count ?? 0),
        BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
        _ => string.Empty
    };

    static string Kind(MemberDeclarationSyntax declaration) => declaration switch
    {
        InterfaceDeclarationSyntax => "interface",
        EnumDeclarationSyntax => "enum",
        DelegateDeclarationSyntax => "delegate",
        StructDeclarationSyntax or RecordDeclarationSyntax { ClassOrStructKeyword.RawKind: (int)SyntaxKind.StructKeyword } => "struct",
        ClassDeclarationSyntax type when type.Modifiers.Any(SyntaxKind.StaticKeyword) => "static",
        _ => "class"
    };
}
