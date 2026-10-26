// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;

/// <summary>
/// Restores the separate specifications project the scaffold emitted before specifications compiled into the
/// application in Debug, so the earlier frozen bytes remain a gate that only the project layout changed.
/// </summary>
internal static class SeparateSpecificationsLayout
{
    const string Prefix = "cratis-scaffold:text:";

    internal static ImmutableArray<ArtifactRenderInput> Restore(ImmutableArray<ArtifactRenderInput> inputs, string project, string? rootNamespace = null)
    {
        var restored = inputs.Select(input => Restore(input, project)).ToList();
        var version = inputs[0].Version;
        restored.Add(CratisArtifactRenderInput.CreateText($"{project}.Specs/{project}.Specs.csproj", version, SpecificationsProject(project, rootNamespace ?? project)));
        return [.. restored.OrderBy(input => input.Name, StringComparer.Ordinal)];
    }

    // Restores both the separate specifications project and the former license header in planned output.
    internal static IEnumerable<PlannedArtifact> RestoreWithLicenseHeader(IEnumerable<PlannedArtifact> artifacts, string project, string rootNamespace)
    {
        var restored = artifacts.Select(artifact =>
        {
            if (artifact.Kind != PlannedArtifactKind.Text) return artifact;
            var input = CratisArtifactRenderInput.CreateText(artifact.RelativePath, "1", Encoding.UTF8.GetString(artifact.Bytes.AsSpan()));
            var text = Encoding.UTF8.GetString(Restore(input, project).Bytes.AsSpan());
            text = for_CratisArtifactRenderPlanner.given.LegacyLicenseHeader.Restore(artifact.RelativePath, text);
            return PlannedArtifact.CreateText(artifact.RelativePath, text, artifact.Sources);
        }).ToList();
        restored.Add(PlannedArtifact.CreateText($"{project}.Specs/{project}.Specs.csproj", SpecificationsProject(project, rootNamespace)));
        return restored.OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal);
    }

    static ArtifactRenderInput Restore(ArtifactRenderInput input, string project)
    {
        var path = input.Name[Prefix.Length..];
        var text = Encoding.UTF8.GetString(input.Bytes.AsSpan());
        var restored = path switch
        {
            ".editorconfig" => text.Replace("[{**/when_*.cs,**/when_*/**/*.cs,", "[{**/*.Specs/**/*.cs,**/when_*.cs,", StringComparison.Ordinal),
            "README.md" => text
                .Replace($"dotnet test {project}.csproj\n", $"dotnet test {project}.Specs\n", StringComparison.Ordinal)
                .Replace(
                    "Specifications live beside the slices they specify, in `when_*.cs` files and\n`when_*/`, `for_*/` and `given/` folders. They compile into the application in\nDebug, which is also when its test packages are referenced. A Release build\nremoves them and needs no test packages.",
                    "The separate specs project links the generated slice-local `when_*.cs` files.\nSpecifications run in any configuration, including Release. The web project\nexcludes `when_*.cs` specifications in every configuration.",
                    StringComparison.Ordinal),
            _ when path == $"{project}.slnx" => $"<Solution>\n  <Project Path=\"{project}.csproj\" />\n  <Project Path=\"{project}.Specs/{project}.Specs.csproj\" />\n</Solution>\n",
            _ when path == $"{project}.csproj" => ApplicationProject(text, project),
            _ => text
        };

        return restored == text ? input : CratisArtifactRenderInput.CreateText(path, input.Version, restored);
    }

    static string ApplicationProject(string text, string project)
    {
        // The unconditional GenerateProgramFile setting arrived with the layout that compiles specifications into the application.
        text = text.Replace(
            "    <!-- Program.cs is the entry point; the test SDK must not generate another one. This holds in every\n" +
            "         configuration: a restore for Debug writes the test SDK's targets into the restore assets, which a\n" +
            "         later Release build without a restore still imports. -->\n" +
            "    <GenerateProgramFile>false</GenerateProgramFile>\n",
            string.Empty,
            StringComparison.Ordinal);
        var start = text.IndexOf("  <PropertyGroup Condition=\"'$(Configuration)' == 'Debug'\">", StringComparison.Ordinal);
        var end = text.IndexOf("  <ItemGroup>\n    <PackageReference Include=\"Cratis.Screenplay.Matching\" />", StringComparison.Ordinal);
        var debug = text.IndexOf("  <ItemGroup Condition=\"'$(Configuration)' == 'Debug'\">", StringComparison.Ordinal);
        var import = text.IndexOf("  <Import Project=", StringComparison.Ordinal);
        return text[..start] +
            $"""
              <ItemGroup>
                <InternalsVisibleTo Include="{project}.Specs" />
                <Compile Remove="**/when_*.cs;{project}.Specs/**" />
                <Content Remove="package.json;{project}.Specs/**" />
                <None Remove="package.json;{project}.Specs/**" />
                <EmbeddedResource Remove="{project}.Specs/**" />
              </ItemGroup>

            """ +
            text[end..debug] + text[import..];
    }

    static string SpecificationsProject(string project, string rootNamespace) =>
        $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <RootNamespace>{rootNamespace}.Specs</RootNamespace>
            <IsTestProject>true</IsTestProject>
            <IsPackable>false</IsPackable>
          </PropertyGroup>
          <ItemGroup>
            <ProjectReference Include="../{project}.csproj" />
            <Compile Include="../**/when_*.cs" Exclude="../{project}.Specs/**;../**/bin/**;../**/obj/**;../node_modules/**" Link="%(RecursiveDir)%(Filename)%(Extension)" />
          </ItemGroup>
          <ItemGroup>
            <PackageReference Include="Cratis.Arc.Chronicle.Testing" />
            <PackageReference Include="Cratis.Chronicle.Testing" />
            <PackageReference Include="Cratis.Specifications" />
            <PackageReference Include="Cratis.Specifications.XUnit" />
            <PackageReference Include="Microsoft.NET.Test.Sdk" />
            <PackageReference Include="NSubstitute" />
            <PackageReference Include="xunit" />
            <PackageReference Include="xunit.runner.visualstudio">
              <PrivateAssets>all</PrivateAssets>
              <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
            </PackageReference>
          </ItemGroup>
        </Project>

        """;
}
