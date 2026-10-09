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

public class when_rendering_every_literals : Specification
{
    ExecutableSemanticModel _model = null!;
    SemanticExecutionPlan _execution = null!;
    ArtifactRenderPlan _plan = null!;
    string _code = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("every"), "every", "Every.play", every_literal_projection.Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        _model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(_model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        _execution = execution.Plan!;
    }

    void Because()
    {
        _plan = CratisRendering.Plan(_model, _execution, new(ArtifactRenderScopeKind.Application, _model.Application.Id), new("Projects", "Projects"));
        Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
        _code = Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath == "Projects/Registration/ProjectLookup/ProjectLookup.cs").Bytes.AsSpan());
    }

    [Fact] void should_establish_required_properties_from_every() => _plan.Success.ShouldBeTrue();
    [Fact] void should_set_the_literal_on_both_from_events() => (_code.Split("from.Set(model => model.Label).ToValue(\"fixed\")", StringSplitOptions.None).Length - 1).ShouldEqual(2);
    [Fact] void should_set_the_literal_on_the_join() => _code.ShouldContain("join.Set(model => model.Label).ToValue(\"fixed\")");
    [Fact] void should_replace_the_colliding_set() => _code.ShouldNotContain("from.Set(model => model.Label).ToValue(\"local\")");
    [Fact] void should_keep_context_independent_every_mappings() => _code.ShouldContain("every.Set(model => model.LastSeen).ToEventSourceId()");
    [Fact] void should_not_call_the_missing_all_set_to_value() => _code.ShouldNotContain("every.Set(model => model.Label)");
    [Fact] void should_apply_literals_at_the_nested_level() => _code.ShouldContain("from.Set(model => model.Name).ToValue(new global::Projects.Common.ProjectName(\"nested\"))");
    [Fact] void should_apply_literals_at_the_child_level() => _code.ShouldContain("from.Set(model => model.Name).ToValue(new global::Projects.Common.ProjectName(\"child\"))");
}
#endif
