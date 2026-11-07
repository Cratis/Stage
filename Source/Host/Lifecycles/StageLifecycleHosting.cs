// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Definitions;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Lifecycles;
using Cratis.Stage.Lifecycles;
using Cratis.Stage.Semantics;
using Microsoft.Extensions.Logging;

namespace Cratis.Stage.Host.Lifecycles;

/// <summary>
/// Hosts the state machines and workflows of the loaded model on the Orleans silo.
/// </summary>
internal static class StageLifecycleHosting
{
    /// <summary>
    /// Registers the workflow definitions and node performers of a model. This must run before <see cref="StageOrleansConfiguration.AddStageOrleans(WebApplicationBuilder)"/>
    /// so the engine finds them first; the engine registers its own defaults only when nothing is registered.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="plan">The execution plan of the loaded model.</param>
    internal static void Add(IServiceCollection services, SemanticExecutionPlan plan)
    {
        services.AddSingleton<IWorkflowDefinitions>(StageWorkflowDefinitions.For(plan));
        services.AddSingleton<IWorkflowNodePerformers>(provider => new StageWorkflowPerformers(plan, () => provider.GetRequiredService<ISemanticRuntime>()));
        services.AddSingleton(provider => new StageLifecycleEffects(plan, provider.GetRequiredService<ISemanticRuntime>(), provider.GetRequiredService<IWorkflows>()));
    }

    /// <summary>
    /// Starts driving the lifecycles from the facts the runtime commits, once the world has been reconstructed.
    /// </summary>
    /// <param name="services">The application services.</param>
    /// <param name="plan">The execution plan.</param>
    /// <returns>The driver, to be disposed with the application; null when the model declares no state machine or workflow.</returns>
    internal static async Task<LifecycleDriver?> Start(IServiceProvider services, SemanticExecutionPlan plan)
    {
        var application = plan.Model.Application;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Cratis.Stage.Lifecycles");
        foreach (var refusal in LifecycleAdmission.ForRuntime(plan))
        {
            LifecycleLog.WorkflowRefused(logger, refusal.Owner, refusal.Code, refusal.Message);
        }

        if (application.StateMachines.IsDefaultOrEmpty && application.Workflows.IsDefaultOrEmpty) return null;
        var runtime = services.GetRequiredService<ISemanticRuntime>();
        var driver = new LifecycleDriver(plan, runtime, (ISemanticWorldFacts)runtime, services.GetRequiredService<StageLifecycleEffects>(), LifecycleAdmission.ExecutableWorkflows(plan), logger);
        await driver.Start();
        return driver;
    }
}
