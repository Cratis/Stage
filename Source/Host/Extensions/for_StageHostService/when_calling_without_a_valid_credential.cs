// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Extensions.Protocol;
using Cratis.Stage.Host.Extensions.given;
using Grpc.Core;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_StageHostService;

public class when_calling_without_a_valid_credential : an_extension_protocol
{
    StatusCode _withoutCredential;
    StatusCode _withAnotherCredential;
    StatusCode _beforeTheHandshake;
    StatusCode _afterTheHandshake;

    async Task Establish()
    {
        await StartBackend();
        await StartStage();
        _backend.Description.Settings.Add(new SettingDeclaration { Name = "channel" });
    }

    async Task Because()
    {
        _withoutCredential = await Code(HostClient(null));
        _withAnotherCredential = await Code(HostClient("not-a-credential"));

        // The credential exists once issued, but the handshake that gives the extension its description has not completed.
        _beforeTheHandshake = await Code(HostClient(_sessions.TokenFor(System)));

        await _connections.Connect(System, CancellationToken.None);
        _afterTheHandshake = await Code(HostClient(_sessions.TokenFor(System)));
    }

    static async Task<StatusCode> Code(StageHost.StageHostClient client)
    {
        try
        {
            await client.GetSettingAsync(new GetSettingRequest { Name = "channel" });
            return StatusCode.OK;
        }
        catch (RpcException exception)
        {
            return exception.StatusCode;
        }
    }

    [Fact] void should_refuse_a_call_without_a_credential() => _withoutCredential.ShouldEqual(StatusCode.Unauthenticated);
    [Fact] void should_refuse_a_credential_that_was_not_issued() => _withAnotherCredential.ShouldEqual(StatusCode.Unauthenticated);
    [Fact] void should_refuse_a_call_before_the_handshake_completes() => _beforeTheHandshake.ShouldEqual(StatusCode.FailedPrecondition);
    [Fact] void should_serve_the_call_after_the_handshake() => _afterTheHandshake.ShouldEqual(StatusCode.OK);
}
