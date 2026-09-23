// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
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

    [Fact] void should_report_the_typed_diagnostic() => Assert.Contains(_plan.Diagnostics, _ => _.Code == "STAGE-ESM-012" && _.Severity == ArtifactRenderDiagnosticSeverity.Error);
    [Fact] void should_not_be_publishable() => Assert.False(_plan.Success);
    [Fact] void should_discard_scaffold_and_semantic_artifacts() => Assert.Empty(_plan.Artifacts);
}
