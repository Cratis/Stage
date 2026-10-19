// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// Creates the deterministic in-memory inputs for a first-run Cratis backend application.
/// </summary>
public sealed partial class CratisBackendApplicationScaffold
{
    /// <summary>
    /// Creates the complete backend application scaffold without writing to a file system.
    /// </summary>
    /// <param name="request">The validated scaffold request.</param>
    /// <returns>Normalized UTF-8 text inputs in ordinal relative-path order.</returns>
    /// <exception cref="InvalidCratisBackendApplicationScaffold">Thrown when the request is missing.</exception>
    public ImmutableArray<ArtifactRenderInput> Create(CratisBackendApplicationScaffoldRequest request)
    {
        if (request is null)
        {
            throw new InvalidCratisBackendApplicationScaffold("A backend application scaffold requires a request.");
        }

        var profile = request.Profile;
        var artifacts = new List<(string RelativePath, string Content)>
        {
            (".editorconfig", EditorConfig()),
            ("Directory.Build.props", DirectoryBuildProps(profile)),
            ("Directory.Build.targets", DirectoryBuildTargets()),
            ("Directory.Packages.props", DirectoryPackagesProps(profile)),
            ($"{request.ProjectName}.csproj", Project(request)),
            ($"{request.ProjectName}.Specs/{request.ProjectName}.Specs.csproj", SpecificationsProject(request)),
            ("README.md", Readme(request)),
            ($"{request.ProjectName}.slnx", Solution(request)),
            ("Program.cs", Program(request)),
            ("GeneratedPolicyRegistration.cs", PolicyRegistration(request)),
            ("appsettings.json", AppSettings(request)),
            ("docker-compose.yml", DockerCompose(profile))
        };
        if (request.RepositoryRoot)
        {
            artifacts.Add((CratisContinuousIntegration.WorkflowPath, CratisContinuousIntegration.Workflow(request)));
            artifacts.Add((CratisContinuousIntegration.DependabotPath, CratisContinuousIntegration.Dependabot()));
        }

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
                "RoutePrefix": "{{GeneratedApiRoutes.RoutePrefix}}",
                "IncludeCommandNameInRoute": false,
                "SegmentsToSkipForRoute": {{GeneratedApiRoutes.SegmentsToSkipForRoute}}
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
