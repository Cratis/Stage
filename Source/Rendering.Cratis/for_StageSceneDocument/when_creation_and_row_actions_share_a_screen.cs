// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StageSceneDocument;

public class when_creation_and_row_actions_share_a_screen : Specification
{
    static readonly string FixturePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Frontend/Runtime/src/fixtures/row-actions-scene.json"));
    JsonNode _document = null!;
    string _json = null!;

    void Because()
    {
        var model = invoice_model.Compile(Model);
        var profile = CratisRendering.CreateProfile("Business", new("Business", "Business"));
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, new("Business", "Business"));
        _document = JsonNode.Parse(StageSceneDocument.Render("""
            {"toolbar":{"slice":"Business.Inventory.Resources.RegisterResource"},"screens":[{"name":"Resources",
              "forms":[{"name":"Register","forCommand":"RegisterResource","fields":[],"generationMode":"auto"}],
              "slotContent":{"content":[
                {"id":"register","componentName":"core:action","properties":{"command":"RegisterResource","label":"Register resource","rowAction":true}},
                {"id":"rename","componentName":"core:action","properties":{"command":"RenameResource","label":"Rename resource"}},
                {"componentName":"core:action","properties":{"command":"AddChildResource"}},
                {"componentName":"core:action","properties":{"command":"RegisterSummary"}},
                {"componentName":"core:table","properties":{"typeName":"Resource","rows":[]}}
              ]}}]}
            """,
            context))!;
        _json = _document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n";
        if (Environment.GetEnvironmentVariable("STAGE_UPDATE_VECTORS") == "1") File.WriteAllText(FixturePath, _json);
    }

    [Fact] void should_match_the_runtime_acceptance_fixture() => File.ReadAllText(FixturePath).ShouldEqual(_json);
    [Fact] void should_mark_the_inline_creation_schema() => Schema(0)["x-creates"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_mark_the_by_name_creation_schema() => RouteSchema("RegisterResource")["x-creates"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_carry_creation_intent_into_the_action() => Action(0)["createsEntity"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_carry_creation_intent_into_the_inline_form() => _document["screens"]![0]!["slotContent"]!["forms"]![0]!["properties"]!["createsEntity"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_preserve_the_supplied_creation_identifier() => Action(0)["fields"]!.AsArray().Select(field => field!["name"]!.GetValue<string>()).ShouldContainOnly("resourceId", "name");
    [Fact] void should_not_mark_an_update_as_creation() => (Schema(1)["x-creates"] is null).ShouldBeTrue();
    [Fact] void should_not_mark_an_update_by_name_as_creation() => (RouteSchema("RenameResource")["x-creates"] is null).ShouldBeTrue();
    [Fact] void should_not_mark_a_parent_bound_creation() => (Schema(2)["x-creates"] is null).ShouldBeTrue();
    [Fact] void should_not_mark_an_ambiguous_creation() => (Schema(3)["x-creates"] is null).ShouldBeTrue();
    [Fact] void should_keep_negative_intent_on_the_action() => Action(1)["createsEntity"]!.GetValue<bool>().ShouldBeFalse();

    JsonObject Action(int index) => _document["screens"]![0]!["slotContent"]!["content"]![index]!["properties"]!.AsObject();
    JsonNode Schema(int index) => JsonNode.Parse(Action(index)["schema"]!.GetValue<string>())!;
    JsonNode RouteSchema(string name) => JsonNode.Parse(_document["commandSchemas"]![name]!.GetValue<string>())!;

    const string Model = """
        concept ResourceId : Uuid
        concept ParentId : Uuid
        concept SummaryId : Uuid
        module Business
          feature Inventory
            feature Resources
              slice StateChange RegisterResource
                command RegisterResource
                  resourceId ResourceId identifier
                  name String
                  produces ResourceRegistered
                    for resourceId
                    name = name
                event ResourceRegistered
                  name String
              slice StateChange RenameResource
                command RenameResource
                  resourceId ResourceId identifier
                  name String
                  produces ResourceRenamed
                    for resourceId
                    name = name
                event ResourceRenamed
                  name String
              slice StateChange AddChildResource
                command AddChildResource
                  resourceId ResourceId identifier
                  parentId ParentId
                  name String
                  produces ResourceRegistered
                    for resourceId
                    name = name
              slice StateChange RegisterSummary
                command RegisterSummary
                  summaryId SummaryId identifier
                  name String
                  produces SummaryRegistered
                    for summaryId
                    name = name
                event SummaryRegistered
                  name String
              slice StateChange ImportSummary
                command ImportSummary
                  summaryId SummaryId identifier
                  name String
                  produces SummaryImported
                    for summaryId
                    name = name
                event SummaryImported
                  name String
              slice StateView Summaries
                readmodel Summary
                  summaryId SummaryId
                  name String
                query AllSummaries => Summary[]
                projection SummariesProjection => Summary
                  from SummaryRegistered
                    summaryId = $eventSourceId
                    name = name
                  from SummaryImported
                    summaryId = $eventSourceId
                    name = name
              slice StateView Resources
                readmodel Resource
                  resourceId ResourceId
                  name String
                query AllResources => Resource[]
                projection ResourcesProjection => Resource
                  from ResourceRegistered
                    resourceId = $eventSourceId
                    name = name
                  from ResourceRenamed
                    name = name
        """;
}
