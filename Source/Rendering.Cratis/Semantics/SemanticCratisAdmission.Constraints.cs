// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticCratisAdmission
{
    // Chronicle enforces a constraint when an event is appended, so one declared anywhere governs every selected
    // command producing its events. The planner renders no Chronicle constraints yet, and an application without
    // them accepts appends the reference evaluator rejects.
    static void ValidateConstraints(
        SemanticApplicationContext context,
        IReadOnlyList<LocatedSemanticSlice> slices,
        List<ArtifactRenderDiagnostic> diagnostics)
    {
        var selected = slices.Select(_ => _.Slice.Id).ToHashSet();
        var produced = slices.SelectMany(_ => _.Slice.Commands).SelectMany(_ => _.Produces).Select(_ => _.EventContract).ToHashSet();
        foreach (var (slice, constraint) in context.Constraints.Where(_ =>
            selected.Contains(_.Slice.Id) || _.Constraint.Targets.Any(target => produced.Contains(target.EventContract))))
        {
            diagnostics.Add(Error("STAGE-ESM-014", $"Constraint '{constraint.Name}' is enforced at append time, which the Cratis ESM planner does not render yet.", slice.Id));
        }
    }
}
