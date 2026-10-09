// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_refusing_invalid_options : Specification
{
    CratisPlanResult _result = null!;
    void Because() => _result = CratisRendering.PlanScaffold(new("Shop", "../Shop", "Acme.Shop"));
    [Fact] void should_report_invalid_application_options() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-PLAN-030");
    [Fact] void should_not_claim_success() => _result.Success.ShouldBeFalse();
}
