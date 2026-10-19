// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Stage.Rendering.Cratis.given;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog;

/// <summary>
/// Writes the current backend scaffold to a disposable directory, restores and builds it, and asks MSBuild what each
/// project compiles against. Restoring the scaffold itself, rather than reading the versions Stage builds with, is what
/// makes the catalog describe the packages a generated application really gets.
/// </summary>
static class CatalogScaffold
{
    /// <summary>
    /// The project name the catalog scaffold is written with. Names derived from it are not recorded.
    /// </summary>
    public const string ProjectName = "NameCatalogApp";

    /// <summary>
    /// The audited framework reference pack version used by catalog generation, independent of the installed SDK.
    /// </summary>
    public const string ReferencePackVersion = PureTransitionAdmission.ReferencePackVersion;

    const string GeneratedSources = "obj/name-catalog-generated";

    /// <summary>
    /// Scaffolds, restores and builds the application and reads both projects.
    /// </summary>
    /// <param name="directory">The empty directory to scaffold into.</param>
    /// <returns>The application project and the specifications project.</returns>
    public static async Task<(ScaffoldedProject Application, ScaffoldedProject Specifications)> Restore(DirectoryInfo directory)
    {
        var request = CratisBackendApplicationScaffoldRequest.Create(ProjectName, ProjectName, ProjectName, CratisBackendApplicationScaffoldProfile.Current);
        foreach (var input in new CratisBackendApplicationScaffold().Create(request))
        {
            var path = Path.Combine(directory.FullName, input.Name["cratis-scaffold:text:".Length..]);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, [.. input.Bytes]);
        }

        // A newer SDK can substitute framework ref assemblies for package lib assemblies of the same version.
        // Their public APIs agree, but implementation-only namespaces still participate in C# namespace lookup.
        // Pin both packs so this scaffold's compiler references are stable. Namespace generation also unions
        // package compile/runtime libraries, so changing this pin cannot erase namespaces present on older SDKs.
        await File.WriteAllTextAsync(Path.Combine(directory.FullName, "Directory.Build.targets"),
            $"""
            <Project>
              <ItemGroup>
                <KnownFrameworkReference Update="Microsoft.NETCore.App;Microsoft.AspNetCore.App">
                  <TargetingPackVersion Condition="'%(KnownFrameworkReference.TargetFramework)' == '{request.Profile.TargetFramework}'">{ReferencePackVersion}</TargetingPackVersion>
                </KnownFrameworkReference>
              </ItemGroup>
            </Project>
            """);
        await Run(directory.FullName, "restore", $"{ProjectName}.slnx");
        await Run(directory.FullName, "build", $"{ProjectName}.slnx", "--no-restore", "-p:EmitCompilerGeneratedFiles=true", $"-p:CompilerGeneratedFilesOutputPath={GeneratedSources}");
        var specifications = Path.Combine(directory.FullName, $"{ProjectName}.Specs");

        return (
            await Read(GeneratedProjects.Application, directory.FullName, $"{ProjectName}.csproj"),
            await Read(GeneratedProjects.Specifications, specifications, $"{ProjectName}.Specs.csproj"));
    }

    static async Task<ScaffoldedProject> Read(GeneratedProjects project, string directory, string file)
    {
        var items = await Run(
            directory,
            "msbuild",
            file,
            "-nologo",
            "-t:FindReferenceAssembliesForReferences",
            "-getItem:ReferencePathWithRefAssemblies",
            "-getItem:Using",
            "-getItem:Compile");
        return ScaffoldedProject.Read(project, directory, items, Path.Combine(directory, GeneratedSources));
    }

    static async Task<string> Run(string directory, params string[] arguments)
    {
        // Bounded so a local run does not start every generated application's build at once.
        using var slot = await GeneratedApplicationBuilds.Enter();
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        // Reused MSBuild nodes outlive the command and keep the redirected pipes open.
        start.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        start.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(8));
        if (process.ExitCode != 0)
        {
            throw new CatalogScaffoldFailed($"dotnet {string.Join(' ', arguments)}", $"{await output}{await error}");
        }

        return await output;
    }
}
