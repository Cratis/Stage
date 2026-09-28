// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rejecting_a_play_event_property_rendered_as_true
{
    [Fact]
    public void should_not_generate_artifacts_for_a_false_value_rendered_as_a_boolean_literal()
    {
        const string source = """
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  command RegisterProject
                    projectId String identifier
                    flag Bool
                    produces ProjectRegistered
                      for projectId
                      projectId = projectId
                      true_ = flag
                  event ProjectRegistered
                    projectId String
                    true_ Bool
                  specification RegisteringAProject
                    when RegisterProject
                      projectId = "project-1"
                      flag = false
                    then ProjectRegistered
                      projectId = "project-1"
                      true_ = false
                    then readmodel ProjectSummary
                      key = "project-1"
                      flag = true
                slice StateView ProjectLookup
                  readmodel ProjectSummary
                    key String
                    flag Bool
                  query ProjectById => ProjectSummary?
                    by key String
                  projection ProjectSummaryProjection => ProjectSummary
                    from ProjectRegistered key $eventSourceId
                      flag = true_
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("true-property"), "true-property", "TrueProperty.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join("; ", execution.Issues));
        var plan = CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));

        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-017" && diagnostic.Message.Contains("Chronicle expression", StringComparison.Ordinal));
    }
}
