// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.given;
using Cratis.Stage.Host.Invocations;
using Grpc.Core;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_GrpcAbilityBackend;

public class when_the_backend_answers_an_attempt_with_an_error_status : an_extension_protocol
{
    AbilityResult _answered = null!;
    AbilityResult _next = null!;

    async Task Establish()
    {
        await StartBackend();
        await StartStage();
    }

    async Task Because()
    {
        // The backend itself answers Internal: the connection works, so it is kept.
        _backend.OnInvoke = _ => throw new RpcException(new Status(StatusCode.Internal, "The backend failed."));
        _answered = await _abilities.Invoke(Call(), CancellationToken.None);
        _backend.OnInvoke = _ => new() { Returned = new() };
        _next = await _abilities.Invoke(Call(attempt: 2), CancellationToken.None);
    }

    [Fact] void should_fail_the_attempt() => _answered.ShouldBeOfExactType<AbilityFailed>();
    [Fact] void should_keep_the_connection_for_the_next_attempt() => _backend.Handshakes.ShouldEqual(1);
    [Fact] void should_perform_the_next_attempt() => _next.ShouldBeOfExactType<AbilityReturned>();
}
