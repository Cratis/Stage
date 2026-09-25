// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Registers an admitted semantic plan without making the implementation discoverable by unrelated Cratis hosts.
/// </summary>
public static class SemanticRuntimeHosting
{
    /// <summary>
    /// Registers the in-process runtime and Chronicle fact appender for one Stage session.
    /// </summary>
    /// <param name="services">The host service collection.</param>
    /// <param name="plan">The admitted plan.</param>
    public static void Add(IServiceCollection services, SemanticExecutionPlan plan) => Add(services, plan, SemanticWorld.Empty);

    /// <summary>
    /// Registers an admitted plan with a validated reconstructed world.
    /// </summary>
    /// <param name="services">The host service collection.</param>
    /// <param name="plan">The admitted plan.</param>
    /// <param name="world">The reconstructed world.</param>
    public static void Add(IServiceCollection services, SemanticExecutionPlan plan, SemanticWorld world) => Add(services, plan, () => world);

    /// <summary>
    /// Registers a world supplied after Chronicle registration and before serving requests.
    /// </summary>
    /// <param name="services">The host service collection.</param>
    /// <param name="plan">The admitted plan.</param>
    /// <param name="world">The validated world provider.</param>
    public static void Add(IServiceCollection services, SemanticExecutionPlan plan, Func<SemanticWorld> world)
    {
        services.AddSingleton(plan);
        services.AddSingleton<IAppendSemanticFacts, SemanticFactAppender>();
        services.AddSingleton<ISemanticRuntime>(provider => new SemanticRuntime(plan, provider.GetRequiredService<IAppendSemanticFacts>(), world));
    }

    /// <summary>
    /// Creates a runtime with a supplied atomic fact appender for an isolated session.
    /// </summary>
    /// <param name="plan">The admitted plan.</param>
    /// <param name="appender">The fact appender.</param>
    /// <returns>An isolated runtime.</returns>
    public static ISemanticRuntime Create(SemanticExecutionPlan plan, IAppendSemanticFacts appender) => new SemanticRuntime(plan, appender);

    /// <summary>
    /// Creates a runtime from a previously validated world.
    /// </summary>
    /// <param name="plan">The admitted plan.</param>
    /// <param name="appender">The fact appender.</param>
    /// <param name="world">The reconstructed world.</param>
    /// <returns>An isolated runtime.</returns>
    public static ISemanticRuntime Create(SemanticExecutionPlan plan, IAppendSemanticFacts appender, SemanticWorld world) => new SemanticRuntime(plan, appender, world);
}
