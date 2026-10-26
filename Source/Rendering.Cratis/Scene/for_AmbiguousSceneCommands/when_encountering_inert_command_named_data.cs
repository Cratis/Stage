// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Scene.for_AmbiguousSceneCommands;

public class when_encountering_inert_command_named_data : Specification
{
    string[] _references = null!;

    void Because() => _references = [.. AmbiguousSceneCommands.References(JsonNode.Parse("""
        {
          "command":"metadata",
          "structure":{"modules":[{"command":"structure"}]},
          "screens":[{
            "metadata":{"command":"metadata"},
            "state":{"forCommand":"state"},
            "slotContent":{"main":[
              {"componentName":"custom:row","properties":{"command":"custom","forCommand":"custom"}},
              {"componentName":"core:table","properties":{"data":[{"command":"row","forCommand":"row","componentName":"core:action","properties":{"command":"nested-row"}}]}},
              {"componentName":"core:action","properties":{"arguments":[{"name":"command","source":"state.command"}]}},
              {"componentName":"core:toolbarItem","properties":{"kind":"OpenDialog","target":"Dialog"}},
              {"componentName":"core:commandForm","properties":{"command":"Actual","initialValues":{"command":"value"}}}
            ]}
          }]
        }
        """))];

    [Fact] void should_not_interpret_rows_state_arguments_or_custom_properties_as_executables() => _references.ShouldContainOnly("Actual");
}
