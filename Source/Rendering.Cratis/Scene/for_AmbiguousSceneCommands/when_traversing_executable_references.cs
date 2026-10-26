// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Scene.for_AmbiguousSceneCommands;

public class when_traversing_executable_references : Specification
{
    string[] _references = null!;

    void Because() => _references = [.. AmbiguousSceneCommands.References(JsonNode.Parse("""
        {
          "behaviors":[{"bindings":[{"actions":[{"kind":"ExecuteCommand","command":"App","onSuccess":[{"kind":"ExecuteCommand","command":"Continuation"}]}]}]}],
          "screens":[{
            "forms":[{"forCommand":"Form"}],
            "slotContent":{"main":[
              {"componentName":"core:action","properties":{"command":"Action","alternatives":[{"command":"Guarded"}],"otherwise":{"command":"Fallback"}}},
              {"componentName":"core:toolbarItem","properties":{"kind":"Action","target":"Toolbar"}},
              {"componentName":"Cratis.Components:commandForm","properties":{"command":"Native"}},
              {"componentName":"Stage:commandForm","properties":{"command":"StageNative"}},
              {"componentName":"Stage:confirmDialog","properties":{"decisions":[false,[],{"command":"Decision"}]}}
            ]},
            "contributions":[{"content":{"componentName":"core:stack","children":[{"componentName":"core:commandForm","properties":{"command":"Contributed"}}]}}]
          }],
          "layouts":[{"content":{"main":[{"componentName":"core:grid","slots":{"children":[{"componentName":"core:action","properties":{"command":"Layout"}}]}}]}}],
          "screenTemplates":[{"content":{"main":[{"componentName":"core:action","properties":{"command":"Template"}}]}}],
          "dialogTemplates":[{"content":{"main":[{"componentName":"core:action","properties":{"command":"Dialog"}}]}}]
        }
        """))];

    [Fact] void should_visit_only_typed_executable_slots() => _references.ShouldContainOnly("App", "Continuation", "Form", "Action", "Guarded", "Fallback", "Toolbar", "Native", "StageNative", "Decision", "Contributed", "Layout", "Template", "Dialog");
}
