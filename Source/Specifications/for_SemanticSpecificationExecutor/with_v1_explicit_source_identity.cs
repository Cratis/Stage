// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class with_v1_explicit_source_identity : Specification
{
    [Fact]
    public async Task should_project_the_explicit_v1_command_source_without_a_reaction()
    {
        var plan = Compile(false);
        plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
        plan.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
            .Any(slice => !slice.Reactions.IsDefaultOrEmpty).ShouldBeFalse();
        await ShouldProjectExplicitSource(plan, 1);
    }

    [Fact]
    public void should_compile_the_colocated_v6_cascade_fixture()
    {
        var plan = Compile(true);
        plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V6);
    }

    [Fact]
    public async Task should_inherit_the_explicit_command_source_in_a_v6_reaction_without_an_override()
    {
        var plan = Compile(true);
        plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V6);
        var reaction = plan.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
            .Where(slice => !slice.Reactions.IsDefaultOrEmpty).SelectMany(slice => slice.Reactions).Single();
        Assert.Single(Assert.Single(reaction.Triggers).Produces).Destination.ShouldBeNull();
        await ShouldProjectExplicitSource(plan, 2);
    }

    static async Task ShouldProjectExplicitSource(SemanticExecutionPlan plan, int factCount)
    {
        SemanticSpecificationAdmission.Check(plan, plan.Specifications.Values.Single()).ShouldBeNull();
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed,
            $"{result.Outcome}: {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
        result.Trace!.Facts.Count.ShouldEqual(factCount);
        result.Trace.Facts[0].EventContract.ShouldEqual(plan.Events.Values.Single(contract => contract.Name == "ProjectRegistered").Id.ToString());
        result.Trace.Facts[^1].EventContract.ShouldEqual(plan.Events.Values.Single(contract => contract.Name == (factCount == 1 ? "ProjectRegistered" : "ProjectFollowed")).Id.ToString());
        Assert.All(result.Trace.Facts, fact =>
        {
            fact.EventSourceType.ShouldEqual(plan.Model.SemanticVersion == SemanticVersion.V1 ? null : nameof(SemanticPrimitiveType.Text));
            fact.EventSource.ShouldEqual("\"project-1\"");
        });
        var readModel = plan.ReadModels.Values.Single();
        var projected = Assert.Single(result.Trace.ReadModels);
        projected.Key.ShouldEqual($"{readModel.Id}:\"project-1\"");
        var values = JsonSerializer.Deserialize<Dictionary<string, string>>(projected.Value)!;
        values[readModel.Properties.Single(property => property.Name == "key").Id.ToString()].ShouldEqual("\"project-1\"");
        values[readModel.Properties.Single(property => property.Name == "name").Id.ToString()].ShouldEqual("\"hello\"");
    }

    static SemanticExecutionPlan Compile(bool reaction)
    {
        var source = $$"""
            module Projects
              feature Registration
                slice {{(reaction ? "Automation" : "StateChange")}} RegisterProject
                  command RegisterProject
                    projectId String identifier
                    name String
                    produces ProjectRegistered
                      for projectId
                      projectId = projectId
                      name = name
                  event ProjectRegistered
                    projectId String
                    name String
                  specification RegisteringAProject
                    when RegisterProject
                      projectId = "project-1"
                      name = "hello"
                    then ProjectRegistered
                      projectId = "project-1"
                      name = "hello"{{(reaction ? "\n        then ProjectFollowed\n          name = \"hello\"" : string.Empty)}}
                    then readmodel ProjectSummary
                      key = "project-1"
                      name = "hello"
            {{(reaction ? "      event ProjectFollowed\n        name String\n      reaction FollowProject\n        when ProjectRegistered\n          name\n          produces ProjectFollowed\n            name = name" : string.Empty)}}
                slice StateView ProjectLookup
                  readmodel ProjectSummary
                    key String
                    name String
                  query ProjectById => ProjectSummary?
                    by key String
                  projection ProjectSummaryProjection => ProjectSummary
                    from {{(reaction ? "ProjectFollowed" : "ProjectRegistered")}} key $eventSourceId
                      name = name
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ExplicitSourceIdentity"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", source);
        var compilation = new SemanticModelCompiler().Compile("ExplicitSourceIdentity", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var compiled = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        Assert.NotNull(compiled.Plan);

        return compiled.Plan!;
    }
}
