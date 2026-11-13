// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_queries_of_one_read_model_need_different_gates : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = a_fleet_model.Plan(an_available_fleet_model.Source.Replace(
        "      readmodel Vessel\n",
        "      query VesselByOtherKey => Vessel optional\n        by vesselId Uuid\n\n      readmodel Vessel\n",
        StringComparison.Ordinal));

    [Fact] void should_refuse_the_model() => _plan.Success.ShouldBeFalse();
    [Fact] void should_refuse_the_gates() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-078").ShouldBeTrue();
}
#endif
