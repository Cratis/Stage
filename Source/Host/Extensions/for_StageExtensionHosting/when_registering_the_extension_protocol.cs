// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Invocations;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_StageExtensionHosting;

public class when_registering_the_extension_protocol : Specification
{
    IServiceCollection _disabled = null!;
    IServiceCollection _enabled = null!;

    void Because()
    {
        _disabled = new ServiceCollection().AddStageInvocations(Configuration([])).AddStageExtensions(Configuration([]));
        _enabled = new ServiceCollection().AddStageInvocations(Configuration([])).AddStageExtensions(Configuration(new() { ["Stage:Extensions:HostPort"] = "50100" }));
    }

    static IConfiguration Configuration(Dictionary<string, string?> values) => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    static Type? Backend(IServiceCollection services) => services.Last(descriptor => descriptor.ServiceType == typeof(IAbilityBackend)).ImplementationType;

    [Fact] void should_keep_the_backend_that_reaches_nothing_until_it_is_enabled() => Backend(_disabled).ShouldEqual(typeof(NoAbilityBackend));
    [Fact] void should_not_listen_until_it_is_enabled() => _disabled.Any(descriptor => descriptor.ImplementationType == typeof(ExtensionHostServer)).ShouldBeFalse();
    [Fact] void should_realize_abilities_by_calling_backends_once_enabled() => Backend(_enabled).ShouldEqual(typeof(GrpcAbilityBackend));
    [Fact] void should_listen_once_enabled() => _enabled.Any(descriptor => descriptor.ImplementationType == typeof(ExtensionHostServer)).ShouldBeTrue();
}
