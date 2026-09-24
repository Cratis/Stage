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
    /// <returns>Nine normalized UTF-8 text inputs in ordinal relative-path order.</returns>
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
            ("Program.cs", Program(request)),
            ("GeneratedPolicyRegistration.cs", PolicyRegistration(request)),
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
            <CratisProxiesOutputPath>$(MSBuildThisFileDirectory)</CratisProxiesOutputPath>
            <CratisProxiesSegmentsToSkip>1</CratisProxiesSegmentsToSkip>
            <CratisProxiesSkipOutputDeletion>true</CratisProxiesSkipOutputDeletion>
            <CratisProxiesSkipCommandNameInRoute>true</CratisProxiesSkipCommandNameInRoute>
            <CratisProxiesUseSourceFileAsOutputFile>true</CratisProxiesUseSourceFileAsOutputFile>
          </PropertyGroup>
          <ItemGroup>
            <Content Remove="package.json" />
            <None Remove="package.json" />
          </ItemGroup>
          <ItemGroup>
            <PackageReference Include="Cratis" Version="{{profile.CratisPackageVersion}}" />
            <PackageReference Include="Cratis.Arc.MongoDB" Version="{{profile.CratisArcMongoDBPackageVersion}}" />
            <PackageReference Include="Cratis.Chronicle" Version="{{profile.CratisChroniclePackageVersion}}" />
            <PackageReference Include="Cratis.Chronicle.AspNetCore" Version="{{profile.CratisChroniclePackageVersion}}" />
          </ItemGroup>
          <ItemGroup Condition="'$(Configuration)' == 'Debug'">
            <PackageReference Include="Cratis.Arc.Chronicle.Testing" Version="{{profile.CratisArcChronicleTestingPackageVersion}}" />
            <PackageReference Include="Cratis.Chronicle.Testing" Version="{{profile.CratisChroniclePackageVersion}}" />
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
          <Import Project="Customizations/Dependencies.props" Condition="Exists('Customizations/Dependencies.props')" />
        </Project>
        """;
    }

    static string Program(CratisBackendApplicationScaffoldRequest request) =>
        $$"""
        // Copyright (c) Cratis. All rights reserved.
        // Licensed under the MIT license. See LICENSE file in the project root for full license information.

        using Cratis.Arc.MongoDB;

        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddHealthChecks();
        builder.AddCratis(
            configureArcBuilder: arc => arc.WithMongoDB(),
            configureChronicleBuilder: chronicle => chronicle.WithCamelCaseNamingPolicy());
        {{request.RootNamespace}}.GeneratedPolicies.Registration.Register(builder.Services);
        ConfigureServices(builder);

        var app = builder.Build();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseCratis();
        app.MapHealthChecks("/healthz");
        app.MapFallbackToFile("/index.html");
        ConfigureApplication(app);

        await app.RunAsync();

        // Optional partial methods disappear when Customizations/Program.cs supplies no implementation.
        public partial class Program
        {
            static partial void ConfigureServices(WebApplicationBuilder builder);
            static partial void ConfigureApplication(WebApplication app);
        }
        """;

    static string PolicyRegistration(CratisBackendApplicationScaffoldRequest request) =>
        $$"""
        // Copyright (c) Cratis. All rights reserved.
        // Licensed under the MIT license. See LICENSE file in the project root for full license information.

        using Microsoft.Extensions.DependencyInjection;

        namespace {{request.RootNamespace}}.GeneratedPolicies;

        /// <summary>
        /// Registers the policies generated from the Screenplay application.
        /// </summary>
        public static partial class Registration
        {
            /// <summary>
            /// Registers generated policies when the application contains protected operations.
            /// </summary>
            /// <param name="services">The application's services.</param>
            public static void Register(IServiceCollection services) => RegisterGenerated(services);

            static partial void RegisterGenerated(IServiceCollection services);
        }
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

    // The development image keeps its bundled MongoDB data in anonymous volumes, which a recreated container does not
    // get back: `docker compose down` followed by `up` would start from an empty store. Named volumes make the
    // lifecycle explicit - data survives restarts, recreation and regeneration, and only `down --volumes` removes it.
    static string DockerCompose(CratisBackendApplicationScaffoldProfile profile) =>
        $$"""
        services:
          chronicle:
            image: cratis/chronicle:{{profile.ChronicleImageVersion}}-development
            ports:
              - "27017:27017"
              - "35000:35000"
            volumes:
              - chronicle-data:/data/db
              - chronicle-config:/data/configdb

        volumes:
          chronicle-data:
          chronicle-config:
        """;
}
