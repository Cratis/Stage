// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_observable_optional_not_empty_concept_keys : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = invoice_model.Plan(invoice_model.Compile(an_observable_lookup_model.Source.Replace("min 2", "not empty", StringComparison.Ordinal)));

    [Fact] void should_report_no_errors() => _plan.Diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_emit_artifacts() => _plan.Artifacts.ShouldNotBeEmpty();
}
