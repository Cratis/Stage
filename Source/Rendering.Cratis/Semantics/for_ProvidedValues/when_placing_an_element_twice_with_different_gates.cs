// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_placing_an_element_twice_with_different_gates : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = a_fleet_model.Plan(an_available_screen_element_model.Compile(conflicting: true));

    [Fact] void should_refuse_the_model() => _plan.Success.ShouldBeFalse();
    [Fact] void should_refuse_with_the_conflicting_gate_diagnostic() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == SemanticAvailabilityRendering.ConflictingElementGates).ShouldBeTrue();
}
#endif
