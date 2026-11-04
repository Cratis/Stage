// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Ensures a join in a nested object that is itself inside a nested object is refused: only root-level nested joins are admitted.
/// </summary>
public class when_rejecting_a_join_in_a_nested_object_inside_a_nested_object : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = nested_join_projection.Source
            .Replace("type NestedLink\n", "type Deep\n  reference ProjectId\n  value ProjectName?\ntype NestedLink\n  inner Deep?\n", StringComparison.Ordinal)
            .Replace("        from ProjectRenamed\n", "        from ProjectRenamed\n        from ProjectNoted key projectId\n", StringComparison.Ordinal)
            .Replace(
                "          join linked on reference\n            with ProjectNamed\n              value = name",
                "          nested inner\n            from ProjectNoted key projectId\n              reference = noteId\n              value = name\n            join deepLinked on reference\n              with ProjectNamed\n                value = name",
                StringComparison.Ordinal);
        Assert.Contains("nested inner", source, StringComparison.Ordinal);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("nested-join"), "nested-join", "Deep.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        _plan = CratisRendering.Plan(model, execution.Plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
    }

    [Fact] void should_not_render() => _plan.Success.ShouldBeFalse();
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_name_the_shape() => _plan.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-017").Message.ShouldContain("directly at the read-model root");
}
#endif
