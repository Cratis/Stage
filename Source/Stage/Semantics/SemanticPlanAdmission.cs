// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Semantics;

// Screenplay keeps the plan's unadmitted identities internal. Its public partial compilation
// exposes the same issues, without reflection or dropping the refusal state from the original plan.
internal static class SemanticPlanAdmission
{
    static readonly ConditionalWeakTable<SemanticExecutionPlan, SemanticExecutionPlanCompilation> _compilations = [];

    internal static SemanticUnsupportedCapability? Check(SemanticExecutionPlan plan, SemanticId artifact)
    {
        var compilation = Compilation(plan);
        var issues = compilation.Issues.Concat(compilation.UnselectedIssues).ToArray();
        var issue = issues.FirstOrDefault(issue => issue.Artifact == artifact);
        var capability = plan.Commands.ContainsKey(artifact) ? StageExecutionCapability.Command : StageExecutionCapability.Projection;
        var readModel = artifact;
        if (plan.Queries.TryGetValue(artifact, out var query))
        {
            capability = StageExecutionCapability.Query;
            readModel = query.ReadModel;
        }
        issue ??= issues.FirstOrDefault(issue => plan.Projections.TryGetValue(issue.Artifact, out var projection) && projection.ReadModel == readModel);

        return issue is null
            ? SemanticProjectionAdmission.CheckProductions(plan, artifact)
            : new(capability, artifact.ToString(), $"STAGE-PLAN-004: {issue.Artifact}: {issue.Details}");
    }

    internal static SemanticExecutionPlanCompilation Compilation(SemanticExecutionPlan plan) =>
        _compilations.GetValue(plan, static plan => SemanticExecutionPlan.Compile(plan.Model, []));
}
