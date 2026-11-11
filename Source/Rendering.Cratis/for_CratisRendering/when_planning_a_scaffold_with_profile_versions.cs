// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_planning_a_scaffold_with_profile_versions : Specification
{
    CratisPlanResult _retained = null!;
    CratisPlanResult _fresh = null!;
    CratisPlanResult _explicit4 = null!;
    CratisPlanResult _unknown = null!;

    void Because()
    {
        _retained = CratisRendering.PlanScaffold(new("Shop", "Shop.Backend", "Acme.Shop") { ProfileVersion = "3" });
        _fresh = CratisRendering.PlanScaffold(new("Shop", "Shop.Backend", "Acme.Shop"));
        _explicit4 = CratisRendering.PlanScaffold(new("Shop", "Shop.Backend", "Acme.Shop") { ProfileVersion = "4" });
        _unknown = CratisRendering.PlanScaffold(new("Shop", "Shop.Backend", "Acme.Shop") { ProfileVersion = "99" });
    }

    [Fact] void should_target_net10_for_the_retained_version() => Text(_retained, "Directory.Build.props").ShouldContain("net10.0");
    [Fact] void should_target_net10_for_a_fresh_application() => Text(_fresh, "Directory.Build.props").ShouldContain("net10.0");
    [Fact] void should_target_net11_for_an_explicit_version_4() => Text(_explicit4, "Directory.Build.props").ShouldContain("net11.0");
    [Fact] void should_refuse_an_unknown_version() => _unknown.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-PLAN-032");
    [Fact] void should_plan_nothing_for_an_unknown_version() => _unknown.Success.ShouldBeFalse();

    static string Text(CratisPlanResult result, string path) => Encoding.UTF8.GetString(result.Artifacts.Single(artifact => artifact.RelativePath == path).Bytes.AsSpan());
}
