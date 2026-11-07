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
/// Ensures joins inside nested objects stay refused where Chronicle's engine cannot wire them.
/// </summary>
public class when_rejecting_nested_joins_chronicle_cannot_wire : Specification
{
    const string NestedLinkType = "type NestedLink\n  reference ProjectId\n  value ProjectName?\n";

    [Fact]
    void should_refuse_a_join_inside_a_nested_object_under_children()
    {
        var source = when_rendering_scoped_projections.ScopedSource
            .Replace("type ProjectNote\n  noteId ProjectId\n  name ProjectName\n", NestedLinkType + "type ProjectNote\n  noteId ProjectId\n  name ProjectName\n  inner NestedLink?\n", StringComparison.Ordinal)
            .Replace(
                "          remove with ProjectNoteRemoved key noteId",
                "          nested inner\n            from ProjectRegistered key projectId\n              reference = projectId\n              value = name\n            join linked on reference\n              with ProjectNamed\n                value = name\n          remove with ProjectNoteRemoved key noteId",
                StringComparison.Ordinal);

        Assert.Contains("A join inside a nested object under children is not wired by Chronicle's engine.", Refusal(source), StringComparison.Ordinal);
    }

    [Fact]
    void should_refuse_a_nested_join_on_the_nested_key()
    {
        var source = nested_join_projection.Source
            .Replace("type NestedLink\n", "type NestedLink\n  id ProjectId\n", StringComparison.Ordinal)
            .Replace("            reference = projectId\n", "            reference = projectId\n            id = projectId\n", StringComparison.Ordinal)
            .Replace("join linked on reference", "join linked on id", StringComparison.Ordinal);

        Assert.Contains("A nested join on the nested object's key cannot render", Refusal(source), StringComparison.Ordinal);
    }

    static string Refusal(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("nested-join"), "nested-join", "Scopes.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        if (!execution.Success)
        {
            return string.Join(Environment.NewLine, execution.Issues.Select(issue => issue.Details));
        }

        var plan = CratisRendering.Plan(model, execution.Plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        return Assert.Single(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-017").Message;
    }
}
#endif
