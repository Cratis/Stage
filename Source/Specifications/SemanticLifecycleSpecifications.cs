// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications;

/// <summary>
/// Verifies the specifications written inside state machines and workflows.
/// </summary>
/// <remarks>
/// Lifecycle specifications belong to the application rather than to a slice, so they are selected with the application scope
/// (or with no scope at all). They are decided by Screenplay's reference lifecycle evaluator over the same plan the slice
/// specifications run against, which is the meaning the runtime and the rendered application are held to.
/// </remarks>
static class SemanticLifecycleSpecifications
{
    internal const string StateMachineKind = "StateMachine";
    internal const string WorkflowKind = "Workflow";

    internal static IEnumerable<SemanticSpecificationRunRecord> Run(SemanticExecutionPlan plan, SemanticSpecificationSelection selection)
    {
        var scopes = selection.Scopes.IsDefault ? [] : selection.Scopes;
        var application = plan.Model.Application;
        if (!scopes.IsEmpty && !scopes.Contains(application.Id)) return [];
        if (application.StateMachines.IsDefaultOrEmpty && application.Workflows.IsDefaultOrEmpty) return [];

        var workflows = application.Workflows.Select(workflow => workflow.Name).ToHashSet(StringComparer.Ordinal);
        return SemanticLifecycleRunner.RunAll(plan.Model).Select(result => new SemanticSpecificationRunRecord(
            $"{application.Id}/{(workflows.Contains(result.Owner) ? WorkflowKind : StateMachineKind)}/{result.Owner}/{result.Name}",
            result.Name,
            $"{application.Id}/{result.Owner}",
            workflows.Contains(result.Owner) ? WorkflowKind : StateMachineKind,
            result.Passed ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Failed,
            null,
            null,
            result.Failures,
            null));
    }
}
