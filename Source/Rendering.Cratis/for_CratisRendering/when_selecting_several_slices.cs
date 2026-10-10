// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_selecting_several_slices : given.a_source_plan
{
    void Because() => _result = CratisRendering.PlanFrom(_loaded, new([PlanSelectionEntry.Slice("Projects", "Registration", "RegisterProject"), PlanSelectionEntry.Slice("Tasks", "Backlog", "Intake", "CaptureTask")]), _planOptions);
    [Fact] void should_plan_successfully() => ShouldSucceed();
    [Fact] void should_include_both_commands() => Paths(_result).Count(path => path.EndsWith("/RegisterProject.cs", StringComparison.Ordinal) || path.EndsWith("/CaptureTask.cs", StringComparison.Ordinal)).ShouldEqual(2);
}
