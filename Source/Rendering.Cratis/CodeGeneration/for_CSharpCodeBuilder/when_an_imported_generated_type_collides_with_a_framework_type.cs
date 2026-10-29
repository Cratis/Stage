// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpCodeBuilder;

public class when_an_imported_generated_type_collides_with_a_framework_type
{
    [Fact]
    void should_bind_the_generated_type_and_the_aliased_framework_identity()
    {
        var source = new CSharpCodeBuilder(new ImportRequest("Shop.Orders", GeneratedProjects.Application, [new("Shop.Common", "Guid")], []))
            .Namespace("Shop.Orders").Line("public record Order(global::Shop.Common.Guid Key, global::System.Guid Id, global::System.DateTime Occurred);").ToString();
        Assert.Contains("using StageImport0 = global::System;", source);
        Assert.Contains("namespace Shop.Orders;\n\nusing Shop.Common;", source);
        Assert.DoesNotContain("using System;", source);
        var tree = CSharpSyntaxTree.ParseText(SemanticBodyCompilation.ImplicitUsings + source);
        var declaration = CSharpSyntaxTree.ParseText("namespace Shop.Common; public record Guid;");
        var compilation = CSharpCompilation.Create("GeneratedAlias", [tree, declaration], PureTransitionAdmission.AnalysisReferences(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, generalDiagnosticOption: ReportDiagnostic.Error, nullableContextOptions: NullableContextOptions.Enable));
        Assert.Empty(compilation.GetDiagnostics().Where(SemanticBodyCompilation.IsFailure));
        var order = compilation.GetTypeByMetadataName("Shop.Orders.Order")!;
        Assert.True(SymbolEqualityComparer.Default.Equals(((IPropertySymbol)Assert.Single(order.GetMembers("Key"))).Type, compilation.GetTypeByMetadataName("Shop.Common.Guid")));
        Assert.True(SymbolEqualityComparer.Default.Equals(((IPropertySymbol)Assert.Single(order.GetMembers("Id"))).Type, compilation.GetTypeByMetadataName("System.Guid")));
        Assert.True(SymbolEqualityComparer.Default.Equals(((IPropertySymbol)Assert.Single(order.GetMembers("Occurred"))).Type, compilation.GetTypeByMetadataName("System.DateTime")));
    }
}
