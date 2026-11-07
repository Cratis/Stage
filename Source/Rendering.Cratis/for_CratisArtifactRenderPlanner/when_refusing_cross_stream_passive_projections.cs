// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;
using static Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_passive_read_models;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_cross_stream_passive_projections : Specification
{
    [Theory]
    [InlineData("payload-key")]
    [InlineData("join")]
    [InlineData("children")]
    [InlineData("nested")]
    public void should_refuse_incomplete_stream_local_replay_without_artifacts(string shape)
    {
        var source = Source.Replace("        reads Balance by id\n", string.Empty, StringComparison.Ordinal);
        var clause = string.Empty;
        switch (shape)
        {
            case "payload-key":
                source = source.Replace("        produces Adjusted\n          for id\n", "        produces Adjusted\n          for id\n          id = id\n", StringComparison.Ordinal)
                    .Replace("      event Adjusted\n", "      event Adjusted\n        id String\n", StringComparison.Ordinal)
                    .Replace("        from Adjusted\n          id = $eventSourceId", "        from Adjusted key id\n          id = id", StringComparison.Ordinal);
                break;
            case "join":
                clause = "        join details on id\n          with Adjusted\n            quantity = quantity\n";
                break;
            case "children":
                source = "type Line\n  id String\n  quantity Int\n" + source.Replace("      readmodel Balance\n", "      readmodel Balance\n        lines Line[]\n", StringComparison.Ordinal)
                    .Replace("      event Adjusted\n", "      event Adjusted\n        id String\n        parentId String\n", StringComparison.Ordinal)
                    .Replace("        produces Adjusted\n          for id\n", "        produces Adjusted\n          for id\n          id = id\n          parentId = id\n", StringComparison.Ordinal);
                clause = "        children lines identified by id\n          from Adjusted key id\n            parent parentId\n            quantity = quantity\n";
                break;
            case "nested":
                source = "type Details\n  quantity Int\n" + source.Replace("      readmodel Balance\n", "      readmodel Balance\n        details Details?\n", StringComparison.Ordinal);
                clause = "        nested details\n          from Adjusted\n            quantity = quantity\n";
                break;
        }
        source = source.Replace("        remove with Removed", clause + "        remove with Removed", StringComparison.Ordinal);
        var compilation = Compile(source);
        var plan = Plan(compilation);
        plan.Success.ShouldBeFalse();
        plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-061");
        plan.Artifacts.ShouldBeEmpty();
    }
}
#endif
