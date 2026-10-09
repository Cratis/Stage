// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_selecting_a_sub_feature : given.a_source_plan
{
    void Because() => _result = CratisRendering.PlanFrom(_loaded, new([PlanSelectionEntry.Feature("Tasks", "Backlog", "Intake")]), _planOptions);
    [Fact] void should_plan_successfully() => ShouldSucceed();
    [Fact] void should_preserve_nested_placement() => Paths(_result).ShouldContain("Tasks/Backlog/Intake/CaptureTask/CaptureTask.cs");
    [Fact] void should_exclude_the_other_module() => Paths(_result).Any(path => path.StartsWith("Projects/", StringComparison.Ordinal)).ShouldBeFalse();
}
