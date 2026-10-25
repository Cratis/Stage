// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_shared_user_interface;

public class with_a_profile_carrying_the_input : given.a_partitioned_screen_plan
{
    void Because()
    {
        var profile = ArtifactRenderProfile.Create(_profile.Target, _profile.TargetVersion, _profile.Renderer, _profile.RendererVersion, [.. _profile.Inputs, UserInterfaceInput.Create([])]);
        _result = CratisRendering.PlanFrom(
            _loaded,
            new([PlanSelectionEntry.Slice("Projects", "Registration", "RegisterProject")]),
            _options with { Profile = profile, UserInterface = new([]) });
    }

    [Fact] void should_refuse_the_plan() => _result.Success.ShouldBeFalse();
    [Fact] void should_say_the_option_owns_the_input() => _result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("user interface input", StringComparison.Ordinal)).ShouldBeTrue();
}
