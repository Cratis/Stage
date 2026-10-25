// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_shared_user_interface;

public class with_an_unknown_slice : given.a_partitioned_screen_plan
{
    void Because() => _result = CratisRendering.PlanFrom(
        _loaded,
        new([PlanSelectionEntry.Slice("Projects", "Registration", "RegisterProject")]),
        _options with { UserInterface = new([PlanSelectionEntry.Slice("Projects", "Registration", "Missing")]) });

    [Fact] void should_refuse_the_plan() => _result.Success.ShouldBeFalse();
    [Fact] void should_plan_nothing() => _result.Artifacts.ShouldBeEmpty();
    [Fact] void should_say_the_user_interface_selection_is_wrong() => _result.Diagnostics.Any(diagnostic => diagnostic.Message.StartsWith("User interface selection: ", StringComparison.Ordinal) && diagnostic.Message.Contains("'Missing'", StringComparison.Ordinal)).ShouldBeTrue();
}
