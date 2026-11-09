// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Specifications;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentityOptions.when_binding;

public class with_forwarded_identity_only : Specification
{
    ServiceProvider _provider = null!;

    void Because()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Stage:Sandbox:ForwardedIdentity"] = "true" }).Build();
        var services = new ServiceCollection();
        services.AddOptions<ArcOptions>();
        SandboxIdentityOptions.Configure(services, configuration, Substitute.For<IHostEnvironment>());
        _provider = services.BuildServiceProvider();
    }

    [Fact] void should_trust_the_identity_authproxy_forwards() => _provider.GetRequiredService<IOptions<ArcOptions>>().Value.TrustForwardedIdentityHeaders.ShouldBeTrue();
    [Fact] void should_not_play_a_persona() => _provider.GetRequiredService<SandboxIdentityOptions>().Enabled.ShouldBeFalse();

    void Destroy() => _provider.Dispose();
}
