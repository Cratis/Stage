// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_refusing_a_domain_for_scaffold_only : Specification
{
    CratisPlanResult _result = null!;
    void Because() => _result = CratisRendering.PlanScaffold(new("Shop", "Shop", "Shop") { Domain = "Sales" });
    [Fact] void should_report_the_invalid_mode() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-PLAN-022");
    [Fact] void should_not_render_any_artifacts() => _result.Artifacts.ShouldBeEmpty();
}
