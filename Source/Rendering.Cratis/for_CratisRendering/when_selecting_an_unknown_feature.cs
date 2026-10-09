// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_selecting_an_unknown_feature : given.a_source_plan
{
    void Because() => _result = CratisRendering.PlanFrom(_loaded, new([PlanSelectionEntry.Feature("Tasks", "Backlog", "Unknown")]), _planOptions);
    [Fact] void should_refuse_the_missing_sub_feature() => ShouldRefuse("STAGE-PLAN-012");
    [Fact] void should_not_render_anything() => _result.Artifacts.ShouldBeEmpty();
}
