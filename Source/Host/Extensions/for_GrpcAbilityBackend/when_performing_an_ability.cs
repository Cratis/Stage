// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Extensions.Protocol;
using Cratis.Stage.Host.Extensions.given;
using Cratis.Stage.Host.Invocations;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_GrpcAbilityBackend;

public class when_performing_an_ability : an_extension_protocol
{
    AbilityResult _result = null!;

    async Task Establish()
    {
        await StartBackend();
        await StartStage();
        _backend.OnInvoke = _ => new InvokeResponse { Returned = new Returned { Outcome = "posted", OutputJson = "{\"ts\":\"1\"}" } };
    }

    async Task Because() => _result = await _abilities.Invoke(Call(input: "{\"text\":\"hi\"}"), CancellationToken.None);

    [Fact] void should_return_the_named_outcome() => ((AbilityReturned)_result).Outcome.ShouldEqual("posted");
    [Fact] void should_return_what_the_ability_returned() => ((AbilityReturned)_result).Output.ShouldEqual("{\"ts\":\"1\"}");
    [Fact] void should_send_the_input() => _backend.Invoked.Single().InputJson.ShouldEqual("{\"text\":\"hi\"}");
    [Fact] void should_name_the_ability_and_its_contract_major() => (_backend.Invoked.Single().Ability == "Post" && _backend.Invoked.Single().ContractMajor == 1).ShouldBeTrue();
    [Fact] void should_tell_the_effect_class() => _backend.Invoked.Single().Effect.ShouldEqual(Cratis.Stage.Extensions.Protocol.EffectClass.Once);
    [Fact] void should_carry_the_invocation_identity_as_the_idempotency_key() => (_backend.Invoked.Single().Context.InvocationId == "invocation-1" && _backend.Invoked.Single().Context.IdempotencyKey == "invocation-1").ShouldBeTrue();
    [Fact] void should_carry_the_attempt() => _backend.Invoked.Single().Context.Attempt.ShouldEqual(1);
    [Fact] void should_carry_the_tenant_and_the_system() => (_backend.Invoked.Single().Context.Tenant == "Default" && _backend.Invoked.Single().Context.System == "Slack").ShouldBeTrue();
    [Fact] void should_carry_what_caused_it() => (_backend.Invoked.Single().Context.Causation == "event-42" && _backend.Invoked.Single().Context.OriginAddress == "Fulfillment/Post/branch 1").ShouldBeTrue();
    [Fact] void should_carry_the_package_it_pinned() => _backend.Invoked.Single().Context.PackageDigest.ShouldEqual("sha256:aaaa");
    [Fact] void should_carry_a_deadline_in_the_future() => (_backend.Invoked.Single().Context.Deadline.ToDateTime() > DateTime.UtcNow).ShouldBeTrue();
    [Fact] void should_hand_the_host_endpoint_over_at_the_handshake() => _backend.Described!.HostEndpoint.ShouldEqual(_options.HostEndpoint);
    [Fact] void should_hand_a_credential_over_at_the_handshake() => _backend.Described!.SessionToken.Length.ShouldBeGreaterThan(0);
    [Fact] void should_speak_protocol_version_one() => _backend.Described!.ProtocolVersion.Major.ShouldEqual(1);
    [Fact] void should_do_the_handshake_once() => _backend.Handshakes.ShouldEqual(1);
}
