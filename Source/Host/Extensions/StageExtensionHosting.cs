// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Host.Invocations;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// Registers the extension protocol on the host.
/// </summary>
public static class StageExtensionHosting
{
    /// <summary>
    /// Registers the extension protocol. Nothing changes until <see cref="ExtensionOptions.HostPort"/> is set: then the host services listen, abilities are
    /// realized by calling backends, and the endpoints, settings and trigger routes registered earlier are kept, as they are the seams the package loader and the
    /// language support fill.
    /// </summary>
    /// <param name="services">The collection to register the services in.</param>
    /// <param name="configuration">The configuration, whose <c lang="text">Stage:Extensions</c> section holds the options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddStageExtensions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ExtensionOptions>(configuration.GetSection(ExtensionOptions.Section));
        if (configuration.GetValue<int>($"{ExtensionOptions.Section}:{nameof(ExtensionOptions.HostPort)}") <= 0) return services;

        services.TryAddSingleton<IExtensionEndpoints, ConfiguredExtensionEndpoints>();
        services.TryAddSingleton<ISystemSettings, ConfigurationSystemSettings>();
        services.TryAddSingleton<IContractTriggerRoutes, NoContractTriggerRoutes>();
        services.TryAddSingleton<IContractTriggers, SignalContractTriggers>();
        services.TryAddSingleton<ExtensionSessions>();
        services.TryAddSingleton<ExtensionTelemetry>();
        services.TryAddSingleton<ExtensionConnections>();
        services.Replace(ServiceDescriptor.Singleton<IAbilityBackend, GrpcAbilityBackend>());
        services.AddHostedService<ExtensionHostServer>();
        return services;
    }
}
