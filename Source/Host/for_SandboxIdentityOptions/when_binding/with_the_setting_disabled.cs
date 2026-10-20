// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Specifications;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentityOptions.when_binding;

public class with_the_setting_disabled : Specification
{
    ServiceProvider _provider = null!;

    void Because()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Stage:Sandbox:Identity"] = "false" }).Build();
        var services = new ServiceCollection();
        services.AddOptions<ArcOptions>();
        SandboxIdentityOptions.Configure(services, configuration);
        _provider = services.BuildServiceProvider();
    }

    [Fact] void should_leave_forwarded_header_trust_disabled() => _provider.GetRequiredService<IOptions<ArcOptions>>().Value.TrustForwardedIdentityHeaders.ShouldBeFalse();
    [Fact] void should_gate_the_persona_fallback() => _provider.GetRequiredService<SandboxIdentityOptions>().Enabled.ShouldEqual(false);

    void Destroy() => _provider.Dispose();
}
