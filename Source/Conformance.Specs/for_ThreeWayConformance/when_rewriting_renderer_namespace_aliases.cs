// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_rewriting_renderer_namespace_aliases : Specification
{
    RenderedFile[] _files = null!;
    QualifiedOutputResult _proof = null!;

    void Establish()
    {
        GeneratedTypeName[] types = [new("Model.Types", "Outer")];
        var source = new CSharpCodeBuilder(GeneratedProjects.Application, types, hasScaffoldGlobalUsings: false)
            .Namespace("Example").OpenBlock("public class Host<Outer>")
            .Line("public global::Model.Types.Outer Value = null!;").EndBlock().ToString();
        _files = [new("Outer.cs", "namespace Model.Types; public class Outer {}"), new("Host.cs", source)];
    }

    void Because() => _proof = QualifiedOutputProof.Verify(_files);

    [Fact] void should_compile_the_actual_renderer_output() => Assert.Empty(QualifiedOutputProof.CompilationErrors(_files));
    [Fact] void should_accept_the_renderer_alias_without_an_unknown_scope() => Assert.Empty(_proof.Failures);
    [Fact] void should_preserve_the_existing_alias() => Assert.Equal(_files[1].Content, _proof.Rewritten.Single(file => file.RelativePath == "Host.cs").Content);

    [Fact]
    void should_reserve_the_existing_alias_when_another_collision_needs_one()
    {
        var files = _files.Select(file => file with
        {
            Content = file.Content.Replace("Host<Outer>", "Host<Outer, Guid>", StringComparison.Ordinal)
                .Replace("Value = null!;", "Value = null!; public global::System.Guid Identifier = default;", StringComparison.Ordinal)
        }).ToArray();
        var proof = QualifiedOutputProof.Verify(files);
        var host = proof.Rewritten.Single(file => file.RelativePath == "Host.cs").Content;
        Assert.Empty(proof.Failures);
        Assert.Empty(QualifiedOutputProof.CompilationErrors(proof.Rewritten));
        Assert.Contains("using StageImport0 = global::Model.Types;", host);
        Assert.Contains("using StageImport1 = global::System;", host);
        Assert.Contains("StageImport1::Guid", host);
    }

    [Theory]
    [InlineData("using Alias = System.Guid;")]
    [InlineData("using static System.Math;")]
    public void should_still_reject_non_namespace_alias_scopes(string directive) =>
        Assert.Throws<CSharpImportRejected>(() => QualifiedOutputProof.Verify([new("Host.cs", $"{directive}\nnamespace Example; public class Host {{}}")]));
}
#endif
