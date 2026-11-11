// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_planning_from_source_with_profile_versions : given.a_source_plan
{
    CratisPlanResult _version4 = null!;
    CratisPlanResult _version3 = null!;
    CratisPlanResult _unversioned = null!;

    async Task Because()
    {
        _version4 = await CratisRendering.PlanFrom(new PlaySources(_root, []), _featureSelection, new(_planOptions.ApplicationName, _planOptions.ProjectName, _planOptions.RootNamespace) { ProfileVersion = "4" });
        _version3 = await CratisRendering.PlanFrom(new PlaySources(_root, []), _featureSelection, new(_planOptions.ApplicationName, _planOptions.ProjectName, _planOptions.RootNamespace) { ProfileVersion = "3" });
        _unversioned = await CratisRendering.PlanFrom(new PlaySources(_root, []), _featureSelection, _planOptions);
    }

    [Fact] void should_admit_version_4() => _version4.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-CRATIS-001");
    [Fact] void should_produce_artifacts_for_version_4() => _version4.Artifacts.ShouldNotBeEmpty();
    [Fact] void should_plan_version_3_unchanged() => _version3.Artifacts.ShouldNotBeEmpty();
    [Fact] void should_plan_without_a_version_as_version_3() => _unversioned.Digest.ShouldEqual(_version3.Digest);
}
