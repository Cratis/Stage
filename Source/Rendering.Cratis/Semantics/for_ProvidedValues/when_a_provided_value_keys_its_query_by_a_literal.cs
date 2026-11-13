// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_a_provided_value_keys_its_query_by_a_literal : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = a_fleet_model.Plan(a_fleet_model.Source.Replace("by $context.arguments.vesselId", "by \"abc\"", StringComparison.Ordinal));

    [Fact] void should_refuse_the_model() => _plan.Success.ShouldBeFalse();
    [Fact] void should_say_why() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-075" && diagnostic.Message.Contains("must key its query", StringComparison.Ordinal)).ShouldBeTrue();
}
#endif
