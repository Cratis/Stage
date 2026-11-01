// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Specs;
using Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_RealizedApplication;

/// <summary>
/// The files rendered for authentication and onboarding are real C# against the real Cratis, Chronicle, Herald and Ante packages: the
/// application builds in Debug and Release without a warning.
/// </summary>
public class when_building_the_application : given.a_model_with_authentication_and_onboarding
{
    ProcessResult _debug = null!;
    ProcessResult _release = null!;
    string _application = null!;

    async Task Because()
    {
        _application = Directory.CreateDirectory(SpecTemporaryRoot.NewPath("stage-realized-build")).FullName;
        foreach (var artifact in _plan.Artifacts)
        {
            var path = Path.Combine(_application, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, [.. artifact.Bytes]);
        }

        var project = $"{Application}.csproj";
        _debug = await ApplicationProcess.Run(_application, TimeSpan.FromMinutes(8), "dotnet", "build", project, "-c", "Debug", "--nologo");
        _release = await ApplicationProcess.Run(_application, TimeSpan.FromMinutes(8), "dotnet", "build", project, "-c", "Release", "--nologo");
    }

    void Destroy() => SpecTemporaryRoot.Delete(_application);

    [Fact] void should_build_in_debug_without_warnings() => Failures(_debug).ShouldEqual(string.Empty);
    [Fact] void should_build_in_release_without_warnings() => Failures(_release).ShouldEqual(string.Empty);
    [Fact] void should_pin_the_chronicle_client_ante_needs() => File.ReadAllText(Path.Combine(_application, "Directory.Packages.props")).ShouldContain("Cratis.Chronicle\" Version=\"19.114.0\"");
    [Fact] void should_run_the_matching_chronicle_kernel_locally() => File.ReadAllText(Path.Combine(_application, "docker-compose.yml")).ShouldContain("cratis/chronicle:19.114.0-development");
    [Fact] void should_copy_the_email_templates_beside_the_application() => File.ReadAllText(Path.Combine(_application, $"{Application}.csproj")).ShouldContain("Emails/**");

    static string Failures(ProcessResult result) => result.ExitCode == 0 && !result.Output.Contains("warning ", StringComparison.Ordinal) ? string.Empty : result.Output;
}
