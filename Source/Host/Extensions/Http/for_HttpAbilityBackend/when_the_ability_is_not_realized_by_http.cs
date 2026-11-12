// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Http.given;
using Cratis.Stage.Host.Invocations;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Http.for_HttpAbilityBackend;

public class when_the_ability_is_not_realized_by_http : an_http_system
{
    AbilityResult _result = null!;

    async Task Establish()
    {
        await StartSystem();
        StartStage(Parsed(BookPickup));
    }

    async Task Because() => _result = await _backend.Invoke(Call("Cancel"), CancellationToken.None);

    [Fact] void should_hand_it_to_the_backend_it_decorates() => _inner.Received(1).Invoke(Arg.Is<AbilityCall>(call => call.Ability.Ability == "Cancel"), Arg.Any<CancellationToken>());
    [Fact] void should_return_what_that_backend_returned() => ((AbilityNotSent)_result).Reason.ShouldEqual("The inner backend was asked.");
    [Fact] void should_not_call_the_system() => _received.Count.ShouldEqual(0);
}
