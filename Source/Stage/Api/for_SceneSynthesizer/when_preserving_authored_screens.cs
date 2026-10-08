// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Xunit;

namespace Cratis.Stage.Api.for_SceneSynthesizer;

public class when_preserving_authored_screens : given.a_scene_model
{
    SceneApplication _authored = null!;
    SceneApplication _result = null!;

    void Establish() => _authored = _scene with
    {
        Screens = [new("AuthoredOverview", _scene.Layouts[0].Name, new Dictionary<string, IReadOnlyList<Cratis.Scene.Model.Elements.SceneElement>>(), [], [])]
    };

    void Because() => _result = SceneSynthesizer.Synthesize(_authored, _semanticModel);

    [Fact] void should_preserve_the_authored_scene_without_synthesizing_defaults() => ReferenceEquals(_result, _authored).ShouldBeTrue();
}
