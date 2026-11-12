// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Host.Extensions.Activation;
using Cratis.Stage.Host.Extensions.Components;
using Cratis.Stage.Host.Extensions.Inbound;
using Cratis.Stage.Host.Invocations;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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

        services.TryAddSingleton<ConfiguredExtensionEndpoints>();
        services.Replace(ServiceDescriptor.Singleton(provider => new ActivatedSystems(null, null, provider.GetRequiredService<IOptions<ExtensionOptions>>())));
        services.TryAddSingleton<ExtensionActivator>();
        services.TryAddSingleton<IBackendActivity>(provider => provider.GetRequiredService<ExtensionActivator>());
        services.TryAddSingleton<IScreenExtensionUsage, NoScreenExtensionUsage>();
        services.TryAddSingleton<ScreenActivation>();
        services.AddSingleton<IBackendRuntime, NativeBackendRuntime>();
        services.AddSingleton<IBackendRuntime, SharedHostBackendRuntime>();
        services.AddSingleton<IBackendRuntime, ContainerBackendRuntime>();
        services.TryAddSingleton<IExtensionEndpoints>(provider => new ActivatingExtensionEndpoints(provider.GetRequiredService<ConfiguredExtensionEndpoints>(), provider.GetRequiredService<ExtensionActivator>()));
        services.TryAddSingleton<ISystemSettings, ConfigurationSystemSettings>();
        services.TryAddSingleton<IContractTriggerRoutes, NoContractTriggerRoutes>();
        services.TryAddSingleton<IContractTriggers, SignalContractTriggers>();
        services.TryAddSingleton<IExtensionComponentCatalog>(ExtensionComponentCatalog.Empty);
        services.TryAddSingleton<ISystemSettingDeclarations>(SystemSettingDeclarations.Empty);
        services.TryAddSingleton<ExtensionPublicSettings>();
        services.TryAddSingleton<BrowserInvocationOptions>();
        services.TryAddSingleton<BrowserInvocations>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IInvocationOriginHandler, ScreenInteractionOriginHandler>());
        services.TryAddSingleton<ExtensionSessions>();
        services.TryAddSingleton<ExtensionTelemetry>();
        services.TryAddSingleton<ExtensionConnections>();
        services.Replace(ServiceDescriptor.Singleton<IAbilityBackend, GrpcAbilityBackend>());
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IInboundDeliveries, GrainInboundDeliveries>();
        services.TryAddSingleton<InboundDeliveryDispatcher>();
        services.AddHostedService<ExtensionHostServer>();
        services.AddHostedService<ExtensionActivationService>();
        services.AddHostedService(provider => provider.GetRequiredService<InboundDeliveryDispatcher>());
        return services;
    }
}
