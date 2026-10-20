// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Layouts;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Xunit;

namespace Cratis.Stage.Api.for_SceneSynthesizer;

public class when_synthesizing_on_an_authored_layout : given.a_scene_model
{
    Layout _layout = null!;
    SceneApplication _semantic = null!;
    SceneApplication _legacy = null!;

    void Establish()
    {
        // An authored Application layout must not be mistaken for the old synthesized shell.
        _layout = new("Application", [new Slot("content")]);
        _scene = _scene with { Layouts = [_layout] };
    }

    void Because()
    {
        _semantic = SceneSynthesizer.Synthesize(_scene, _semanticModel);
        _legacy = SceneSynthesizer.Synthesize(_scene, _eventModel);
    }

    [Fact] void should_preserve_the_authored_layout() => _semantic.Layouts.Single().ShouldEqual(_layout);
    [Fact] void should_place_every_synthesized_screen_on_the_authored_layout() => _semantic.Screens.All(screen => screen.Layout == _layout.Name).ShouldBeTrue();
    [Fact] void should_preserve_the_authored_layout_on_the_event_model_path() => _legacy.Layouts.Single().ShouldEqual(_layout);
    [Fact] void should_place_every_event_model_screen_on_the_authored_layout() => _legacy.Screens.All(screen => screen.Layout == _layout.Name).ShouldBeTrue();
}
