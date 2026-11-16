// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.given;
using Cratis.Stage.Host.Invocations;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_GrpcAbilityBackend;

public class when_the_backend_is_down : an_extension_protocol
{
    AbilityResult _beforeTheHandshake = null!;
    AbilityResult _inFlight = null!;

    async Task Establish()
    {
        await StartBackend();
        await StartStage();
    }

    async Task Because()
    {
        // Connected, then the backend goes away: the call may have been sent, so it is a failure that may have reached the system.
        await _connections.Connect(System, CancellationToken.None);
        await _backendApp.StopAsync();
        _inFlight = await _abilities.Invoke(Call(), CancellationToken.None);

        // The next attempt does the handshake again and cannot, so it is certain nothing was sent.
        _beforeTheHandshake = await _abilities.Invoke(Call(attempt: 2), CancellationToken.None);
    }

    [Fact] void should_fail_an_attempt_that_may_have_been_sent() => ((AbilityFailed)_inFlight).MayHaveReachedTheSystem.ShouldBeTrue();
    [Fact] void should_answer_the_next_attempt_as_not_sent() => _beforeTheHandshake.ShouldBeOfExactType<AbilityNotSent>();
    [Fact] void should_not_have_sent_the_next_attempt_when_the_handshake_fails() => ((AbilityNotSent)_beforeTheHandshake).Reason.ShouldContain("handshake");
}
