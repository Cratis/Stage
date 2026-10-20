// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentityOptions.when_binding;

public class the_docker_defaults : Specification
{
    bool _enabled;

    void Because()
    {
        var configuration = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(typeof(SandboxIdentityOptions).Assembly.Location)!)
            .AddJsonFile("appsettings.Docker.json").Build();
        var services = new ServiceCollection();
        SandboxIdentityOptions.Configure(services, configuration);
        using var provider = services.BuildServiceProvider();
        _enabled = provider.GetRequiredService<SandboxIdentityOptions>().Enabled;
    }

    [Fact] void should_enable_the_sandbox_identity() => _enabled.ShouldBeTrue();
}
