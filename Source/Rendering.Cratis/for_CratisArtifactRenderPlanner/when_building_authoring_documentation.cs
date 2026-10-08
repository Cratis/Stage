// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_building_authoring_documentation : a_generated_application
{
    string _warnings = null!;

    protected override ArtifactRenderPlan CreatePlan() => when_rendering_authoring_documentation.Plan(
        when_rendering_authoring_documentation.Compile(when_rendering_authoring_documentation.Source));

    async Task Because() => _warnings = BuildWarnings(await Run(
        "documentation-debug-build.log",
        "build",
        "Projects.csproj",
        "--configuration",
        "Debug",
        "--nologo",
        "-warnaserror",
        "-p:TreatWarningsAsErrors=true",
        "-p:CodeAnalysisTreatWarningsAsErrors=true",
        "-p:MSBuildTreatWarningsAsErrors=true",
        "-p:CratisProxiesOutputPath="));

    [Fact] void should_build_documented_declarations_without_warnings() => _warnings.ShouldEqual(string.Empty);
}
#endif
