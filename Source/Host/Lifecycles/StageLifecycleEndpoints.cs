// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Lifecycles;
using Cratis.Stage.Lifecycles;

namespace Cratis.Stage.Host.Lifecycles;

/// <summary>
/// Exposes where state machines are, what the runtime cannot drive, and the approvals workflows are waiting for.
/// </summary>
internal static class StageLifecycleEndpoints
{
    /// <summary>The route that reports state machines and workflow runs.</summary>
    internal const string Route = "/stage/lifecycles";

    /// <summary>
    /// The body of an approval decision.
    /// </summary>
    /// <param name="Approved">Whether the person approved.</param>
    /// <param name="Actor">Who decided; the sandbox person when omitted.</param>
    internal sealed record ApprovalDecision(bool Approved, string? Actor);

    internal static void Map(WebApplication app, SemanticExecutionPlan plan, Func<LifecycleDriver?> driver)
    {
        var application = plan.Model.Application;
        app.MapGet(Route, async () =>
        {
            var current = driver();
            var effects = app.Services.GetRequiredService<StageLifecycleEffects>();
            var machines = new List<object>();
            foreach (var machine in application.StateMachines)
            {
                var states = current is null ? [] : await current.States(machine);
                machines.Add(new { machine.Name, States = states.Select(item => new { Key = LifecycleSubjects.KeyText(item.Key), item.State }) });
            }

            var executable = LifecycleAdmission.ExecutableWorkflows(plan);
            var workflows = new List<object>();
            foreach (var workflow in application.Workflows)
            {
                workflows.Add(new { workflow.Name, Runs = executable.Contains(workflow.Name) ? await effects.Runs(workflow.Name) : [] });
            }

            return Results.Json(new { StateMachines = machines, Workflows = workflows, Refused = LifecycleAdmission.ForRuntime(plan) }, StageJson.Options);
        });
        app.MapPost($"{Route}/workflows/{{workflow}}/subjects/{{subject}}/approvals/{{node}}", async (string workflow, string subject, string node, ApprovalDecision decision) =>
        {
            var effects = app.Services.GetRequiredService<StageLifecycleEffects>();
            var decided = await effects.Decide(workflow, subject, node, decision.Approved, decision.Actor ?? "sandbox");
            return decided ? Results.Accepted() : Results.NotFound();
        });
    }
}
