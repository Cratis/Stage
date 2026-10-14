// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_screen_fragments;

public class with_a_cross_slice_command : given.a_partitioned_screen_plan
{
    void Because() => _result = CratisRendering.PlanFrom(_loaded, new([PlanSelectionEntry.Slice("Projects", "Registration", "ProjectLookup")]), _options);

    [Fact] void should_admit_the_plan() => _result.Success.ShouldBeTrue();
    [Fact] void should_keep_the_bound_command_in_the_screen() => Text(Fragments(_result.Artifacts).Single()).ShouldContain("RegisterProject");
    [Fact] void should_attribute_only_the_declaring_slice() => Fragments(_result.Artifacts).Single().Sources.ShouldContainOnly(Slice("ProjectLookup").Id);
    [Fact] void should_not_include_the_screen_declared_by_the_command_slice() => Text(Fragments(_result.Artifacts).Single()).ShouldNotContain("StaticOverview");
    [Fact] void should_emit_the_declaring_slices_business_file() => _result.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("/ProjectLookup.cs", StringComparison.Ordinal)).ShouldBeTrue();
}
