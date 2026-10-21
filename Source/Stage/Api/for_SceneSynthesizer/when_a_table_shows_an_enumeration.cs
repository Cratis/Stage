// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Xunit;

namespace Cratis.Stage.Api.for_SceneSynthesizer;

public class when_a_table_shows_an_enumeration : given.a_scene_model
{
    ExternalComponent _semanticColumn = null!;
    ExternalComponent _legacyColumn = null!;

    void Because()
    {
        _semanticColumn = StageColumn(SceneSynthesizer.Synthesize(_scene, _semanticModel));
        _legacyColumn = StageColumn(SceneSynthesizer.Synthesize(_scene, _eventModel));
    }

    static ExternalComponent StageColumn(SceneApplication scene) => scene.Screens
        .SelectMany(screen => screen.SlotContent.Values.SelectMany(elements => elements))
        .OfType<ExternalComponent>().Single(element => element.ComponentName == "core:table")
        .Slots["columns"].OfType<ExternalComponent>().Single(column => Equals(column.Properties["property"], "stage"));

    [Fact] void should_carry_the_semantic_enumeration_members() => ((IReadOnlyList<string>)_semanticColumn.Properties["options"]!).ShouldContainOnly("planned", "active");
    [Fact] void should_carry_the_legacy_enumeration_members() => ((IReadOnlyList<string>)_legacyColumn.Properties["options"]!).ShouldContainOnly("planned", "active");
}
