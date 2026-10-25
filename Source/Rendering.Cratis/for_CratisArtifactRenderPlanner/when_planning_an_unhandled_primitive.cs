// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_an_unhandled_primitive : a_register_project_render_request
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        // Fault injection deliberately bypasses Screenplay model validation: a future enum value can arrive
        // from an updated dependency even when the current compiler cannot produce it from .play source.
        var model = _model with { };
        var application = model.Application with
        {
            Concepts = [model.Application.Concepts[0] with { Primitive = (SemanticPrimitiveType)99 },
                .. model.Application.Concepts.Skip(1)]
        };
        typeof(ExecutableSemanticModel)
            .GetField("<Application>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(model, application);
        _plan = _planner.Plan(_request with { Model = model });
    }

    [Fact] void should_report_the_typed_diagnostic() => _plan.Diagnostics.Single(diagnostic => string.Equals(diagnostic.Code, "STAGE-ESM-012", StringComparison.Ordinal) && diagnostic.Artifact == _model.Application.Concepts[0].Id).Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Warning);
    [Fact] void should_record_the_failed_owner() => _plan.ImplementationGaps.Any(gap => gap.Kind == ArtifactImplementationGapKind.Rendering && string.Equals(gap.Owner, _model.Application.Concepts[0].Id.ToString(), StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_be_partial() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
    [Fact] void should_keep_independent_concepts() => _plan.Artifacts.Any(artifact => artifact.Sources.Contains(_model.Application.Concepts[1].Id)).ShouldBeTrue();
#if DEBUG
    [Fact] void should_compile_the_remaining_output() => given.opaque_policy_model.Compile(_plan).GetTypes().Length.ShouldBeGreaterThan(0);
#endif
}
