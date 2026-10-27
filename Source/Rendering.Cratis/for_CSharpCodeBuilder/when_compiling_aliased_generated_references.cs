// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CSharpCodeBuilder;

public class when_compiling_aliased_generated_references
{
    [Fact]
    void should_bind_generic_nested_keyword_and_shadowed_names_without_changing_authored_bytes()
    {
        const string body = "return typeof(global::Model.Types.@class).FullName!;";
        GeneratedTypeName[] types = [new("Model.Types", "Outer", 1), new("Model.Other", "Outer", 1), new("Model.Types", "class")];
        var source = new CSharpCodeBuilder(GeneratedProjects.Application, types, hasScaffoldGlobalUsings: false).Namespace("Example")
            .OpenBlock("public class Host<Outer, Dictionary>")
            .Line("public global::Model.Types.Outer<int> First = null!;")
            .Line("public global::Model.Other.Outer<int> Second = null!;")
            .Line("public global::Model.Types.@class Third = null!;")
            .Line("public global::System.Collections.Generic.Dictionary<int, string>.Enumerator Nested;")
            .OpenBlock("public string Run()").RawVerbatim(body).EndBlock().EndBlock().ToString();
        var tree = CSharpSyntaxTree.ParseText(source);
        var declarations = CSharpSyntaxTree.ParseText("namespace Model.Types { public class Outer<T> {} public class @class {} } namespace Model.Other { public class Outer<T> {} }");
        var compilation = CSharpCompilation.Create("AliasBinding", [tree, declarations], PureTransitionAdmission.AnalysisReferences(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        Assert.Empty(compilation.GetDiagnostics().Where(SemanticBodyCompilation.IsFailure));
        var model = compilation.GetSemanticModel(tree);
        var fields = tree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>().Select(field => (IFieldSymbol)model.GetDeclaredSymbol(field.Declaration.Variables.Single())!).ToArray();
        Assert.True(SymbolEqualityComparer.Default.Equals(fields[0].Type.OriginalDefinition, compilation.GetTypeByMetadataName("Model.Types.Outer`1")));
        Assert.Equal(SpecialType.System_Int32, ((INamedTypeSymbol)fields[0].Type).TypeArguments.Single().SpecialType);
        Assert.True(SymbolEqualityComparer.Default.Equals(fields[3].Type.OriginalDefinition, compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2+Enumerator")));
        SpecialType[] expectedArguments = [SpecialType.System_Int32, SpecialType.System_String];
        Assert.Equal(expectedArguments, ((INamedTypeSymbol)fields[3].Type).ContainingType.TypeArguments.Select(type => type.SpecialType));
        Assert.True(SymbolEqualityComparer.Default.Equals(fields[1].Type.OriginalDefinition, compilation.GetTypeByMetadataName("Model.Other.Outer`1")));
        Assert.True(SymbolEqualityComparer.Default.Equals(fields[2].Type, compilation.GetTypeByMetadataName("Model.Types.class")));
        Assert.Contains(body, source);
        Assert.All(tree.GetRoot().DescendantNodes().OfType<AliasQualifiedNameSyntax>().Where(alias => alias.Alias.Identifier.ValueText == "global"), alias => Assert.True(alias.Ancestors().OfType<UsingDirectiveSyntax>().Any() || alias.Ancestors().OfType<ReturnStatementSyntax>().Any()));
    }
}
