// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_encoding;

public class and_the_body_does_not_match_its_content_hash : given.a_body_planner
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = PlanBody(new() { ["templates/index.sbn"] = "{{ impl.body 'r1' }}" }, Requirement("r1", "recorded body"), "tampered body");

    [Fact] void should_have_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_the_integrity_refusal() => _plan.Diagnostics.Select(_ => (_.Code, _.Severity)).ShouldEqual([(DeclarativeCodes.BodyIntegrity, ArtifactRenderDiagnosticSeverity.Error)]);
    [Fact] void should_not_succeed() => _plan.Success.ShouldBeFalse();
}
