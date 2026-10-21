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

public class when_a_table_shows_an_enumeration : Specification
{
    JsonArray _columns = null!;
    JsonArray _dialogOptions = null!;

    void Because()
    {
        var model = invoice_model.Compile("""
            concept OpportunityId : Uuid
            concept OpportunityStage : Enum
              newLead
              inProgress
              won
            module Sales
              feature Opportunities
                slice StateChange CaptureOpportunity
                  command CaptureOpportunity
                    opportunityId OpportunityId generated identifier
                    stage OpportunityStage
                    produces OpportunityCaptured
                      for opportunityId
                      stage = stage
                  event OpportunityCaptured
                    stage OpportunityStage
                slice StateView Pipeline
                  readmodel Opportunity
                    opportunityId OpportunityId
                    title String
                    stage OpportunityStage
                  query AllOpportunities => Opportunity[]
            """);
        var profile = CratisRendering.CreateProfile("Sales", new("Sales", "Sales"));
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, new("Sales", "Sales"));
        var result = StageSceneDocument.Render("""
            {"toolbar":{"slice":"Sales.Opportunities.CaptureOpportunity"},"screens":[{"name":"Pipeline","slotContent":{"content":[{"componentName":"core:table","properties":{"typeName":"Sales.Opportunities.Pipeline.Opportunity"},"slots":{"columns":[{"componentName":"core:column","properties":{"property":"stage","label":"Stage"}},{"componentName":"core:column","properties":{"property":"title","label":"Title"}}]}}]}}]}
            """, context);
        var document = JsonNode.Parse(result)!;
        _columns = document["screens"]![0]!["slotContent"]!["content"]![0]!["slots"]!["columns"]!.AsArray();
        _dialogOptions = JsonNode.Parse(document["commandSchemas"]!["CaptureOpportunity"]!.GetValue<string>())!["properties"]!["stage"]!["enum"]!.AsArray();
    }

    [Fact] void should_carry_the_members_in_declared_order() => _columns[0]!["properties"]!["options"]!.AsArray().Select(option => option!.GetValue<string>()).ShouldContainOnly("newLead", "inProgress", "won");
    [Fact] void should_preserve_enumeration_options_for_the_toolbar_dialog() => _dialogOptions.Select(option => option!.GetValue<string>()).ShouldContainOnly("newLead", "inProgress", "won");
    [Fact] void should_not_turn_plain_values_into_enumerations() => _columns[1]!["properties"]!.AsObject().ContainsKey("options").ShouldBeFalse();
}
