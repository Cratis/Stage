// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_refusing_screen_partitions : given.a_partitioned_screen_plan
{
    [Theory]
    [InlineData("stale model")]
    [InlineData("stale scene")]
    [InlineData("unknown owner")]
    [InlineData("duplicate screen")]
    [InlineData("missing screen")]
    [InlineData("different payload")]
    [InlineData("malformed payload")]
    [InlineData("malformed json")]
    [InlineData("duplicate input")]
    [InlineData("unsupported version")]
    public void should_refuse_without_partial_output(string reason)
    {
        var node = JsonNode.Parse(Partitions.Bytes.AsSpan())!.AsObject();
        var entries = node["Entries"]!.AsArray();
        switch (reason)
        {
            case "stale model": node["ModelRevision"] = "stale"; break;
            case "stale scene": node["SceneSha256"] = "stale"; break;
            case "unknown owner": entries[0]!["Owner"] = _loaded.Model.Application.Id.ToString(); break;
            case "duplicate screen": entries[1]!["Index"] = 0; break;
            case "missing screen": entries.RemoveAt(0); break;
            case "different payload": entries[0]!["Screen"]!["name"] = "Unrelated"; break;
            case "malformed payload": entries[0]!["Screen"]!["contributions"] = null; break;
        }

        var text = reason == "malformed json" ? "{" : CanonicalSceneJson.Write(node);
        var input = ArtifactRenderInput.Create(ScreenPartitionsInput.Name, reason == "unsupported version" ? "999" : Partitions.Version, [.. Encoding.UTF8.GetBytes(text)]);
        var profile = WithInput(input);
        if (reason == "duplicate input")
        {
            profile = ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs, ArtifactRenderInput.Create(input.Name, "2", input.Bytes)]);
        }

        var result = CratisRendering.PlanFrom(_loaded, new([PlanSelectionEntry.Feature("Projects", "Registration")]), _options with { Profile = profile });
        result.Success.ShouldBeFalse();
        result.Artifacts.ShouldBeEmpty();
        result.Diagnostics.Single().Code.ShouldEqual("STAGE-CRATIS-001");
        result.Diagnostics.Single().Message.ShouldContain("Screen partitions");
    }
}
