// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_selecting_a_feature_and_a_sibling_slice : given.a_source_plan
{
    void Because() => _result = CratisRendering.PlanFrom(_loaded, new([PlanSelectionEntry.Feature("Projects", "Registration"), PlanSelectionEntry.Slice("Tasks", "Backlog", "Intake", "CaptureTask")]), _planOptions);
    [Fact] void should_plan_successfully() => ShouldSucceed();
    [Fact] void should_include_the_feature_and_the_other_slice() => Paths(_result).ShouldContain("Tasks/Backlog/Intake/CaptureTask/CaptureTask.cs");
    [Fact] void should_include_the_feature_read_model() => Paths(_result).ShouldContain("Projects/Registration/ProjectLookup/ProjectLookup.cs");
}
