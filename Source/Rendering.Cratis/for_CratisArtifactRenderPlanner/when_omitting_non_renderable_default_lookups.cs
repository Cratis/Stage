// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_omitting_non_renderable_default_lookups : a_register_project_render_request
{
    [Theory]
    [InlineData("Uuid", true, false)]
    [InlineData("Date", true, false)]
    [InlineData("DateTime", true, false)]
    [InlineData("Payload", true, false)]
    [InlineData("String", false, false)]
    [InlineData("String", true, true)]
    void should_compose_only_usable_queries_after_real_semantic_admission(string resultType, bool hasQuery, bool expectLookup)
    {
        var source = $$"""
            concept ProjectId : Uuid
            type Payload
              text String
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  command RegisterProject
                    projectId ProjectId identifier
                    value {{resultType}}
                    produces ProjectRegistered
                      for projectId
                      projectId = projectId
                      value = value
                  event ProjectRegistered
                    projectId ProjectId
                    value {{resultType}}
                slice StateView ProjectLookup
                  readmodel ProjectSummary
                    projectId ProjectId
                    value {{resultType}}
                  query ProjectById => ProjectSummary?
                    by projectId ProjectId
                  projection ProjectSummaryProjection => ProjectSummary
                    from ProjectRegistered key projectId
                      value = value
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("lookup-policy"), "lookup-policy", "Lookup.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        if (!hasQuery)
        {
            // Text binding currently needs a query to infer the identifier. The validated semantic model
            // already has that identity and permits a projection without an exposed query.
            var module = model.Application.Modules.Single();
            var feature = module.Features.Single();
            var application = model.Application with
            {
                Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(slice => slice with { Queries = [] })] }] }]
            };
            model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application);
        }

        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), _options);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        if (!hasQuery)
        {
            Text(plan.Artifacts.Single(_ => _.RelativePath == SceneBindingsRenderer.RelativePath)).ShouldNotContain("registerQueryIdentity");
        }

        using var scene = JsonDocument.Parse(Text(plan.Artifacts.Single(_ => _.RelativePath == SceneCompositionInput.RelativePath)));
        var elements = scene.RootElement.GetProperty("screens")[0].GetProperty("slotContent").GetProperty(DefaultLayout.ContentSlotName).EnumerateArray().ToArray();
        elements.Count(_ => _.GetProperty("componentName").GetString() == "Cratis.Components:commandForm").ShouldEqual(1);
        elements.Any(_ => _.GetProperty("componentName").GetString() == "Cratis.Components:queryInputForm").ShouldEqual(expectLookup);
        if (expectLookup)
        {
            elements.Single(_ => _.GetProperty("componentName").GetString() == "Cratis.Components:queryInputForm").GetProperty("properties").GetProperty("resultField").GetString().ShouldEqual("value");
        }
    }
}
