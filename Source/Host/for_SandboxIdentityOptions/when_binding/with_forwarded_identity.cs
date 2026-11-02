// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Specifications;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentityOptions.when_binding;

public class with_forwarded_identity : Specification
{
    ServiceProvider _provider = null!;

    void Because()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Stage:Sandbox:Identity"] = "true",
            ["Stage:Sandbox:ForwardedIdentity"] = "true"
        }).Build();
        var services = new ServiceCollection();
        services.AddOptions<ArcOptions>();
        SandboxIdentityOptions.Configure(services, configuration);
        _provider = services.BuildServiceProvider();
    }

    [Fact] void should_trust_the_identity_authproxy_forwards() => _provider.GetRequiredService<IOptions<ArcOptions>>().Value.TrustForwardedIdentityHeaders.ShouldBeTrue();
    [Fact] void should_stop_playing_a_persona_so_the_signed_in_caller_decides() => _provider.GetRequiredService<SandboxIdentityOptions>().Enabled.ShouldBeFalse();
    [Fact] void should_say_the_identity_is_forwarded() => _provider.GetRequiredService<SandboxIdentityOptions>().ForwardedIdentity.ShouldBeTrue();

    void Destroy() => _provider.Dispose();
}

public class with_forwarded_identity_only : Specification
{
    ServiceProvider _provider = null!;

    void Because()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Stage:Sandbox:ForwardedIdentity"] = "true" }).Build();
        var services = new ServiceCollection();
        services.AddOptions<ArcOptions>();
        SandboxIdentityOptions.Configure(services, configuration);
        _provider = services.BuildServiceProvider();
    }

    [Fact] void should_trust_the_identity_authproxy_forwards() => _provider.GetRequiredService<IOptions<ArcOptions>>().Value.TrustForwardedIdentityHeaders.ShouldBeTrue();
    [Fact] void should_not_play_a_persona() => _provider.GetRequiredService<SandboxIdentityOptions>().Enabled.ShouldBeFalse();

    void Destroy() => _provider.Dispose();
}
