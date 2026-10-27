// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_rewriting_a_qualified_reference_with_an_alias : Specification
{
    RenderedFile[] _files = null!;
    QualifiedOutputResult _proof = null!;
    CompilationUnitSyntax _rewritten = null!;

    void Establish() => _files =
    [
        new("Outer.cs", "namespace Model.Types; public class Outer {}"),
        new("Host.cs", "namespace Example; public class Host<Outer> { public global::Model.Types.Outer Value = null!; }")
    ];

    void Because()
    {
        _proof = QualifiedOutputProof.Verify(_files);
        _rewritten = CSharpSyntaxTree.ParseText(_proof.Rewritten.Single(file => file.RelativePath == "Host.cs").Content).GetCompilationUnitRoot();
    }

    [Fact] void should_compile_the_qualified_fixture() => Assert.Empty(QualifiedOutputProof.CompilationErrors(_files, legacyImports: true));
    [Fact] void should_preserve_the_binding_after_rewriting() => Assert.Empty(_proof.Failures);
    [Fact] void should_compile_the_aliased_output() => Assert.Empty(QualifiedOutputProof.CompilationErrors(_proof.Rewritten));

    [Fact]
    void should_declare_the_alias_used_by_the_colliding_reference()
    {
        var reference = Assert.Single(_rewritten.DescendantNodes().OfType<AliasQualifiedNameSyntax>(),
            name => !name.Ancestors().OfType<UsingDirectiveSyntax>().Any());
        Assert.StartsWith("StageImport", reference.Alias.Identifier.ValueText);
        Assert.Equal("Outer", reference.Name.ToString());
        var directive = Assert.Single(_rewritten.Usings, @using => @using.Alias?.Name.Identifier.ValueText == reference.Alias.Identifier.ValueText);
        Assert.Equal("global::Model.Types", directive.NamespaceOrType.ToString());
    }
}
#endif
