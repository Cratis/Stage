// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_the_domain_is_invalid : given.a_source_plan
{
    void Because() => _result = CratisRendering.PlanFrom(_loaded, _featureSelection, _planOptions with { Domain = "Sales/../Outside" });
    [Fact] void should_refuse_traversal() => ShouldRefuse("STAGE-PLAN-020");
    [Fact] void should_not_render_anything() => _result.Artifacts.ShouldBeEmpty();
}
