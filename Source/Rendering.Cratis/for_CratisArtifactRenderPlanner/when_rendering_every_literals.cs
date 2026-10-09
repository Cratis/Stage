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
    [Fact] void should_not_lower_literals_onto_joins() => _code.ShouldNotContain("join.Set(model => model.Label)");
    [Fact] void should_replace_the_colliding_set() => _code.ShouldNotContain("from.Set(model => model.Label).ToValue(\"local\")");
    [Fact] void should_keep_context_independent_every_mappings() => _code.ShouldContain("every.Set(model => model.LastSeen).ToEventSourceId()");
    [Fact] void should_not_call_the_missing_all_set_to_value() => _code.ShouldNotContain("every.Set(model => model.Label)");
    [Fact] void should_apply_literals_at_the_nested_level() => _code.ShouldContain("from.Set(model => model.Name).ToValue(new global::Projects.Common.ProjectName(\"nested\"))");
    [Fact] void should_apply_literals_at_the_child_level() => _code.ShouldContain("from.Set(model => model.Name).ToValue(new global::Projects.Common.ProjectName(\"child\"))");

    [Fact]
    void should_allow_literals_at_join_free_child_and_nested_levels_below_a_root_join()
    {
        var source = every_literal_projection.Source
            .Replace("label String", "label String?", StringComparison.Ordinal)
            .Replace("          label = \"fixed\"\n", string.Empty, StringComparison.Ordinal)
            .Replace("\n        every\n", "\n        join project on projectId\n          with ProjectNamed\n            name = name\n        every\n", StringComparison.Ordinal);
        var plan = Plan(source);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
    }

    [Fact]
    void should_reject_a_from_mapping_of_an_ancestor_of_the_literal_target()
    {
        const string source = """
            concept ProjectId : Uuid
            type Details
              label String
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  command RegisterProject
                    projectId ProjectId identifier
                    details Details
                    produces ProjectRegistered
                      for projectId
                      projectId = projectId
                      details = details
                  event ProjectRegistered
                    projectId ProjectId
                    details Details
                slice StateView ProjectLookup
                  readmodel ProjectSummary
                    projectId ProjectId
                    details Details
                  query ProjectById => ProjectSummary?
                    by projectId ProjectId
                  projection ProjectSummaryProjection => ProjectSummary
                    no automap
                    from ProjectRegistered key projectId
                      details = details
                    every
                      details.label = "fixed"
            """;
        var plan = Plan(source);
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-017" &&
            diagnostic.Message.Contains("overlapping or non-Set from mapping", StringComparison.Ordinal));
    }

    static ArtifactRenderPlan Plan(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("every-level"), "every-level", "Every.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        return CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
    }
}
#endif
