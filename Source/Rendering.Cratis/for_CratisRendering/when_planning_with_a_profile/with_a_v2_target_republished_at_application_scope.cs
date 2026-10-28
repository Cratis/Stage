// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_with_a_profile;

public class with_a_v2_target_republished_at_application_scope : given.a_republished_application_plan
{
    // A v2 target holds every planned file but the scaffold-owned GlobalUsings.cs, which v3 introduced.
    protected override string? Existing(string path, string planned) => path == GlobalUsingsPath ? null : planned;

    [Fact] void should_plan_successfully() => _full.Success.ShouldBeTrue();
    [Fact] void should_plan_the_global_usings() => _full.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain(GlobalUsingsPath);
    [Fact] void should_be_compatible() => _check.ShouldBeOfExactType<CratisPublicationCheck.Compatible>();
    [Fact] void should_not_read_the_global_usings_from_the_target() => _readPaths.Contains(GlobalUsingsPath).ShouldBeFalse();
    [Fact] void should_compile_with_only_the_planned_global_usings() => string.Join(Environment.NewLine, _errors).ShouldEqual(string.Empty);
}
