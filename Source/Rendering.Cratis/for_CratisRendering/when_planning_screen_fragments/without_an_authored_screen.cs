// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_screen_fragments;

public class without_an_authored_screen : given.a_partitioned_screen_plan
{
    protected override string ModelSource => Source.Replace("      screen StaticOverview\n        title \"Static overview\"\n", string.Empty, StringComparison.Ordinal);

    void Because() => _result = CratisRendering.PlanFrom(_loaded, new([PlanSelectionEntry.Slice("Projects", "Registration", "RegisterProject")]), _options);

    [Fact] void should_admit_the_plan() => _result.Success.ShouldBeTrue();
    [Fact] void should_not_substitute_the_application_ui() => Fragments(_result.Artifacts).ShouldBeEmpty();
    [Fact] void should_explain_the_absent_slice_owned_ui() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-SCREEN-005").Artifact.ShouldEqual(Slice("RegisterProject").Id);
}
