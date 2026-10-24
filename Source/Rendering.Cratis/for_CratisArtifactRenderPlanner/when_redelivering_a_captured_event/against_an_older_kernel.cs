// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.EventSequences;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_redelivering_a_captured_event;

public class against_an_older_kernel : given.a_captured_delivery
{
    void Because() => Render();

    [Fact]
    public async Task should_refuse_the_effects_instead_of_appending_without_keys()
    {
        _log.SupportsIdempotentAppends = false;
        await Assert.ThrowsAsync<IdempotentAppendsNotSupported>(() => Deliver(Fact(0)));
        _log.Committed.ShouldBeEmpty();
        _log.Attempts.ShouldBeEmpty();
    }
}
#endif
