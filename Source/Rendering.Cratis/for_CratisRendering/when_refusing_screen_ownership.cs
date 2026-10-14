// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_refusing_screen_ownership : given.a_partitioned_screen_plan
{
    void Because()
    {
        var node = JsonNode.Parse(Partitions.Bytes.AsSpan())!;
        node["Entries"]![0]!["Owner"] = Slice("ProjectLookup").Id.ToString();
        var input = ArtifactRenderInput.Create(ScreenPartitionsInput.Name, Partitions.Version, [.. Encoding.UTF8.GetBytes(CanonicalSceneJson.Write(node))]);
        _result = CratisRendering.PlanFrom(_loaded, new([PlanSelectionEntry.Feature("Projects", "Registration")]), _options with { Profile = WithInput(input) });
    }

    [Fact] void should_refuse_an_existing_slice_that_does_not_declare_the_screen() => _result.Success.ShouldBeFalse();
    [Fact] void should_emit_no_partial_artifacts() => _result.Artifacts.ShouldBeEmpty();
    [Fact] void should_explain_the_declaration_mismatch() => _result.Diagnostics.Single().Message.ShouldContain("does not declare the screen in the compilation");
}
