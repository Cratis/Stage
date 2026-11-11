// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_planning_a_repository_with_the_retained_profile : Specification
{
    CratisPlanResult _result = null!;
    CratisPlanResult _expected = null!;

    void Because()
    {
        var options = new CratisRepositoryPlanOptions("Acme", ["Orders", "Billing"]) { ProfileVersion = "3" };
        _result = CratisRendering.PlanRepository(options);
        _expected = CratisPlanResult.Create("Acme", [.. CratisRepositoryScaffold.Render(options, CratisBackendApplicationScaffoldProfile.Version3)], []);
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_render_exactly_what_the_version_3_profile_renders() => _result.Digest.ShouldEqual(_expected.Digest);
    [Fact] void should_target_net10() => Text("Directory.Build.props").ShouldContain("net10.0");

    string Text(string path) => Encoding.UTF8.GetString(_result.Artifacts.Single(artifact => artifact.RelativePath == path).Bytes.AsSpan());
}
