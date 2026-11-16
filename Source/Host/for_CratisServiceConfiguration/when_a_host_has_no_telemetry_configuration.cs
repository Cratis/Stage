// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.AspNetCore;
using Cratis.Chronicle.Diagnostics;
using Cratis.Specifications;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cratis.Stage.Host.for_CratisServiceConfiguration;

public class when_a_host_has_no_telemetry_configuration : Specification
{
    ServiceProvider _services = null!;
    ChronicleAspNetCoreOptions _options = null!;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddStageCratis("TelemetryConfiguration", "Stage");
        _services = builder.Services.BuildServiceProvider();
    }

    void Because() => _options = _services.GetRequiredService<IOptions<ChronicleAspNetCoreOptions>>().Value;

    // Chronicle no longer binds the telemetry configuration by convention, so a host that never asks for telemetry carries none and keeps the defaults.
    [Fact] void should_not_bind_a_telemetry_configuration() => _services.GetServices<ChronicleTelemetryConfiguration>().ShouldBeEmpty();
    [Fact] void should_keep_default_identifier_tracing() => _options.Telemetry.EventSourceIdTracing.ShouldEqual(EventSourceIdTracing.Raw);

    void Destroy() => _services.Dispose();
}
