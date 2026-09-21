// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Scene.for_CanonicalSceneJson;

/// <summary>
/// The payload is a planned artifact, so equal input has to produce equal bytes.
/// </summary>
public class when_serializing_the_same_scene_twice : Specification
{
    SceneApplication _scene = null!;
    string _first = null!;
    string _second = null!;

    void Establish()
    {
        var layout = DefaultLayout.Create();
        _scene = new SceneApplication([], [], [layout], [], [], []);
    }

    void Because()
    {
        _first = CanonicalSceneJson.Serialize(_scene);
        _second = CanonicalSceneJson.Serialize(_scene);
    }

    [Fact] void should_produce_identical_text() => _second.ShouldEqual(_first);

    [Fact] void should_order_every_property_name_ordinally() =>
        _first.IndexOf("\"dialogTemplates\"", StringComparison.Ordinal).ShouldBeLessThan(_first.IndexOf("\"layouts\"", StringComparison.Ordinal));

    [Fact] void should_emit_without_indentation() => _first.ShouldNotContain("\n");
}
