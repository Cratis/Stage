// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_encoding;

public class and_the_language_refuses_the_body : given.a_body_planner
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = PlanBody(new() { ["templates/index.sbn"] = "{{ impl.body 'r1' }}" }, Requirement("r1", "refuse this"), "refuse this");

    [Fact] void should_have_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_the_language_code() => _plan.Diagnostics.Select(_ => _.Code).ShouldEqual([given.a_test_language.RefusedBody]);
}
