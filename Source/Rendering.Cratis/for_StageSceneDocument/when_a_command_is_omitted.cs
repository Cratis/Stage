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

public class when_a_command_is_omitted : Specification
{
    JsonObject _schemas = null!;
    JsonObject _schemasWithoutOmission = null!;

    void Because()
    {
        var model = invoice_model.Compile("""
            concept OpportunityId : Uuid
            module Sales
              feature Opportunities
                slice StateChange CaptureOpportunity
                  command CaptureOpportunity
                    opportunityId OpportunityId generated identifier
                    title String
                    produces OpportunityCaptured
                      for opportunityId
                      title = title
                    returns opportunityId
                  event OpportunityCaptured
                    title String
            """);
        var profile = CratisRendering.CreateProfile("Sales", new("Sales", "Sales"));
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, new("Sales", "Sales"));
        const string scene = "{\"toolbar\":{\"slice\":\"Sales.Opportunities.CaptureOpportunity\"},\"screens\":[]}";
        _schemasWithoutOmission = JsonNode.Parse(StageSceneDocument.Render(scene, context))!["commandSchemas"]!.AsObject();
        context.OmittedOwners = [.. context.Commands.Keys];
        _schemas = JsonNode.Parse(StageSceneDocument.Render(scene, context))!["commandSchemas"]!.AsObject();
    }

    [Fact] void should_describe_the_command_when_nothing_is_omitted() => _schemasWithoutOmission.ContainsKey("CaptureOpportunity").ShouldBeTrue();
    [Fact] void should_not_describe_the_omitted_command() => _schemas.ContainsKey("CaptureOpportunity").ShouldBeFalse();
}
