// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_warning_on_secret_read_model_properties;

public class with_a_projection_copying_a_secret_concept : given.a_secret_read_model_plan
{
    void Because() => _plan = invoice_model.Plan(Compile());

    [Fact] void should_report_one_warning_for_the_secret_property() => Warnings.Count(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning).ShouldEqual(1);
    [Fact] void should_name_the_concept_read_model_and_property() => Warnings.Single().Message.ShouldContain("Read model 'Information' property 'apiKey' is populated with secret concept 'ApiKey'");
    [Fact] void should_not_warn_for_a_personal_only_concept() => Warnings.ShouldNotContain(diagnostic => diagnostic.Message.Contains("'health'", StringComparison.Ordinal));
    [Fact] void should_not_refuse_the_plan() => _plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
    [Fact] void should_still_emit_artifacts() => _plan.Artifacts.ShouldNotBeEmpty();
}
