// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StageSceneDocument;

public class when_creation_and_row_actions_share_a_collection : Specification
{
    const string Model = """
            concept ResourceId : Uuid
            concept ParentId : Uuid
            concept SummaryId : Uuid
            module Business
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

    JsonNode _document = null;

    void Because()
    {
        var model = invoice_model.Compile(Model);
        var profile = CratisRendering.CreateProfile("Business", new("Business", "Business"));
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, new("Business", "Business"));
        // lang=json
        var scene = """
            {"toolbar":{"slice":"Business.Resources.RegisterResource"},"screens":[{"name":"Resources","slotContent":{"content":[
              {"componentName":"core:action","properties":{"command":"RegisterResource","rowAction":true}},
              {"componentName":"core:action","properties":{"command":"RenameResource","rowAction":true}},
              {"componentName":"core:action","properties":{"command":"AddChildResource","rowAction":true}},
              {"componentName":"core:action","properties":{"command":"RegisterSummary","rowAction":true}},
              {"componentName":"core:table","properties":{"typeName":"Resource","rows":[]}},
              {"componentName":"core:table","properties":{"typeName":"Summary","rows":[]}}
            ]}}]}
            """;
        _document = JsonNode.Parse(StageSceneDocument.Render(scene, context));
    }

    [Fact] void should_mark_the_inline_creation_schema() => Schema(0)["x-creates"].GetValue<bool>().ShouldBeTrue();
    [Fact] void should_mark_the_route_creation_schema() => RouteSchema("RegisterResource")["x-creates"].GetValue<bool>().ShouldBeTrue();
    [Fact] void should_clear_the_inherited_row_hint_on_the_creation() => Action(0).ContainsKey("rowAction").ShouldBeFalse();
    [Fact] void should_carry_creation_intent_into_the_form() => Action(0)["createsEntity"].GetValue<bool>().ShouldBeTrue();
    [Fact] void should_ask_for_the_callers_identifier_even_when_the_collection_is_empty() => Fields(0).ShouldContainOnly("resourceId", "name");
    [Fact] void should_keep_the_row_action_bound() => Action(1)["rowAction"].GetValue<bool>().ShouldBeTrue();
    [Fact] void should_hide_only_the_row_actions_identifier() => Fields(1).ShouldContainOnly("name");
    [Fact] void should_not_mark_the_inline_row_schema_as_creation() => (Schema(1)["x-creates"] is null).ShouldBeTrue();
    [Fact] void should_not_mark_the_route_row_schema_as_creation() => (RouteSchema("RenameResource")["x-creates"] is null).ShouldBeTrue();
    [Fact] void should_keep_the_key_in_the_creation_route_schema() => RouteSchema("RegisterResource")["properties"]["resourceId"]["x-concept"].GetValue<string>().ShouldEqual("ResourceId");
    [Fact] void should_keep_the_key_in_the_row_action_route_schema() => RouteSchema("RenameResource")["properties"]["resourceId"]["x-concept"].GetValue<string>().ShouldEqual("ResourceId");
    [Fact] void should_not_mark_a_parent_and_child_action_inline() => (Schema(2)["x-creates"] is null).ShouldBeTrue();
    [Fact] void should_not_mark_a_parent_and_child_action_route() => (RouteSchema("AddChildResource")["x-creates"] is null).ShouldBeTrue();
    [Fact] void should_keep_the_parent_and_child_action_row_bound() => Action(2)["rowAction"].GetValue<bool>().ShouldBeTrue();
    [Fact] void should_not_mark_an_ambiguous_creation_inline() => (Schema(3)["x-creates"] is null).ShouldBeTrue();
    [Fact] void should_not_mark_an_ambiguous_creation_route() => (RouteSchema("RegisterSummary")["x-creates"] is null).ShouldBeTrue();
    [Fact] void should_keep_an_ambiguous_creation_row_bound() => Action(3)["rowAction"].GetValue<bool>().ShouldBeTrue();

    JsonObject Action(int index) => _document["screens"][0]["slotContent"]["content"][index]["properties"].AsObject();
    JsonNode Schema(int index) => JsonNode.Parse(Action(index)["schema"].GetValue<string>());
    JsonNode RouteSchema(string command) => JsonNode.Parse(_document["commandSchemas"][command].GetValue<string>());
    string[] Fields(int index) => [.. Action(index)["fields"].AsArray().Select(field => field["name"].GetValue<string>())];
}
