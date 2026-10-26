// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_composing_command_names.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Scene.for_AmbiguousSceneCommands;

public class when_commands_have_distinct_names : a_two_module_request
{
    IReadOnlyList<string> _names = null!;
    string[] _keys = null!;

    void Because()
    {
        var slices = new SemanticApplicationContext(_request, _options).AllSlices;
        var index = new SceneCommandIndex(slices);
        _names = index.AmbiguousNames;
        _keys = [.. slices.SelectMany(slice => slice.Slice.Commands.Select(command => index.ById[command.Id].Key))];
    }

    [Fact] void should_keep_the_bare_authored_names() => _keys.ShouldContainOnly("RegisterProject", "OtherRegisterProject");
    [Fact] void should_find_no_ambiguity() => _names.ShouldBeEmpty();
}
