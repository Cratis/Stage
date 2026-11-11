// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Extensions.Protocol;
using Cratis.Stage.Host.Extensions.given;
using Cratis.Stage.Host.Invocations;
using Xunit;
using EffectClass = Cratis.Stage.Host.Invocations.EffectClass;

namespace Cratis.Stage.Host.Extensions.for_GrpcAbilityBackend;

public class when_the_backend_cannot_be_used : an_extension_protocol
{
    protected override IEnumerable<AbilityDescriptor> Declared => [Descriptor("Post", EffectClass.Once, "posted"), Descriptor("Pin", EffectClass.Idempotent)];

    AbilityResult _contractDeparts = null!;
    AbilityResult _unknownSystem = null!;
    AbilityResult _otherProtocol = null!;
    int _invoked;

    async Task Establish()
    {
        await StartBackend();
        await StartStage();
    }

    async Task Because()
    {
        // The backend realizes Post but not Pin.
        _contractDeparts = await _abilities.Invoke(Call(), CancellationToken.None);

        _backend.Description = Described(Ability("Post", Cratis.Stage.Extensions.Protocol.EffectClass.Once, "posted"), Ability("Pin", Cratis.Stage.Extensions.Protocol.EffectClass.Idempotent));
        _backend.Description.ProtocolVersion.Major = 2;
        _otherProtocol = await _abilities.Invoke(Call(), CancellationToken.None);

        _unknownSystem = await _abilities.Invoke(Call() with { Ability = new("Mail", "Send", 1) }, CancellationToken.None);
        _invoked = _backend.Invoked.Count;
    }

    [Fact] void should_not_send_when_the_backend_does_not_honor_the_contract() => ((AbilityNotSent)_contractDeparts).Reason.ShouldContain("Pin@1 is not realized");
    [Fact] void should_not_send_when_the_protocol_major_differs() => ((AbilityNotSent)_otherProtocol).Reason.ShouldContain("protocol major 2");
    [Fact] void should_not_send_to_a_system_with_no_backend() => ((AbilityNotSent)_unknownSystem).Reason.ShouldContain("No backend is known for the system Mail");
    [Fact] void should_never_have_called_the_backend() => _invoked.ShouldEqual(0);
}
