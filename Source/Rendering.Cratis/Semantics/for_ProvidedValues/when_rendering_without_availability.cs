// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_rendering_without_availability : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = a_fleet_model.Plan(a_fleet_model.Source);

    [Fact] void should_render_successfully() => Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
    [Fact] void should_not_generate_the_availability_registration() => _plan.Artifacts.Any(artifact => artifact.RelativePath.Contains("GeneratedAvailability/", StringComparison.Ordinal)).ShouldBeFalse();
}
#endif
