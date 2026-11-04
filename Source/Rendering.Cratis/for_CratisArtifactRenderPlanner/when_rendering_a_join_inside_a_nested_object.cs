// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Ensures a join inside a root-level nested object renders as a nested Chronicle join.
/// </summary>
public class when_rendering_a_join_inside_a_nested_object : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _code = null!;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("nested-join"), "nested-join", "NestedJoin.play", nested_join_projection.Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        _plan = CratisRendering.Plan(model, execution.Plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
        var artifact = _plan.Artifacts.SingleOrDefault(candidate => candidate.RelativePath == "Projects/Registration/NestedJoinLookup/NestedJoinLookup.cs");
        _code = artifact is null ? string.Empty : Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
    }

    [Fact] void should_succeed() => Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
    [Fact] void should_emit_the_join_on_the_nested_object() => _code.ShouldContain("nested.Join<");
    [Fact] void should_join_on_the_reference() => _code.ShouldContain("join.On(model => model.Reference)");
}
#endif
