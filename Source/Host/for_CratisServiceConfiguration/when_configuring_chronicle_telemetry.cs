// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_CratisServiceConfiguration;

public class when_configuring_chronicle_telemetry : Specification
{
    ServiceProvider _services = null!;
    ChronicleTelemetryConfiguration _configuration = null!;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddStageCratis("TelemetryConfiguration", "Stage");
        _services = builder.Services.BuildServiceProvider();
    }

    void Because()
    {
        _configuration = _services.GetRequiredService<ChronicleTelemetryConfiguration>();
    }

    [Fact] void should_register_the_telemetry_configuration() => _configuration.ShouldNotBeNull();
    [Fact] void should_register_the_telemetry_callback() => _configuration.Configure.ShouldNotBeNull();

    void Destroy() => _services.Dispose();
}
