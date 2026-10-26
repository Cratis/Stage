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

public class when_scaffold_spelling_removes_the_last_alias_use : Specification
{
    string _source = null!;
    CSharpCompilation _compilation = null!;
    SyntaxTree _tree = null!;

    void Because()
    {
        _source = new CSharpCodeBuilder(GeneratedProjects.Application, [new("Shop.Orders", "Subject", 1)], hasScaffoldGlobalUsings: true)
            .Namespace("Shop.Orders")
            .Line("public class Subject<T> {}")
            .OpenBlock("public class Host")
            .Line("public global::System.Reactive.Subjects.Subject<int> Value = null!;")
            .EndBlock().ToString();
        _tree = CSharpSyntaxTree.ParseText(_source);
        _compilation = CSharpCompilation.Create(
            "ScaffoldAlias",
            [_tree],
            [.. PureTransitionAdmission.AnalysisReferences(), MetadataReference.CreateFromFile(typeof(System.Reactive.Subjects.Subject<>).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable)
                .WithSpecificDiagnosticOptions([new KeyValuePair<string, ReportDiagnostic>("CS1701", ReportDiagnostic.Suppress)]));
    }

    [Fact] void should_choose_the_rooted_scaffold_spelling() => _source.ShouldContain("public global::System.Reactive.Subjects.Subject<int> Value");
    [Fact] void should_not_emit_an_unused_alias() => _source.ShouldNotContain("using StageImport");
    [Fact] void should_compile_without_errors() => Assert.Empty(_compilation.GetDiagnostics().Where(SemanticBodyCompilation.IsFailure));
    [Fact] void should_not_leave_an_unnecessary_using_diagnostic() => Assert.DoesNotContain(_compilation.GetDiagnostics(), diagnostic => diagnostic.Id == "CS8019");
    [Fact]
    void should_bind_to_the_framework_type_instead_of_the_local_model()
    {
        var field = _tree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>().Single();
        var symbol = (IFieldSymbol)_compilation.GetSemanticModel(_tree).GetDeclaredSymbol(field.Declaration.Variables.Single())!;
        Assert.True(SymbolEqualityComparer.Default.Equals(symbol.Type.OriginalDefinition, _compilation.GetTypeByMetadataName("System.Reactive.Subjects.Subject`1")));
    }
}
