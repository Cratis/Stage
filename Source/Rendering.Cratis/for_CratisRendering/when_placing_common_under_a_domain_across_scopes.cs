// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_placing_common_under_a_domain_across_scopes : given.a_source_plan
{
    CratisPlanResult _featurePlan = null!;
    void Establish() => _featurePlan = CratisRendering.PlanFrom(_loaded, _featureSelection, _planOptions with { Domain = "Sales/Retail" });
    void Because() => _result = CratisRendering.PlanFrom(_loaded, _sliceSelection, _planOptions with { Domain = "Sales/Retail" });
    [Fact] void should_plan_successfully() => ShouldSucceed();
    [Fact] void should_render_referenced_common_artifacts() => _result.Artifacts.Where(artifact => artifact.RelativePath.StartsWith("Sales/Retail/Common/", StringComparison.Ordinal)).ShouldNotBeEmpty();
    [Fact] void should_preserve_common_bytes_across_scopes() => _result.Artifacts.Where(artifact => artifact.RelativePath.StartsWith("Sales/Retail/Common/", StringComparison.Ordinal)).All(artifact => artifact.Bytes.SequenceEqual(_featurePlan.Artifacts.Single(expected => expected.RelativePath == artifact.RelativePath).Bytes)).ShouldBeTrue();
}
