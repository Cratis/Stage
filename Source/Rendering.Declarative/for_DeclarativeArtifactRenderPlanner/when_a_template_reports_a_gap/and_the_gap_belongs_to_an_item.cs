// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_a_template_reports_a_gap;

public class and_the_gap_belongs_to_an_item : given.a_declarative_planner
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = PlanWith(new() { ["templates/command.sbn"] = "{{ if item.name == 'Echo' }}{{ gap 'throwing_operation_body' 'Missing behavior' 'model' }}{{ else }}ok{{ end }}" });

    [Fact] void should_keep_the_placeholder_in_the_degraded_artifact() => Text(_plan, "Commands/Echo.test").ShouldEqual("/* ThrowingOperationBody STAGE-DEGRADE-002 */");
    [Fact] void should_still_render_the_other_artifacts() => Text(_plan, "Commands/Shout.test").ShouldEqual("ok");
    [Fact] void should_render_every_output() => _plan.Artifacts.Length.ShouldEqual(4);
    [Fact] void should_record_one_degradation_warning() => _plan.Diagnostics.Select(_ => (_.Code, _.Severity)).ShouldEqual([(ArtifactRenderDegradationCodes.UnsupportedConstruct, ArtifactRenderDiagnosticSeverity.Warning)]);
    [Fact] void should_own_the_warning_by_the_item() => _plan.Diagnostics[0].Artifact.ShouldEqual(_plan.Artifacts.Single(_ => _.RelativePath == "Commands/Echo.test").Sources.Single());
    [Fact] void should_record_the_degradation_for_the_item() => _plan.Degradations.Select(_ => _.Diagnostic).ShouldEqualInOrder([.. _plan.Diagnostics]);
    [Fact] void should_have_no_plan_evidence() => _plan.PlanEvidence.ShouldBeEmpty();
    [Fact] void should_still_succeed() => _plan.Success.ShouldBeTrue();
}
