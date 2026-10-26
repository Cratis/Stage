// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CSharpCodeBuilder;

public class when_inline_bare_names_keep_their_original_binding : Specification
{
    const string Body = "        // authored bytes stay here\r\n        return new Subject<int>();\r\n";
    string _source = null!;
    CSharpCompilation _compilation = null!;
    CSharpCompilation _original = null!;
    SyntaxTree _tree = null!;
    SyntaxTree _originalTree = null!;

    void Because()
    {
        _source = new CSharpCodeBuilder(GeneratedProjects.Application, [new("Shop.Models", "Subject", 1)], hasScaffoldGlobalUsings: true)
            .Namespace("Shop.Orders")
            .OpenBlock("public class Host")
            .Line("public global::Shop.Models.Subject<int> Value = null!;")
            .OpenBlock("public object Run()")
            .RawVerbatim(Body).EndBlock().EndBlock().ToString();
        _tree = CSharpSyntaxTree.ParseText(_source);
        _originalTree = CSharpSyntaxTree.ParseText("namespace Shop.Orders; public class Original { public object Run() {\n" + Body + "} }");
        var globals = CSharpSyntaxTree.ParseText("global using System.Reactive.Subjects;");
        var model = CSharpSyntaxTree.ParseText("namespace Shop.Models { public class Subject<T> {} }");
        MetadataReference[] references = [.. PureTransitionAdmission.AnalysisReferences(), MetadataReference.CreateFromFile(typeof(System.Reactive.Subjects.Subject<>).Assembly.Location)];
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable);
        _compilation = CSharpCompilation.Create("RenderedInline", [_tree, globals, model], references, options);
        _original = CSharpCompilation.Create("OriginalInline", [_originalTree, globals, model], references, options);
    }

    [Fact] void should_preserve_the_authored_splice_byte_for_byte() => _source.ShouldContain(Body);
    [Fact] void should_not_import_a_namespace_that_rebinds_the_authored_name() => _source.ShouldNotContain("using Shop.Models;");
    [Fact] void should_compile_the_original_body() => Assert.Empty(_original.GetDiagnostics().Where(SemanticBodyCompilation.IsFailure));
    [Fact] void should_compile_the_rendered_body() => Assert.Empty(_compilation.GetDiagnostics().Where(SemanticBodyCompilation.IsFailure));
    [Fact]
    void should_keep_the_authored_framework_binding_before_and_after_rendering()
    {
        AssertFrameworkBinding(_original, _originalTree);
        AssertFrameworkBinding(_compilation, _tree);
    }
    [Fact]
    void should_keep_the_generated_field_bound_to_the_model()
    {
        var field = _tree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>().Single();
        var symbol = (IFieldSymbol)_compilation.GetSemanticModel(_tree).GetDeclaredSymbol(field.Declaration.Variables.Single())!;
        Assert.True(SymbolEqualityComparer.Default.Equals(symbol.Type.OriginalDefinition, _compilation.GetTypeByMetadataName("Shop.Models.Subject`1")));
    }

    static void AssertFrameworkBinding(CSharpCompilation compilation, SyntaxTree tree)
    {
        var creation = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
        var symbol = compilation.GetSemanticModel(tree).GetTypeInfo(creation).Type!;
        Assert.True(SymbolEqualityComparer.Default.Equals(symbol.OriginalDefinition, compilation.GetTypeByMetadataName("System.Reactive.Subjects.Subject`1")));
    }
}
