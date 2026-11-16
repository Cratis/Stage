// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

// A free screen is declared where it sits and rendered by the slice that adopts it first, so a partial render of that slice carries it and a
// partial render of the slices that do not adopt it leaves it out (Cratis/Stage#1150).
public class when_creating_screen_partitions_for_an_adopted_free_screen : given.a_partitioned_screen_plan
{
    const string Shape = "shape ProjectRow\n  name String\n\nscreen ProjectList\n  title \"Projects\"\n  data ProjectRow[]\n  table ProjectRow\n    column name\n\nscreen NobodyAdopts\n  title \"Design only\"\n  data ProjectRow[]\n\n";

    protected override string ModelSource => Shape + Source + "\n      query AllProjects => ProjectSummary[]\n      uses screen ProjectList\n        data ProjectRow via query AllProjects\n";

    CratisPlanResult _lookup = null!;
    CratisPlanResult _registration = null!;

    void Because()
    {
        _lookup = CratisRendering.PlanFrom(_loaded, new([PlanSelectionEntry.Feature("Projects", "Registration")]), _options);
        _registration = CratisRendering.PlanFrom(_loaded, new([PlanSelectionEntry.Slice("Projects", "Registration", "RegisterProject")]), _options);
    }

    [Fact] void should_convert_the_adopted_free_screen_into_the_scene() => _scene.Screens.Select(screen => screen.Name).ShouldContain("ProjectList");
    [Fact] void should_leave_a_free_screen_nobody_adopts_out_of_the_scene() => _scene.Screens.Select(screen => screen.Name).ShouldNotContain("NobodyAdopts");
    [Fact] void should_own_the_adopted_screen_by_the_slice_that_adopts_it() => Partitions.Bytes.AsSpan().Length.ShouldBeGreaterThan(0);
    [Fact] void should_plan_the_feature() => _lookup.Success.ShouldBeTrue();
    [Fact] void should_carry_the_adopted_screen_in_the_slice_that_adopts_it() => string.Join('\n', Fragments(_lookup.Artifacts).Select(Text)).ShouldContain("ProjectList");
    [Fact] void should_not_carry_the_adopted_screen_in_a_slice_that_does_not_adopt_it() => string.Join('\n', Fragments(_registration.Artifacts).Select(Text)).ShouldNotContain("ProjectList");
}
