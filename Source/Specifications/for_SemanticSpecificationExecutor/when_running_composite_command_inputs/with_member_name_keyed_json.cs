// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_command_inputs;

public class with_member_name_keyed_json : given.a_composite_input
{
    JsonElement _json;

    void Establish() => Prepare("Node[] optional", "[{\"count\":7,\"next\":{\"count\":8}}]");
    void Because()
    {
        var input = _specification.When!.Values[^1];
        var type = _command.Properties.Single(property => property.Id == input.TargetProperty).Type;
        _json = JsonSerializer.Deserialize<JsonElement>(SemanticRunContext.Canonical(input.Value, type, new SemanticApplicationContext(_plan)));
    }

    [Fact] void should_use_declared_member_names() => _json[0].GetProperty("count").GetInt32().ShouldEqual(7);
    [Fact] void should_preserve_nested_values() => _json[0].GetProperty("next").GetProperty("count").GetInt32().ShouldEqual(8);
    [Fact] void should_not_serialize_semantic_identity_metadata() => _json.GetRawText().ShouldEqual("[{\"count\":7,\"next\":{\"count\":8}}]");
}
