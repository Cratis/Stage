// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_around_refusals;

public class with_a_refused_specification_outside_the_selected_slice : given.a_refused_specification
{
    SemanticApplicationContext _context = null!;
    List<ArtifactRenderDiagnostic> _outsideDiagnostics = [];
    ImmutableArray<ArtifactRenderRefusal> _outsideRefusals;

    void Establish()
    {
        _request = _original with { Scope = new(ArtifactRenderScopeKind.Slice, _projectLookup.Id) };
        _context = new(_request, _options);

        // Admission normally visits only selected slices. Exercise degradation of a real outside
        // diagnostic separately, without claiming that the scoped planner validates that slice.
        SemanticSpecificationAdmission.Validate(_context, _context.DeclaringSlice(_refused).Slice, _outsideDiagnostics);
    }

    void Because()
    {
        _outsideRefusals = SemanticRefusalScope.Degrade(_context, _outsideDiagnostics);
        _plan = _planner.Plan(_request);
    }

    [Fact] void should_keep_the_refused_specification_outside_the_scope() => _context.SelectedSlices().SelectMany(located => located.Slice.Specifications).Select(specification => specification.Id).ShouldNotContain(_refused);
    [Fact] void should_name_the_outside_specification_without_throwing() => _outsideDiagnostics.Single().Message.ShouldContain("Skipped specification 'RegisteringAProject' (STAGE-ESM-011)");
    [Fact] void should_preserve_the_outside_refusal_reason() => _outsideDiagnostics.Single().Message.ShouldContain("Given read-model state");
    [Fact] void should_degrade_the_outside_diagnostic_to_a_warning() => _outsideDiagnostics.Single().Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Warning);
    [Fact] void should_classify_the_outside_refusal() => _outsideRefusals.Single().Artifact.ShouldEqual(_refused);
    [Fact] void should_complete_the_scoped_plan() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Complete);
    [Fact] void should_render_the_selected_slice() => _plan.Artifacts.SelectMany(artifact => artifact.Sources).ShouldContain(_projectLookup.Id);
    [Fact] void should_never_plan_the_outside_specification() => _plan.Artifacts.SelectMany(artifact => artifact.Sources).ShouldNotContain(_refused);
    [Fact] void should_not_claim_to_omit_an_unplanned_specification() => _plan.Refusals.ShouldBeEmpty();
    [Fact] void should_not_report_an_unplanned_specification() => _plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Artifact == _refused);
}
