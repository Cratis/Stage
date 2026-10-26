// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_composing_command_names.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Scene.for_AmbiguousSceneCommands;

public class when_two_names_repeat_in_two_slices : a_two_module_request
{
    string[] _keys = null!;

    void Because()
    {
        var originals = new SemanticApplicationContext(_request, _options).AllSlices.Where(slice => slice.Slice.Commands.Length > 0).ToArray();
        var first = originals[0].Slice.Commands.Single();
        var second = originals[1].Slice.Commands.Single();
        LocatedSemanticSlice[] slices =
        [
            originals[0] with { Slice = originals[0].Slice with { Commands = [first with { Name = "A" }, second with { Name = "B" }] } },
            originals[1] with { Slice = originals[1].Slice with { Commands = [second with { Name = "A" }, first with { Name = "B" }] } }
        ];

        // Use distinct stable identities for all four occurrences.
        slices[1] = slices[1] with { Slice = slices[1].Slice with { Commands = [second with { Id = originals[0].Slice.Id, Name = "A" }, first with { Id = originals[1].Slice.Id, Name = "B" }] } };
        var index = new SceneCommandIndex(slices);
        _keys = [.. slices.SelectMany(slice => slice.Slice.Commands.Select(command => index.ById[command.Id].Key))];
    }

    [Fact] void should_retain_four_command_keys() => _keys.Distinct(StringComparer.Ordinal).Count().ShouldEqual(4);
    [Fact] void should_include_the_command_in_each_structural_key() => _keys.All(key => key.StartsWith("$command$", StringComparison.Ordinal)).ShouldBeTrue();
}
