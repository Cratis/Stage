// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// Creates the deterministic in-memory inputs for a first-run Cratis backend application.
/// </summary>
public sealed class CratisBackendApplicationScaffold
{
    /// <summary>
    /// Creates the complete backend application scaffold without writing to a file system.
    /// </summary>
    /// <param name="request">The validated scaffold request.</param>
    /// <returns>Eight normalized UTF-8 text inputs in ordinal relative-path order.</returns>
    /// <exception cref="InvalidCratisBackendApplicationScaffold">Thrown when the request is missing.</exception>
    public ImmutableArray<ArtifactRenderInput> Create(CratisBackendApplicationScaffoldRequest request)
    {
        if (request is null)
        {
            throw new InvalidCratisBackendApplicationScaffold("A backend application scaffold requires a request.");
        }

        var profile = request.Profile;
        var artifacts = new (string RelativePath, string Content)[]
        {
            ("Directory.Build.props", DirectoryBuildProps()),
            ("Directory.Build.targets", DirectoryBuildTargets()),
            ("Directory.Packages.props", DirectoryPackagesProps()),
            ($"{request.ProjectName}.csproj", Project(request)),
            ($"{request.ProjectName}.slnx", Solution(request)),
            ("Program.cs", Program()),
            ("appsettings.json", AppSettings(request)),
            ("docker-compose.yml", DockerCompose(profile))
        };

        return
        [
            .. artifacts
                .OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal)
                .Select(artifact => Input(artifact.RelativePath, profile.Version, artifact.Content))
        ];
    }

    static ArtifactRenderInput Input(string relativePath, string version, string content)
    {
        var withSingleTrailingLineFeed = $"{content.TrimEnd('\r', '\n')}\n";
        return CratisArtifactRenderInput.CreateText(relativePath, version, withSingleTrailingLineFeed);
    }

    static string DirectoryBuildProps() =>
        """
        <Project />
        """;

    static string DirectoryBuildTargets() =>
        """
        <Project />
        """;

    static string DirectoryPackagesProps() =>
        """
        <Project>
          <PropertyGroup>
            <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
          </PropertyGroup>
        </Project>
        """;

    static string Solution(CratisBackendApplicationScaffoldRequest request) =>
        $$"""
        <Solution>
          <Project Path="{{request.ProjectName}}.csproj" />
        </Solution>
        """;

    static string Project(CratisBackendApplicationScaffoldRequest request)
    {
        var profile = request.Profile;
        return $$"""
        <Project Sdk="Microsoft.NET.Sdk.Web">
          <PropertyGroup>
            <TargetFramework>{{profile.TargetFramework}}</TargetFramework>
            <RootNamespace>{{request.RootNamespace}}</RootNamespace>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
            <IsPackable>false</IsPackable>
            <IsTestProject Condition="'$(Configuration)' == 'Debug'">true</IsTestProject>
            <NoWarn Condition="'$(Configuration)' == 'Debug'">$(NoWarn);CS7022</NoWarn>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Cratis" Version="{{profile.CratisPackageVersion}}" />
            <PackageReference Include="Cratis.Arc.MongoDB" Version="{{profile.CratisArcMongoDBPackageVersion}}" />
          </ItemGroup>
          <ItemGroup Condition="'$(Configuration)' == 'Debug'">
            <PackageReference Include="Cratis.Arc.Chronicle.Testing" Version="{{profile.CratisArcChronicleTestingPackageVersion}}" />
            <PackageReference Include="Cratis.Specifications" Version="{{profile.CratisSpecificationsPackageVersion}}" />
            <PackageReference Include="Cratis.Specifications.XUnit" Version="{{profile.CratisSpecificationsXUnitPackageVersion}}" />
            <PackageReference Include="Microsoft.NET.Test.Sdk" Version="{{profile.MicrosoftNetTestSdkPackageVersion}}" />
            <PackageReference Include="NSubstitute" Version="{{profile.NSubstitutePackageVersion}}" />
            <PackageReference Include="xunit" Version="{{profile.XunitPackageVersion}}" />
            <PackageReference Include="xunit.runner.visualstudio" Version="{{profile.XunitRunnerVisualStudioPackageVersion}}">
              <PrivateAssets>all</PrivateAssets>
              <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
            </PackageReference>
          </ItemGroup>
        </Project>
        """;
    }

    static string Program() =>
        """
        // Copyright (c) Cratis. All rights reserved.
        // Licensed under the MIT license. See LICENSE file in the project root for full license information.

        using Cratis.Arc.MongoDB;

        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddHealthChecks();
        builder.AddCratis(
            configureArcBuilder: arc => arc.WithMongoDB(),
            configureChronicleBuilder: chronicle => chronicle.WithCamelCaseNamingPolicy());

        var app = builder.Build();
        app.UseCratis();
        app.MapHealthChecks("/healthz");

        await app.RunAsync();
        """;

    static string AppSettings(CratisBackendApplicationScaffoldRequest request) =>
        $$"""
        {
          "Cratis": {
            "Arc": {
              "GeneratedApis": {
                "RoutePrefix": "api",
                "IncludeCommandNameInRoute": false,
                "SegmentsToSkipForRoute": 1
              }
            },
            "Chronicle": {
              "EventStore": "{{request.ApplicationName}}",
              "ConnectionString": "chronicle://chronicle-dev-client:chronicle-dev-secret@localhost:35000"
            },
            "MongoDB": {
              "Server": "mongodb://localhost:27017",
              "Database": "{{request.ApplicationName}}"
            }
          }
        }
        """;

    static string DockerCompose(CratisBackendApplicationScaffoldProfile profile) =>
        $$"""
        services:
          chronicle:
            image: cratis/chronicle:{{profile.ChronicleImageVersion}}-development
            ports:
              - "27017:27017"
              - "35000:35000"
        """;
}
