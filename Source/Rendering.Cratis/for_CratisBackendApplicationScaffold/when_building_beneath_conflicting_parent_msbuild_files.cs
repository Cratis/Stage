// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_building_beneath_conflicting_parent_msbuild_files : Specification
{
    DirectoryInfo _root = null!;
    DirectoryInfo _application = null!;
    ProcessResult _debug = null!;
    ProcessResult _release = null!;

    void Establish()
    {
        _root = new(Path.Combine(Path.GetTempPath(), $"stage-scaffold-smoke-{Guid.NewGuid():N}"));
        _application = _root.CreateSubdirectory("application");
        File.WriteAllText(
            Path.Combine(_root.FullName, "Directory.Build.props"),
            """
            <Project>
              <Target Name="RejectNestedBuild" BeforeTargets="PrepareForBuild">
                <Error Text="The scaffold inherited conflicting parent build properties." />
              </Target>
            </Project>
            """);
        File.WriteAllText(
            Path.Combine(_root.FullName, "Directory.Build.targets"),
            """
            <Project>
              <Target Name="RejectNestedTargetBuild" BeforeTargets="PrepareForBuild">
                <Error Text="The scaffold inherited conflicting parent build targets." />
              </Target>
            </Project>
            """);
        File.WriteAllText(
            Path.Combine(_root.FullName, "Directory.Packages.props"),
            """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
            </Project>
            """);

        foreach (var input in new CratisBackendApplicationScaffold().Create(CratisBackendApplicationScaffoldRequest.Create("SmokeApp")))
        {
            File.WriteAllBytes(
                Path.Combine(_application.FullName, input.Name["cratis-scaffold:text:".Length..]),
                [.. input.Bytes]);
        }
    }

    async Task Because()
    {
        _debug = await Run("test", "SmokeApp.csproj", "-c", "Debug", "--nologo");
        _release = await Run("build", "SmokeApp.csproj", "-c", "Release", "--nologo");
    }

    void Destroy()
    {
        if (_root.Exists)
        {
            _root.Delete(recursive: true);
        }
    }

    [Fact] void should_test_the_debug_scaffold_successfully() => _debug.ExitCode.ShouldEqual(0);
    [Fact] void should_build_the_release_scaffold_successfully() => _release.ExitCode.ShouldEqual(0);
    [Fact] void should_not_import_the_conflicting_parent_build_properties() => $"{_debug.Output}{_release.Output}".ShouldNotContain("conflicting parent build properties");
    [Fact] void should_not_import_the_conflicting_parent_build_targets() => $"{_debug.Output}{_release.Output}".ShouldNotContain("conflicting parent build targets");

    async Task<ProcessResult> Run(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = _application.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }

        var output = $"{await standardOutput}{await standardError}";
        return new(process.ExitCode, output);
    }

    sealed record ProcessResult(int ExitCode, string Output);
}
