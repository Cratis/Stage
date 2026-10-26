// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Scene.for_AmbiguousSceneCommands;

public class when_traversing_unexpected_shapes : Specification
{
    string[] _references = null!;

    void Because() => _references = [.. AmbiguousSceneCommands.References(JsonNode.Parse("""
        {
          "behaviors":[false,[],{"bindings":[1,[],{"actions":[false,[],{"kind":"ExecuteCommand","command":"App","onSuccess":[false,[],{"kind":"ExecuteCommand","command":"Continuation"}]}]}]}],
          "screens":[false,[],{
            "forms":[false,[],{"forCommand":"Form","behaviors":false}],
            "slotContent":{"main":[false,[],
              {"componentName":"core:action","properties":false},
              {"componentName":"core:action","properties":[]},
              {"componentName":"core:action","properties":{"command":"Action","alternatives":[false,[],{"command":"Guarded"}],"otherwise":false}},
              {"componentName":"core:action","properties":{"otherwise":[]}},
              {"componentName":"core:stack","slots":false,"children":[false,[],{"componentName":"core:commandForm","properties":{"command":"Child"}}]},
              {"componentName":"core:stack","slots":{"children":false},"children":false}
            ]},
            "contributions":[false,[],{"content":false}]
          }],
          "layouts":[{"content":false}],
          "screenTemplates":false,
          "dialogTemplates":[{"content":{"main":false}}]
        }
        """))];

    [Fact] void should_ignore_nonobject_nodes_without_losing_valid_references() => _references.ShouldContainOnly("App", "Continuation", "Form", "Action", "Guarded", "Child");
    [Fact] void should_ignore_a_scalar_document() => AmbiguousSceneCommands.References(JsonValue.Create(false)).ShouldBeEmpty();
    [Fact] void should_ignore_an_array_document() => AmbiguousSceneCommands.References(new JsonArray()).ShouldBeEmpty();
}
