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

public class when_rendering_all_literals : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _code = null!;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("all"), "all", "All.play", all_literal_projection.Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        _plan = CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
        Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
        _code = Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath == "Projects/Registration/AllLabelLookup/AllLabelLookup.cs").Bytes.AsSpan());
    }

    [Fact] void should_render_the_literal_inside_from_all() => _code.ShouldContain("every.Set(model => model.Label).ToValue(\"fixed\")");
    [Fact] void should_use_from_all() => _code.ShouldContain("builder.FromAll(every =>");
    [Fact] void should_not_lower_the_literal_onto_from_blocks() => _code.ShouldNotContain("from.Set(model => model.Label)");
    [Fact] void should_set_the_identifier_from_the_event_source() => _code.ShouldContain("every.Set(model => model.ProjectId).ToEventSourceId()");
    [Fact] void should_render_a_concept_literal_as_its_typed_value() => _code.ShouldContain("every.Set(model => model.Title).ToValue(new ProjectName(\"named\"))");
    [Fact] void should_render_a_non_null_literal_for_a_nullable_number() => _code.ShouldContain("every.Set(model => model.Score).ToValue(12.5m)");
}
#endif
