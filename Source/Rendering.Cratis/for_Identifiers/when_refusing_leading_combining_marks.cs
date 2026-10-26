// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_Identifiers;

public class when_refusing_leading_combining_marks : Specification
{
    [Theory]
    [InlineData("_\u0301x")]
    [InlineData("_\u0903x")]
    [InlineData("_\u20ddx")]
    void should_refuse_a_combining_mark_exposed_by_separator_removal(string name)
    {
        var error = Assert.Throws<InvalidGeneratedIdentifier>(() => GeneratedPascalCase.From(name));
        Assert.Equal("STAGE-ESM-019", error.Code);
    }

    [Fact]
    void should_keep_a_combining_mark_following_a_letter()
    {
        Assert.Equal("A\u0301x", GeneratedPascalCase.From("a\u0301x"));
    }

    [Fact]
    void should_return_a_typed_diagnostic_from_the_render_planner()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("shop"), "shop", "Shop.play", "concept Reference : String");
        var compilation = new SemanticModelCompiler().Compile("Shop", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success);
        var original = compilation.Value!.Model;
        var model = ExecutableSemanticModel.Create(
            original.LanguageVersion,
            original.SemanticVersion,
            original.Application with { Concepts = [original.Application.Concepts.Single() with { Name = "_\u0301x" }] });
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success);
        var plan = CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Shop", "Shop"));
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-019");
    }
}
