// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Host.Invocations;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Stage.Host.Extensions.Http;

/// <summary>
/// Registers the built-in <c lang="text">http</c> extension.
/// </summary>
public static class StageHttpAbilityHosting
{
    const int MaxResponseBytes = 4 * 1024 * 1024;

    /// <summary>
    /// Realizes the given abilities with their <c lang="text">http</c> blocks. Call it after <see cref="StageInvocationHosting.AddStageInvocations"/> and after the extension host is added, as it
    /// decorates the <see cref="IAbilityBackend"/> registered by then: abilities without a block still go to it.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="definitions">The <see cref="HttpAbilityDefinition"/> to realize.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no ability backend is registered to decorate.</exception>
    public static IServiceCollection AddStageHttpAbilities(this IServiceCollection services, IEnumerable<HttpAbilityDefinition> definitions)
    {
        var held = definitions.ToArray();
        var previous = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(IAbilityBackend))
            ?? throw new InvalidOperationException("Register the invocations with AddStageInvocations before the http abilities.");

        services.AddHttpClient(HttpRealizingAbilityBackend.ClientName, client =>
        {
            client.MaxResponseContentBufferSize = MaxResponseBytes;
            client.Timeout = Timeout.InfiniteTimeSpan;
        }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false });

        services.TryAddSingleton<ISystemSettings, ConfigurationSystemSettings>();
        services.TryAddSingleton<HttpSystemAccess>();
        services.TryAddSingleton<IHttpAbilityDefinitions>(new HttpAbilityDefinitions(held));
        services.Replace(ServiceDescriptor.Singleton<IAbilityBackend>(provider => new HttpRealizingAbilityBackend(
            Previous(provider, previous),
            provider.GetRequiredService<IHttpAbilityDefinitions>(),
            provider.GetRequiredService<HttpSystemAccess>(),
            provider.GetRequiredService<IHttpClientFactory>(),
            provider.GetRequiredService<ILogger<HttpRealizingAbilityBackend>>())));
        return services;
    }

    static IAbilityBackend Previous(IServiceProvider provider, ServiceDescriptor previous) => previous switch
    {
        { ImplementationInstance: IAbilityBackend instance } => instance,
        { ImplementationFactory: { } factory } => (IAbilityBackend)factory(provider),
        { ImplementationType: { } type } => (IAbilityBackend)ActivatorUtilities.CreateInstance(provider, type),
        _ => throw new InvalidOperationException("The registered ability backend cannot be created.")
    };
}
