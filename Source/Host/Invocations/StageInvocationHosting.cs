// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Hosting;
using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Definitions;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// Registers what durable ability invocations need on the silo Stage hosts.
/// </summary>
public static class StageInvocationHosting
{
    /// <summary>
    /// Registers the invocation services. A catalog, a backend, an answer authorizer and origin handlers registered earlier are kept: they are the seams
    /// the language support for abilities, the extension protocol and the behaviors that invoke abilities fill.
    /// </summary>
    /// <param name="services">The collection to register the services in.</param>
    /// <param name="configuration">The configuration, whose <c lang="text">Stage:Invocations</c> section holds the limits per system.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddStageInvocations(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StageInvocationOptions>(configuration.GetSection(StageInvocationOptions.Section));
        services.PostConfigure<StageInvocationOptions>(options => options.GrainStorageIsDurable = configuration.GetValue("Stage:Orleans:Storage", GrainStorageMode.InMemory) == GrainStorageMode.MongoDB);
        services.TryAddSingleton<InvocationDurability>();
        services.TryAddSingleton<IAbilityCatalog>(AbilityCatalog.Empty);
        services.TryAddSingleton<IAbilityBackend, NoAbilityBackend>();
        services.TryAddSingleton<IInvocationAnswerAuthorizer, PolicyInvocationAnswerAuthorizer>();
        services.TryAddSingleton<SystemGates>();
        services.TryAddSingleton<IInvocationLedger, InvocationLedger>();
        services.TryAddSingleton<InvocationSettlements>();
        services.TryAddSingleton<AbilityWorkflowDefinitions>();
        services.AddSingleton<IWorkflowDefinitions>(provider => provider.GetRequiredService<AbilityWorkflowDefinitions>());
        services.TryAddSingleton<AbilityWorkflowPerformers>();
        services.TryAddSingleton<IAbilityInvocations, AbilityInvocations>();
        return services;
    }

    /// <summary>
    /// Makes the engine find the performers of abilities beside the ones it already finds. The definitions need no such step: the engine asks every registered
    /// <see cref="IWorkflowDefinitions"/>, and <see cref="AddStageInvocations"/> registers the one of abilities.
    /// </summary>
    /// <param name="services">The collection, which may already hold the performers of the lifecycles.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection ComposeWithAbilities(this IServiceCollection services)
    {
        var performers = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(IWorkflowNodePerformers));
        if (performers is null)
        {
            services.AddSingleton<IWorkflowNodePerformers>(provider => provider.GetRequiredService<AbilityWorkflowPerformers>());
            return services;
        }

        services.Remove(performers);
        services.AddSingleton<IWorkflowNodePerformers>(provider => new ComposedWorkflowPerformers(Resolve(provider, performers), provider.GetRequiredService<AbilityWorkflowPerformers>()));
        return services;
    }

    static IWorkflowNodePerformers Resolve(IServiceProvider provider, ServiceDescriptor descriptor) =>
        (IWorkflowNodePerformers)(descriptor.ImplementationInstance
            ?? descriptor.ImplementationFactory?.Invoke(provider)
            ?? ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType!));
}
