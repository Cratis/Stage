// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_inspecting_generation_artifacts
{
    // This probe deliberately does not reconcile the ledger: it retains the renderer output and adapter source before a fix.
    [Fact]
    void should_compile_and_recover_all_three_generation_fixtures()
    {
        var fixtures = generation_round_trips.All.ToArray();
        Assert.Equal(3, fixtures.Length);
        Assert.All(fixtures, fixture => Assert.NotNull(fixture.Result.Comparison));
    }
}
#endif
