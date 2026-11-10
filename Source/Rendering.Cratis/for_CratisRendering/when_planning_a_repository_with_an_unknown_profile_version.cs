// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_planning_a_repository_with_an_unknown_profile_version : Specification
{
    CratisPlanResult _result = null!;

    void Because() => _result = CratisRendering.PlanRepository(new CratisRepositoryPlanOptions("Acme", ["Orders"]) { ProfileVersion = "99" });

    [Fact] void should_not_claim_success() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_unsupported_profile_version() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-PLAN-032");
    [Fact] void should_plan_no_artifacts() => _result.Artifacts.ShouldBeEmpty();
}
