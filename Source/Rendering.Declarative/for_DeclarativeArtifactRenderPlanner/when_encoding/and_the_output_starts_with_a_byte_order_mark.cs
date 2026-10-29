// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_encoding;

public class and_the_output_starts_with_a_byte_order_mark : given.a_body_planner
{
    const string Body = "\uFEFFbody";

    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = PlanBody(new() { ["templates/index.sbn"] = "{{ impl.body 'r1' }}" }, Requirement("r1", Body), Body);

    [Fact] void should_have_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_the_text_refusal() => _plan.Diagnostics.Select(_ => (_.Code, _.Severity)).ShouldEqual([(DeclarativeCodes.OutputText, ArtifactRenderDiagnosticSeverity.Error)]);
    [Fact] void should_not_succeed() => _plan.Success.ShouldBeFalse();
}
