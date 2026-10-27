// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_keeping_safety_refusals;

public class with_a_protected_identity_use : Specification
{
    ExecutableSemanticModel _model = null!;
    SemanticId _concept;
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        _model = invoice_model.Compile(a_concept_compliance_model.Source);
        _concept = _model.Application.Concepts.Single(concept => concept.Name == "InvoiceId").Id;
        var application = _model.Application with
        {
            Concepts = [.. _model.Application.Concepts.Select(concept => concept.Id == _concept ? concept with { Compliance = new(new(), null) } : concept)]
        };

        // Model a producer bypassing upstream validation, as in the existing protected-identity specs.
        typeof(ExecutableSemanticModel).GetField("<Application>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_model, application);
    }

    void Because() => _plan = invoice_model.Plan(_model);

    [Fact] void should_return_a_refused_plan() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
    [Fact] void should_report_the_protected_concept_identity() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-040" && diagnostic.Artifact == _concept && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_not_render_around_the_protected_identity() => _plan.Refusals.ShouldBeEmpty();
}
#endif
