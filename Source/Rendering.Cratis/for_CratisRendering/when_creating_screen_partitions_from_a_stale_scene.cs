// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_creating_screen_partitions_from_a_stale_scene : given.a_partitioned_screen_plan
{
    Exception? _refusal;

    void Because()
    {
        var scene = _legacy.Inputs.Single(input => input.Name == "cratis-scaffold:text:scene.json");
        var node = JsonNode.Parse(scene.Bytes.AsSpan())!;
        var screen = node["screens"]![0]!;
        var element = screen["slotContent"]!.AsObject().GetAt(0).Value!.AsArray()[0]!;
        element["properties"]!["stalePayload"] = "Different content, same screen name and hierarchy";
        var staleScene = ArtifactRenderInput.Create(scene.Name, scene.Version, [.. Encoding.UTF8.GetBytes(CanonicalSceneJson.Write(node))]);
        var profile = ArtifactRenderProfile.Create(_legacy.Target, _legacy.TargetVersion, _legacy.Renderer, _legacy.RendererVersion, [.. _legacy.Inputs.Where(input => input != scene), staleScene]);
        _refusal = Catch.Exception(() => CratisRendering.WithScreenPartitions(profile, _compilation));
    }

    [Fact] void should_refuse_before_a_partitioned_profile_is_created() => _refusal.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_explain_the_stale_payload() => _refusal!.Message.ShouldContain("stale or mismatched payload");
}
