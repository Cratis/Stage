// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_a_composed_application : a_composed_register_project_application
{
    [Fact] void should_plan_without_errors() => _plan.Diagnostics.ShouldBeEmpty();

    [Fact] void should_carry_the_composed_scene() => Artifact(SceneCompositionInput.RelativePath).ShouldNotBeNull();

    [Fact] void should_bind_the_composed_scene_to_the_generated_proxies() => Artifact(SceneBindingsRenderer.RelativePath).ShouldNotBeNull();

    [Fact] void should_register_the_command_under_its_semantic_name() =>
        Text(Artifact(SceneBindingsRenderer.RelativePath)!).ShouldContain("registerCommands({RegisterProject});");

    [Fact] void should_register_the_keyed_query_under_its_semantic_name() =>
        Text(Artifact(SceneBindingsRenderer.RelativePath)!).ShouldContain("registerQueries({ProjectById});");

    /// <summary>
    /// A query or command route is a runtime fact owned by Arc. A generated application that embedded one would
    /// drift from the endpoints Arc actually registers, silently, the first time either convention changed.
    /// </summary>
    /// <remarks>
    /// The dev-server proxy in <c language="json">.frontend/vite.config.ts</c> is deliberately excluded: its paths forward the
    /// browser to the backend origin during development and are not how anything binds to a command or query.
    /// </remarks>
    [Fact] void should_never_embed_an_http_route_in_emitted_application_typescript() =>
        _plan.Artifacts
            .Where(_ => _.RelativePath.EndsWith(".ts", StringComparison.Ordinal) || _.RelativePath.EndsWith(".tsx", StringComparison.Ordinal))
            .Where(_ => !_.RelativePath.StartsWith(".frontend/", StringComparison.Ordinal))
            .Any(_ => Regex.IsMatch(Text(_), @"['""]/[a-z]", RegexOptions.IgnoreCase))
            .ShouldBeFalse();
}
