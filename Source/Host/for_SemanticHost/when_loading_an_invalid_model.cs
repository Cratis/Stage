// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost;

public class when_loading_an_invalid_model : given.a_model_path
{
    readonly List<StageUnsupportedIssue> _issues = [];
    LoadedSemanticModel? _loaded;
    SceneApplication? _scene;

    Task Establish() => File.WriteAllTextAsync(_path, "This is not Screenplay source");

    async Task Because() => (_loaded, _scene) = await SemanticHost.LoadModel(_path, _issues);

    [Fact] void should_not_load_a_model() => _loaded.ShouldBeNull();
    [Fact] void should_not_serve_a_scene() => _scene.ShouldBeNull();
    [Fact] void should_report_the_model_diagnostics() => _issues.ShouldNotBeEmpty();
    [Fact] void should_report_plan_issues() => _issues.TrueForAll(issue => issue.Capability == "Plan" && issue.Artifact == "model" && issue.Details.Length > 0).ShouldBeTrue();
}
