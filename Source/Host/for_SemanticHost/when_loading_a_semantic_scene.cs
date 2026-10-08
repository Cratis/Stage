// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost;

public class when_loading_a_semantic_scene : given.a_model_path
{
    readonly List<StageUnsupportedIssue> _issues = [];
    LoadedSemanticModel? _loaded;
    SceneApplication? _scene;
    SceneApplication _legacy = null!;

    async Task Establish()
    {
        await File.WriteAllTextAsync(_path, Cratis.Stage.Api.for_SceneSynthesizer.given.a_scene_model.Source);
        var legacy = await EventModelLoader.LoadStageApplicationFromPathAsync(_path);
        _legacy = SceneSynthesizer.Synthesize(legacy.Scene, legacy.EventModel);
    }

    async Task Because() => (_loaded, _scene) = await SemanticHost.LoadModel(_path, _issues);

    [Fact] void should_load_the_semantic_model() => _loaded.ShouldNotBeNull();
    [Fact] void should_report_no_unsupported_issues() => _issues.ShouldBeEmpty();
    [Fact] void should_build_the_same_scene_as_the_legacy_host() => JsonSerializer.Serialize(_scene).ShouldEqual(JsonSerializer.Serialize(_legacy));
}
