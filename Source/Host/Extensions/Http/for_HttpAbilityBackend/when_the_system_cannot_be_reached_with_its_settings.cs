// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Http.given;
using Cratis.Stage.Host.Invocations;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Http.for_HttpAbilityBackend;

public class when_the_system_cannot_be_reached_with_its_settings : an_http_system
{
    AbilityResult _noAddress = null!;
    AbilityResult _noToken = null!;
    AbilityResult _unknownScheme = null!;
    AbilityResult _credentialsInAddress = null!;

    async Task Establish()
    {
        await StartSystem();
        StartStage(Parsed(BookPickup));
    }

    async Task Because()
    {
        var address = _values[$"{System}:Http:BaseAddress"];
        _values.Remove($"{System}:Http:BaseAddress");
        _noAddress = await _backend.Invoke(Call(), CancellationToken.None);

        _values[$"{System}:Http:BaseAddress"] = address;
        _values[$"{System}:Http:Authentication"] = "bearer";
        _noToken = await _backend.Invoke(Call(), CancellationToken.None);

        _values[$"{System}:Http:Authentication"] = "kerberos";
        _unknownScheme = await _backend.Invoke(Call(), CancellationToken.None);

        _values[$"{System}:Http:Authentication"] = "none";
        _values[$"{System}:Http:BaseAddress"] = $"http://user:{Secret}@127.0.0.1:1/";
        _credentialsInAddress = await _backend.Invoke(Call(), CancellationToken.None);
    }

    [Fact] void should_not_send_without_a_base_address() => ((AbilityNotSent)_noAddress).Reason.ShouldContain("systems:Carrier:Http:BaseAddress");
    [Fact] void should_not_send_without_the_token() => ((AbilityNotSent)_noToken).Reason.ShouldContain("systems:Carrier:Http:Token");
    [Fact] void should_not_send_with_an_unknown_authentication() => (_unknownScheme is AbilityNotSent).ShouldBeTrue();
    [Fact] void should_refuse_credentials_written_into_the_address() => (_credentialsInAddress is AbilityNotSent).ShouldBeTrue();
    [Fact] void should_not_echo_a_secret_in_the_refusal() => ((AbilityNotSent)_credentialsInAddress).Reason.Contains(Secret, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_have_sent_anything() => _received.Count.ShouldEqual(0);
}
