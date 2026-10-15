// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Profiles;
using Cratis.Scene.Model.SampleData;
using Cratis.Specifications;
using Xunit;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneLayouts = Cratis.Scene.Model.Layouts;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Contracts.Scene.for_SceneApplication;

public class when_serving_a_running_application : Specification
{
    SceneApplication _scene = null!;
    SceneApplication _running = null!;

    void Establish()
    {
        var layout = DefaultLayout.Create();
        var screen = new SceneScreens.Screen("Orders", layout.Name, new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal), [], []);
        var profile = new UiProfile("Web", "web", ["core"], SampleData: new SampleDataSelection([], [], "nordic"));
        _scene = new([profile], [], [layout], [], [], [screen])
        {
            SampleDataPacks = [new("nordic", "Nordic", "nb-NO", "user", [])],
            BlueprintSampleDataPacks = ["crm"]
        };
    }

    void Because() => _running = _scene.ForRuntime();

    [Fact] void should_carry_no_sample_data_packs() => _running.SampleDataPacks.ShouldBeEmpty();
    [Fact] void should_carry_no_blueprint_sample_data_packs() => _running.BlueprintSampleDataPacks.ShouldBeEmpty();
    [Fact] void should_carry_no_sample_data_selection_in_a_profile() => _running.UiProfiles.Single().SampleData.ShouldBeNull();
    [Fact] void should_keep_the_rest_of_the_profile() => _running.UiProfiles.Single().Packages.ShouldContainOnly(["core"]);
    [Fact] void should_keep_the_screens() => _running.Screens.Select(screen => screen.Name).ShouldContainOnly(["Orders"]);
    [Fact] void should_leave_the_authored_application_as_it_was() => _scene.SampleDataPacks.Count.ShouldEqual(1);
}
