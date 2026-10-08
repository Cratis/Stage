// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost;

public class when_loading_an_authored_scene : given.a_model_path
{
    readonly List<StageUnsupportedIssue> _issues = [];
    SceneApplication? _scene;

    Task Establish() => File.WriteAllTextAsync(_path, Cratis.Stage.Api.for_SceneSynthesizer.given.a_scene_model.Source + "\n        screen AuthoredOverview\n          title \"Projects\"");

    async Task Because() => (_, _scene) = await SemanticHost.LoadModel(_path, _issues);

    [Fact] void should_report_no_unsupported_issues() => _issues.ShouldBeEmpty();
    [Fact] void should_preserve_the_authored_screen_instead_of_synthesizing_defaults() => _scene!.Screens.Single().Name.ShouldEqual("AuthoredOverview");
}
