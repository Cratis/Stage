// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.AspNetCore;
using Cratis.Chronicle.Diagnostics;
using Cratis.Specifications;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cratis.Stage.Host.for_CratisServiceConfiguration;

public class when_a_host_has_no_explicit_telemetry_configuration : Specification
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

    [Fact] void should_register_one_default_telemetry_configuration() => _services.GetServices<ChronicleTelemetryConfiguration>().Count().ShouldEqual(1);
    [Fact] void should_omit_event_source_ids_by_default() => _options.Telemetry.EventSourceIdTracing.ShouldEqual(EventSourceIdTracing.Omitted);

    void Destroy() => _services.Dispose();
}
