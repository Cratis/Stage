// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Renderers.for_LegacyGeneratedTypes;

internal static class InventoryAssertions
{
    internal static IReadOnlyList<string> Missing(IEnumerable<RenderedFile> files, ApplicationSet application, string root)
    {
        var inventory = LegacyGeneratedTypes.For(application, root).ToHashSet();

        return [.. files.Where(file => file.RelativePath.EndsWith(".cs", StringComparison.Ordinal))
            .SelectMany(file => CSharpSyntaxTree.ParseText(file.Content).GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            .Where(declaration => declaration.Parent is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
            .Select(declaration => new GeneratedTypeName(
                string.Join('.', declaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(@namespace => @namespace.Name.ToString())),
                declaration.Identifier.ValueText,
                declaration is TypeDeclarationSyntax type ? type.TypeParameterList?.Parameters.Count ?? 0 : 0))
            .Where(declaration => !inventory.Contains(declaration))
            .Select(declaration => $"{declaration.Namespace}.{declaration.Name}`{declaration.Arity}")];
    }
}
