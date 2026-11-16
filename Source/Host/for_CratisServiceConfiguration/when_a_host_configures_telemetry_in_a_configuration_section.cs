// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.AspNetCore;
using Cratis.Chronicle.Diagnostics;
using Cratis.Specifications;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cratis.Stage.Host.for_CratisServiceConfiguration;

public class when_a_host_configures_telemetry_in_a_configuration_section : Specification
{
    const string HashKey = "test-only-hash-key-at-least-32-characters";
    ServiceProvider _services = null!;
    ChronicleAspNetCoreOptions _options = null!;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Chronicle:Telemetry:EventSourceIdTracing"] = "Hashed",
            ["Cratis:Chronicle:Telemetry:EventSourceIdHashKey"] = HashKey
        });
        builder.AddStageCratis("TelemetryConfiguration", "Stage");
        _services = builder.Services.BuildServiceProvider();
    }

    void Because() => _options = _services.GetRequiredService<IOptions<ChronicleAspNetCoreOptions>>().Value;

    [Fact] void should_preserve_the_configured_identifier_tracing() => _options.Telemetry.EventSourceIdTracing.ShouldEqual(EventSourceIdTracing.Hashed);
    [Fact] void should_preserve_the_configured_hash_key() => _options.Telemetry.EventSourceIdHashKey.ShouldEqual(HashKey);

    void Destroy() => _services.Dispose();
}
