// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Scene.for_AmbiguousSceneCommands;

public class when_a_command_identity_appears_twice : a_register_project_render_request
{
    LocatedSemanticSlice _slice = null!;
    IReadOnlyList<string> _names = null!;

    void Establish() => _slice = new SemanticApplicationContext(_request, _options).AllSlices.Single(slice => slice.Slice.Commands.Length > 0);
    void Because() => _names = new SceneCommandIndex([_slice, _slice]).AmbiguousNames;

    [Fact] void should_not_treat_the_same_command_as_ambiguous() => _names.ShouldBeEmpty();
}
