// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rejecting_colliding_generated_property_names
{
    const string Source = """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId String identifier
                displayName String
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  displayName = displayName
              event ProjectRegistered
                projectId String
                displayName String
        """;

    [Theory]
    [InlineData("event")]
    [InlineData("command")]
    public void should_reject_direct_semantic_property_collisions(string kind)
    {
        var model = Compile(Source);
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var changed = slice with
        {
            Events = kind == "event" ? [.. slice.Events.Select(@event => @event with
            {
                Properties = [.. @event.Properties.Select(property => property.Name == "displayName" ? property with { Name = "project_id" } : property)]
            })] : slice.Events,
            Commands = kind == "command" ? [.. slice.Commands.Select(command => command with
            {
                Properties = [.. command.Properties.Select(property => property.Name == "displayName" ? property with { Name = "project_id" } : property)]
            })] : slice.Commands
        };
        var changedModel = ExecutableSemanticModel.Create(
            model.LanguageVersion,
            model.SemanticVersion,
            model.Application with { Modules = [module with { Features = [feature with { Slices = [changed] }] }] });

        AssertRejected(changedModel, kind);
    }

    [Theory]
    [InlineData("event")]
    [InlineData("command")]
    public void should_reject_play_property_collisions(string kind)
    {
        var source = kind == "event"
            ? Source.Replace("event ProjectRegistered\n", "event ProjectRegistered\n        project_id String\n", StringComparison.Ordinal)
                .Replace("displayName = displayName\n", "displayName = displayName\n          project_id = displayName\n", StringComparison.Ordinal)
            : Source.Replace("command RegisterProject\n", "command RegisterProject\n        project_id String\n", StringComparison.Ordinal);
        AssertRejected(Compile(source), kind);
    }

    [Fact]
    public void should_reject_a_colliding_referenced_event_even_when_only_its_state_view_is_selected()
    {
        var model = Compile(Source + "\n    " + """
            slice StateView ProjectLookup
              readmodel ProjectSummary
                projectId String
                displayName String
              query ProjectById => ProjectSummary?
                by projectId String
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered key $eventSourceId
                  displayName = displayName
            """.Replace("\n", "\n    ", StringComparison.Ordinal));
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var changed = feature with
        {
            Slices = [.. feature.Slices.Select(slice => slice with
            {
                Events = [.. slice.Events.Select(@event => @event with
                {
                    Properties = [.. @event.Properties.Select(property => property.Name == "displayName" ? property with { Name = "project_id" } : property)]
                })]
            })]
        };
        var changedModel = ExecutableSemanticModel.Create(
            model.LanguageVersion,
            model.SemanticVersion,
            model.Application with { Modules = [module with { Features = [changed] }] });
        var execution = SemanticExecutionPlan.Compile(changedModel);
        Assert.True(execution.Success, string.Join("; ", execution.Issues));
        var view = changed.Slices.Single(slice => slice.Kind == SemanticSliceKind.StateView);
        var plan = CratisRendering.Plan(changedModel, execution.Plan!, new(ArtifactRenderScopeKind.Slice, view.Id), new("Projects", "Projects"));

        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Message.Contains("references an event", StringComparison.Ordinal));
    }

    [Fact]
    public void should_keep_distinct_safe_names_supported()
    {
        var model = Compile(Source.Replace("displayName", "project_title", StringComparison.Ordinal));
        var plan = Plan(model);
        Assert.True(plan.Success, string.Join("; ", plan.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Assert.NotEmpty(plan.Artifacts);
    }

    static void AssertRejected(ExecutableSemanticModel model, string kind)
    {
        var plan = Plan(model);
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-012" &&
            diagnostic.Message.Contains(kind, StringComparison.OrdinalIgnoreCase) &&
            diagnostic.Message.Contains("generated C#", StringComparison.Ordinal));
    }

    static ArtifactRenderPlan Plan(ExecutableSemanticModel model)
    {
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join("; ", execution.Issues));
        return CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
    }

    static ExecutableSemanticModel Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("collision"), "collision", "Collision.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return compilation.Value!.Model;
    }
}
