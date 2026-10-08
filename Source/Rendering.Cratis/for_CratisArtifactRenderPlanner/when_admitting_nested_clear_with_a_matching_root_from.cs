// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Admits nested clear without relaxing the matching root event and key requirement.
/// </summary>
public class when_admitting_nested_clear_with_a_matching_root_from : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        // ClearWith binds to the event-source key, so its root from must use the same key.
        var source = when_rendering_scoped_projections.ScopedSource
            .Replace("projection ProjectSummaryProjection => ProjectSummary\n", "projection ProjectSummaryProjection => ProjectSummary\n        no automap\n", StringComparison.Ordinal)
            .Replace("from ProjectRenamed key projectId", "from ProjectRenamed", StringComparison.Ordinal)
            .Replace("        children notes identified by noteId", "          clear with ProjectRenamed\n        children notes identified by noteId", StringComparison.Ordinal);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("nested-clear"), "nested-clear", "Scopes.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        _plan = CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
    }

    [Fact] void should_admit_the_projection() => Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
    [Fact] void should_materialize_the_root_identifier_from_the_property_key() => ProjectionCode().ShouldContain("from.Set(model => model.ProjectId).To(evt => evt.ProjectId)");
    [Fact] void should_materialize_the_root_identifier_from_the_event_source_key() => ProjectionCode().ShouldContain("from.Set(model => model.ProjectId).ToEventSourceId()");
    [Fact] void should_render_the_existing_nested_removal_lowering() => Assert.Contains(
        "nested.RemovedWith<global::Projects.Projects.Registration.RegisterProject.ProjectRenamed>",
        Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath == "Projects/Registration/ProjectLookup/ProjectLookup.cs").Bytes.AsSpan()),
        StringComparison.Ordinal);

    string ProjectionCode() => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath == "Projects/Registration/ProjectLookup/ProjectLookup.cs").Bytes.AsSpan());
}
