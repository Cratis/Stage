// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Scene.for_GeneratedSceneMemberNames;

public class when_renaming_subject_inputs : Specification
{
    JsonNode _document = null!;

    void Because()
    {
        _document = JsonNode.Parse("""
            {"command":"Register","actionSubject":"item","subjectInputs":{"orderId":{"subject":"item.id"}}}
            """)!;
        GeneratedSceneMemberNames.Arguments(_document, new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
        {
            ["Register"] = new Dictionary<string, string>(StringComparer.Ordinal) { ["orderId"] = "orderIdentifier" }
        });
    }

    [Fact] void should_rename_the_command_input() => Assert.NotNull(_document["subjectInputs"]!["orderIdentifier"]);
    [Fact] void should_drop_the_authored_input_name() => Assert.Null(_document["subjectInputs"]!["orderId"]);
    [Fact] void should_keep_the_read_model_source_path() => _document["subjectInputs"]!["orderIdentifier"]!["subject"]!.GetValue<string>().ShouldEqual("item.id");
}
