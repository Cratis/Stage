// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisPlanOptions;

public class when_selecting_gap_modes : Specification
{
    CratisPlanOptions _plan = null!;
    CratisRenderingOptions _rendering = null!;
    void Because()
    {
        _plan = new("Projects", "Projects", "Projects");
        _rendering = new("Projects", "Projects");
    }

    [Fact] void should_default_source_planning_to_refuse() => _plan.ImplementationGaps.ShouldEqual(ImplementationGapMode.Refuse);
    [Fact] void should_default_rendering_to_refuse() => _rendering.ImplementationGaps.ShouldEqual(ImplementationGapMode.Refuse);
    [Fact] void should_require_explicit_stub_selection() => (_plan with { ImplementationGaps = ImplementationGapMode.Stub }).ImplementationGaps.ShouldEqual(ImplementationGapMode.Stub);
    [Fact] void should_preserve_the_default_profile_bytes() => string.Join('\n', CratisRendering.CreateProfile("Projects", _rendering).Inputs.Select(input => input.Sha256))
        .ShouldEqual(string.Join('\n', CratisRendering.CreateProfile("Projects", _rendering with { ImplementationGaps = ImplementationGapMode.Refuse }).Inputs.Select(input => input.Sha256)));
    [Fact] void should_not_activate_the_new_profile_input_in_pr_a() => CratisRendering.CreateProfile("Projects", _rendering).Inputs.Any(input => input.Name == ImplementationGapInput.Name).ShouldBeFalse();
}
