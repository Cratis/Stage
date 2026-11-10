// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_planning_a_repository_without_a_profile_version : Specification
{
    CratisPlanResult _result = null!;

    void Because() => _result = CratisRendering.PlanRepository(new("Acme", ["Orders", "Billing"]));

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_target_net11() => Text("Directory.Build.props").ShouldContain("net11.0");
    [Fact] void should_not_target_net10() => Text("Directory.Build.props").ShouldNotContain("net10.0");

    string Text(string path) => Encoding.UTF8.GetString(_result.Artifacts.Single(artifact => artifact.RelativePath == path).Bytes.AsSpan());
}
