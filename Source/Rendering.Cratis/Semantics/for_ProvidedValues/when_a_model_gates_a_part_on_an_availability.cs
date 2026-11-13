// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_a_model_gates_a_part_on_an_availability : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = a_fleet_model.Plan(("audience Beta\n  description \"Tenants in the beta\"\n\n" + a_fleet_model.Source).Replace("  feature Vessels", "  feature Vessels\n    available for Beta", StringComparison.Ordinal));

    [Fact] void should_refuse_the_model() => _plan.Success.ShouldBeFalse();
    [Fact] void should_say_why() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-016" && diagnostic.Message.Contains("", StringComparison.Ordinal)).ShouldBeTrue();
}
#endif
