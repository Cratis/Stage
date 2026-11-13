// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_rendering_without_provided_values : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = a_fleet_model.Plan(a_fleet_model.Source
        .Replace("  provide region String from \"Nordics\"\n", string.Empty, StringComparison.Ordinal)
        .Replace("  provide operator String optional from host\n", string.Empty, StringComparison.Ordinal)
        .Replace("      provide vessel Vessel optional from query VesselByKey by $context.arguments.vesselId\n", string.Empty, StringComparison.Ordinal));

    [Fact] void should_render_successfully() => Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
    [Fact] void should_not_render_a_resolver() => _plan.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("ProvidedValues.cs", StringComparison.Ordinal)).ShouldBeFalse();
}
#endif
