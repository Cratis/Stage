// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.given;
using Cratis.Stage.Host.Invocations;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_GrpcAbilityBackend;

public class when_the_connection_is_cut_off_during_an_attempt : an_extension_protocol
{
    AbilityResult _cutOff = null!;
    AbilityResult _next = null!;

    async Task Establish()
    {
        await StartBackend();
        await StartStage();
    }

    async Task Because()
    {
        // The stream is reset on the wire, which gRPC reports as Internal rather than Unavailable: the connection is still lost.
        _backend.CutOffNextInvoke = true;
        _cutOff = await _abilities.Invoke(Call(), CancellationToken.None);
        _next = await _abilities.Invoke(Call(attempt: 2), CancellationToken.None);
    }

    [Fact] void should_fail_the_attempt_that_was_cut_off() => _cutOff.ShouldBeOfExactType<AbilityFailed>();
    [Fact] void should_count_the_cut_off_attempt_as_one_that_may_have_been_sent() => ((AbilityFailed)_cutOff).MayHaveReachedTheSystem.ShouldBeTrue();
    [Fact] void should_do_the_handshake_again_for_the_next_attempt() => _backend.Handshakes.ShouldEqual(2);
    [Fact] void should_perform_the_next_attempt() => _next.ShouldBeOfExactType<AbilityReturned>();
}
