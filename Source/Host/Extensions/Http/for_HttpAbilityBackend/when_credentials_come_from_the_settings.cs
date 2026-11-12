// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Http.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Http.for_HttpAbilityBackend;

public class when_credentials_come_from_the_settings : an_http_system
{
    RecordedRequest _bearer = null!;
    RecordedRequest _basic = null!;
    RecordedRequest _apiKey = null!;
    RecordedRequest _customApiKey = null!;
    RecordedRequest _none = null!;

    async Task Establish()
    {
        await StartSystem();
        StartStage(Parsed(BookPickup));
    }

    async Task Because()
    {
        _values[$"{System}:Http:Authentication"] = "bearer";
        _values[$"{System}:Http:Token"] = Secret;
        await _backend.Invoke(Call(), CancellationToken.None);
        _bearer = _received[^1];

        _values[$"{System}:Http:Authentication"] = "basic";
        _values[$"{System}:Http:Username"] = "stage";
        _values[$"{System}:Http:Password"] = Secret;
        await _backend.Invoke(Call(), CancellationToken.None);
        _basic = _received[^1];

        _values[$"{System}:Http:Authentication"] = "apiKey";
        _values[$"{System}:Http:ApiKey"] = Secret;
        await _backend.Invoke(Call(), CancellationToken.None);
        _apiKey = _received[^1];

        _values[$"{System}:Http:ApiKeyHeader"] = "X-Carrier-Key";
        await _backend.Invoke(Call(), CancellationToken.None);
        _customApiKey = _received[^1];

        _values.Remove($"{System}:Http:Authentication");
        await _backend.Invoke(Call(), CancellationToken.None);
        _none = _received[^1];
    }

    [Fact] void should_send_the_bearer_token() => _bearer.Headers["Authorization"].ShouldEqual($"Bearer {Secret}");
    [Fact] void should_send_basic_credentials() => _basic.Headers["Authorization"].ShouldEqual("Basic " + Convert.ToBase64String(global::System.Text.Encoding.UTF8.GetBytes($"stage:{Secret}")));
    [Fact] void should_send_the_api_key_in_the_default_header() => _apiKey.Headers["X-API-Key"].ShouldEqual(Secret);
    [Fact] void should_send_the_api_key_in_the_configured_header() => _customApiKey.Headers["X-Carrier-Key"].ShouldEqual(Secret);
    [Fact] void should_send_no_credentials_when_none_are_configured() => (_none.Headers.ContainsKey("Authorization") || _none.Headers.ContainsKey("X-API-Key")).ShouldBeFalse();
    [Fact] void should_never_log_a_secret() => _logs.Records.Exists(record => record.Message.Contains(Secret, StringComparison.Ordinal)).ShouldBeFalse();
}
