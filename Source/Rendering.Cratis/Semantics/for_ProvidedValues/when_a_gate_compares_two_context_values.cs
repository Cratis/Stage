// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_a_gate_compares_two_context_values : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = a_fleet_model.Plan(an_available_fleet_model.Source.Replace(
        "available when $context.region == \"Nordics\" and $context.limit >= 3",
        "available when $context.region == $context.region",
        StringComparison.Ordinal));

    [Fact] void should_refuse_the_model() => _plan.Success.ShouldBeFalse();
    [Fact] void should_refuse_the_condition() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-077" && diagnostic.Message.Contains("another context value", StringComparison.Ordinal)).ShouldBeTrue();
}
#endif
