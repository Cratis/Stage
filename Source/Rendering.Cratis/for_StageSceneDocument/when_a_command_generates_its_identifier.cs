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

public class when_a_command_generates_its_identifier : Specification
{
    JsonObject _form = null!;
    string _dialogSchema = null!;

    void Because()
    {
        var model = invoice_model.Compile("""
            concept OpportunityId : Uuid
            concept Reference : String
              generate nanoid 12
            module Sales
              feature Opportunities
                slice StateChange CaptureOpportunity
                  command CaptureOpportunity
                    opportunityId OpportunityId generated identifier
                    title String
                    reference Reference generated
                    produces OpportunityCaptured
                      for opportunityId
                      title = title
                    returns opportunityId
                  event OpportunityCaptured
                    title String
            """);
        var profile = CratisRendering.CreateProfile("Sales", new("Sales", "Sales"));
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, new("Sales", "Sales"));
        var result = StageSceneDocument.Render(
            """
            {"toolbar":{"slice":"Sales.Opportunities.CaptureOpportunity"},"screens":[{"name":"Capture","forms":[{"name":"CaptureOpportunity","forCommand":"CaptureOpportunity","fields":[],"generationMode":"auto"}]}]}
            """,
            context);
        var document = JsonNode.Parse(result);
        _form = document["screens"][0]["slotContent"]["forms"][0]["properties"].AsObject();
        _dialogSchema = document["commandSchemas"]["CaptureOpportunity"].GetValue<string>();
    }

    [Fact] void should_only_ask_for_properties_the_person_enters() => _form["fields"].AsArray().Select(field => field["name"].GetValue<string>()).ShouldContainOnly("title");
    [Fact] void should_not_require_the_generated_identifier() => _form["schema"].GetValue<string>().ShouldNotContain("opportunityId");
    [Fact] void should_not_require_other_generated_properties() => _form["schema"].GetValue<string>().ShouldNotContain("reference");
    [Fact] void should_not_ask_for_generated_properties_in_the_toolbar_dialog() => JsonNode.Parse(_dialogSchema)["properties"].AsObject().Select(property => property.Key).ShouldContainOnly("title");
}
