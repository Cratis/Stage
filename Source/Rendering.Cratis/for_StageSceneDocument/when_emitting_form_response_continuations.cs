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

public class when_emitting_form_response_continuations : Specification
{
    JsonNode _document = null!;
    JsonNode _form = null!;

    void Because()
    {
        var model = invoice_model.Compile("""
            concept ProjectId : Uuid
            module Projects
              feature Registration
                slice StateChange Register
                  command RegisterProject
                    projectId ProjectId generated identifier
                    title String
                    returns
                      registered_id = projectId
                slice StateView Lookup
                  readmodel Project
                    projectId ProjectId
                  query ProjectById => Project?
                    by projectId ProjectId
            """);
        var profile = CratisRendering.CreateProfile("Projects", new("Projects", "Projects"));
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, new("Projects", "Projects"));
        _document = JsonNode.Parse(StageSceneDocument.Render(
            """
            {"screens":[
              {"name":"Overview","forms":[{"name":"Register","forCommand":"RegisterProject","fields":[],"generationMode":"auto","onSubmit":[{"kind":"navigate","screen":"Details","arguments":[{"name":"projectId","value":{"kind":"commandResponse","path":"registered_id"}}]}]}]},
              {"name":"Details","slotContent":{"content":[{"componentName":"core:data","properties":{"query":"ProjectById","by":"projectId"}}]}}
            ]}
            """,
            context))!;
        _form = _document["screens"]![0]!["slotContent"]!["forms"]![0]!["properties"]!;
    }

    [Fact] void should_preserve_the_continuation_on_the_runtime_form() => _form["onSubmit"]!.AsArray().Count.ShouldEqual(1);
    [Fact] void should_preserve_the_response_source_discriminator() => _form["onSubmit"]![0]!["arguments"]![0]!["value"]!["kind"]!.GetValue<string>().ShouldEqual("commandResponse");
    [Fact] void should_map_the_response_member_to_the_backend_wire_name() => _form["onSubmit"]![0]!["arguments"]![0]!["value"]!["path"]!.GetValue<string>().ShouldEqual("registeredId");
    [Fact] void should_carry_the_matching_response_contract() => _document["responseContracts"]!["commands"]!["RegisterProject"]!["type"]!["properties"]!["registeredId"]!["kind"]!.GetValue<string>().ShouldEqual("string");
    [Fact] void should_carry_the_destination_query_type() => _document["responseContracts"]!["screenInputs"]!["Details"]!["projectId"]!["kind"]!.GetValue<string>().ShouldEqual("string");
    [Fact] void should_keep_generated_values_out_of_the_request() => _document["responseContracts"]!["commandInputs"]!["RegisterProject"]!.AsObject().ContainsKey("projectId").ShouldBeFalse();
}
