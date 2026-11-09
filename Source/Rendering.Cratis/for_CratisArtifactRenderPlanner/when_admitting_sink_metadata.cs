// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using System.Text.Json.Nodes;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_named_sinks.given;
using Cratis.Stage.Rendering.Cratis.Realization;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_admitting_sink_metadata : Specification
{
    [Theory]
    [InlineData("malformed")]
    [InlineData("unknown-read-model")]
    [InlineData("unknown-graph")]
    [InlineData("undeclared-sink")]
    [InlineData("kind-mismatch")]
    [InlineData("unknown-kind")]
    [InlineData("duplicate-input")]
    [InlineData("wrong-version")]
    [InlineData("noncanonical")]
    public void should_fail_closed_without_artifacts(string fault)
    {
        var compilation = a_model_with_named_sinks.Compile(a_model_with_named_sinks.Sinks + a_model_with_named_sinks.Source);
        var profile = a_model_with_named_sinks.Profile(compilation);
        var input = profile.Inputs.Single(candidate => candidate.Name == SinksInput.Name);
        var json = Mutate(JsonNode.Parse(input.Bytes.AsSpan())!, fault);
        var bytes = fault == "malformed" ? "{"u8.ToArray() : Encoding.UTF8.GetBytes(json.ToJsonString(new() { WriteIndented = false }));
        if (fault == "noncanonical") bytes = [32, .. input.Bytes];
        var replacement = ArtifactRenderInput.Create(input.Name, fault == "wrong-version" ? "2" : input.Version, [.. bytes]);
        var inputs = profile.Inputs.Where(candidate => candidate.Name != input.Name).Append(replacement);
        if (fault == "duplicate-input") inputs = inputs.Append(ArtifactRenderInput.Create(input.Name, "2", input.Bytes));
        var plan = new CratisArtifactRenderPlanner().Plan(new(compilation.Model, Screenplay.Semantics.Execution.SemanticExecutionPlan.Compile(compilation.Model, []).Plan!, ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. inputs]), new(ArtifactRenderScopeKind.Application, compilation.Model.Application.Id)));
        plan.Success.ShouldBeFalse();
        plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-CRATIS-001");
        plan.Artifacts.ShouldBeEmpty();
    }

    static JsonNode Mutate(JsonNode json, string fault)
    {
        var readModels = json["readModels"]!.AsObject();
        var graphs = json["graphs"]!.AsObject();
        switch (fault)
        {
            case "unknown-read-model":
                readModels["unknown"] = "Reporting";
                break;
            case "unknown-graph":
                graphs["Unknown"] = "People";
                break;
            case "undeclared-sink":
                graphs["Organization"] = "Elsewhere";
                break;
            case "kind-mismatch":
                graphs["Organization"] = "Reporting";
                break;
            case "unknown-kind":
                json["sinks"]![0]!["kind"] = "tables";
                break;
        }

        return json;
    }
}
#endif
