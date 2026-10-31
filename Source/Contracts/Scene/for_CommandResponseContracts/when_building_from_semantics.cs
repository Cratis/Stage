// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_CommandResponseContracts;

public class when_building_from_semantics : Specification
{
    JsonObject _contracts = null!;
    readonly List<string> _members = [];

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("responses"), "responses", "Responses.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var scene = JsonNode.Parse("""
            {"screens":[{"name":"Details","slotContent":{"content":[{"componentName":"core:data","properties":{"query":"ProjectById","by":"projectId"}}]}}]}
            """)!.AsObject();
        _contracts = new CommandResponseContracts(compilation.Value!.Model.Application).From(
            scene,
            name =>
            {
                _members.Add($"response:{name}");
                return name == "registered_id" ? "registeredId" : name;
            },
            property =>
            {
                _members.Add($"input:{property.Name}");
                return property.Name;
            });
    }

    [Fact] void should_interleave_response_and_input_naming_per_command() => Assert.Equal(new[] { "input:label", "response:registered_id", "input:projectId", "input:name" }, _members);

    [Fact] void should_describe_a_scalar_response() => _contracts["commands"]!["Register"]!["type"]!["kind"]!.GetValue<string>().ShouldEqual("string");
    [Fact] void should_describe_a_void_response_explicitly() => _contracts["commands"]!["Rename"]!["kind"]!.GetValue<string>().ShouldEqual("void");
    [Fact] void should_use_the_transport_name_of_a_record_field() => _contracts["commands"]!["RegisterRecord"]!["type"]!["properties"]!["registeredId"]!["kind"]!.GetValue<string>().ShouldEqual("string");
    [Fact] void should_not_describe_generated_properties_as_caller_inputs() => _contracts["commandInputs"]!["Register"]!.AsObject().ContainsKey("projectId").ShouldBeFalse();
    [Fact] void should_describe_the_target_command_input() => _contracts["commandInputs"]!["Rename"]!["projectId"]!["kind"]!.GetValue<string>().ShouldEqual("string");
    [Fact] void should_describe_the_typed_destination_query_parameter() => _contracts["screenInputs"]!["Details"]!["projectId"]!["kind"]!.GetValue<string>().ShouldEqual("string");

    const string Source = """
        concept ProjectId : Uuid
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                projectId ProjectId generated identifier
                label String
                returns projectId
            slice StateChange RegisterRecord
              command RegisterRecord
                projectId ProjectId generated identifier
                returns
                  registered_id = projectId
            slice StateChange Rename
              command Rename
                projectId ProjectId
                name String
            slice StateView ProjectLookup
              readmodel Project
                projectId ProjectId
              query ProjectById => Project?
                by projectId ProjectId
        """;
}
