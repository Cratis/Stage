// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_event_target_reducers;

public class and_the_reducer_name_collides : Specification
{
    [Theory]
    [InlineData("SamePublicValue")]
    [InlineData("PublicEventCannotBeRetracted")]
    public async Task should_refuse_a_reserved_generated_member(string name)
    {
        var loaded = await when_rendering_a_pure_reducer.Load(and_a_reducer_publishes_a_public_event.Declaration + "\n" +
            and_a_reducer_publishes_a_public_event.Reducer.Replace("OrderPublisher", name, StringComparison.Ordinal));
        var plan = when_rendering_a_pure_reducer.Plan(loaded);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
    }
}
#endif
