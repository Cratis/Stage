// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_planning_from_a_single_file : given.a_source_plan
{
    async Task Because() => _result = await From("projects.play");
    [Fact] void should_plan_successfully() => ShouldSucceed();
    [Fact] void should_include_the_selected_command() => Paths(_result).ShouldContain("Projects/Registration/RegisterProject/RegisterProject.cs");
    [Fact] void should_expose_the_underlying_plan_digest() => _result.Digest.ShouldEqual(_result.Plan!.Digest);
    [Fact] void should_preserve_compiler_information() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == "PLAY0479").Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Information);
    [Fact] void should_include_compiler_information_in_the_underlying_plan() => _result.Plan!.Diagnostics.Single(diagnostic => diagnostic.Code == "PLAY0479").Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Information);
}
