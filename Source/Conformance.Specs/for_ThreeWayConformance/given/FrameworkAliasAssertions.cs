// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

static class FrameworkAliasAssertions
{
    internal static void Verify(RenderedFile original, IReadOnlyList<RenderedFile> rewritten, string qualified)
    {
        var before = QualifiedOutputProof.Compile([original], legacyImports: true).Application;
        var beforeTree = before.SyntaxTrees.Single(tree => tree.FilePath == original.RelativePath);
        var site = Assert.Single(beforeTree.GetRoot().DescendantNodes(), node =>
            node is ParameterSyntax or InvocationExpressionSyntax && node.ToString() == qualified);
        var expected = IntendedSymbol(before.GetSemanticModel(beforeTree), site);
        var type = expected as INamedTypeSymbol ?? expected.ContainingType;
        Assert.NotNull(type);

        var after = QualifiedOutputProof.Compile(rewritten).Application;
        var afterTree = after.SyntaxTrees.Single(tree => tree.FilePath == original.RelativePath);
        var root = (CompilationUnitSyntax)afterTree.GetRoot();
        var directive = Assert.Single(root.Usings, @using => @using.Alias is not null &&
            @using.NamespaceOrType.ToString() == $"global::{type.ContainingNamespace.ToDisplayString()}");
        var alias = directive.Alias!.Name.Identifier.ValueText;
        Assert.Matches("^StageImport[0-9]+$", alias);
        var reference = Assert.Single(root.DescendantNodes().OfType<AliasQualifiedNameSyntax>(), name =>
            name.Alias.Identifier.ValueText == alias && name.Name.Identifier.ValueText == type.Name);
        var model = after.GetSemanticModel(afterTree);
        Assert.True(SymbolEqualityComparer.Default.Equals(type, model.GetSymbolInfo(reference).Symbol));
        if (expected is IMethodSymbol) AssertMethod(model, reference, expected);
    }

    static ISymbol IntendedSymbol(SemanticModel model, SyntaxNode site) => site switch
    {
        ParameterSyntax parameter => Assert.IsType<INamedTypeSymbol>(model.GetTypeInfo(parameter.Type!).Type, exactMatch: false),
        InvocationExpressionSyntax invocation => Assert.IsType<IMethodSymbol>(model.GetSymbolInfo(invocation).Symbol, exactMatch: false),
        _ => throw new InvalidOperationException("The framework ambiguity must name a parameter type or invocation.")
    };

    static void AssertMethod(SemanticModel model, AliasQualifiedNameSyntax reference, ISymbol expected)
    {
        var access = Assert.IsType<MemberAccessExpressionSyntax>(reference.Parent);
        var invocation = Assert.IsType<InvocationExpressionSyntax>(access.Parent);
        Assert.True(SymbolEqualityComparer.Default.Equals(expected, model.GetSymbolInfo(invocation).Symbol));
    }
}
#endif
