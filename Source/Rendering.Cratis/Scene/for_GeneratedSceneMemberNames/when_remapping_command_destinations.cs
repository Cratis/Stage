// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Scene.for_GeneratedSceneMemberNames;

public class when_remapping_command_destinations : Specification
{
    JsonNode _document = null!;

    void Because()
    {
        _document = JsonNode.Parse("""
            {"command":"Text","arguments":[{"name":"text.value","binding":{"path":"selection.text"}}],
             "fields":[{"name":"text"},{"name":"identity","sourceProperty":"text"},{"name":"detail","sourceProperty":"text.value"}],
             "onSuccess":[{"command":"Other","arguments":[{"name":"text","binding":{"path":"selection.text"}}]}]}
            """)!;
        GeneratedSceneMemberNames.Arguments(_document, new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
        {
            ["Text"] = new Dictionary<string, string>(StringComparer.Ordinal) { ["text"] = "text_" }
        });
        GeneratedSceneMemberNames.Fields(_document["fields"], new Dictionary<string, string>(StringComparer.Ordinal) { ["text"] = "text_" });
    }

    [Fact] void should_not_invent_a_source_alias_for_a_renamed_destination()
    {
        _document["fields"]![0]!["name"]!.GetValue<string>().ShouldEqual("text_");
        Assert.Null(_document["fields"]![0]!["sourceProperty"]);
    }

    [Fact] void should_escape_an_explicit_command_member_alias() => _document["fields"]![1]!["sourceProperty"]!.GetValue<string>().ShouldEqual("text_");
    [Fact] void should_escape_only_the_root_of_a_nested_command_member_alias() => _document["fields"]![2]!["sourceProperty"]!.GetValue<string>().ShouldEqual("text_.value");
    [Fact] void should_escape_only_the_command_input_root() => _document["arguments"]![0]!["name"]!.GetValue<string>().ShouldEqual("text_.value");
    [Fact] void should_preserve_the_read_model_source_binding() => _document["arguments"]![0]!["binding"]!["path"]!.GetValue<string>().ShouldEqual("selection.text");
    [Fact] void should_preserve_another_commands_input() => _document["onSuccess"]![0]!["arguments"]![0]!["name"]!.GetValue<string>().ShouldEqual("text");
}
