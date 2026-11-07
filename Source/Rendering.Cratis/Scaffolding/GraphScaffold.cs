// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// Connects the application scaffold to Neo4j when the model declares a graph: Arc reads the graph through <c language="csharp">IGraphSession</c>,
/// Chronicle's Neo4j sink writes it, and a development Neo4j server joins the compose file.
/// </summary>
/// <remarks>
/// No password is written anywhere. Arc and Chronicle both name the environment variable that holds it (<c language="text">NEO4J_PASSWORD</c>),
/// and the compose file takes its value from the environment of the person running it and refuses to start without it.
/// </remarks>
internal static class GraphScaffold
{
    /// <summary>
    /// The environment variable that holds the Neo4j password for Arc, Chronicle and the development server.
    /// </summary>
    internal const string PasswordVariable = "NEO4J_PASSWORD";

    const string ProgramUsing = "using Cratis.Arc.MongoDB;\n";
    const string ProgramBuilder = "arc => arc.WithMongoDB(),";
    const string ProjectReference = "    <PackageReference Include=\"Cratis.Arc.MongoDB\" />\n";
    const string PackageVersionPrefix = "    <PackageVersion Include=\"Cratis.Arc.MongoDB\" ";
    const string SettingsMarker = "    \"MongoDB\": {";
    const string ComposeVolumes = "\nvolumes:\n  chronicle-data:\n";
    const string ComposeImage = "-development\n";
    const string ReadmeMarker = "## Customize\n";

    /// <summary>
    /// Patches the scaffold files that connect an application to Neo4j; any other artifact passes through unchanged.
    /// </summary>
    /// <param name="artifact">The scaffold artifact.</param>
    /// <param name="applicationName">The application name, which is also the Chronicle event store the graph belongs to.</param>
    /// <returns>The patched artifact, or the artifact itself.</returns>
    internal static PlannedArtifact Patch(PlannedArtifact artifact, string applicationName)
    {
        var path = artifact.RelativePath;
        if (path.Contains('/', StringComparison.Ordinal)) return artifact;
        return path switch
        {
            "Program.cs" => Rewrite(artifact, Program),
            "appsettings.json" => Rewrite(artifact, text => Insert(text, SettingsMarker, Settings(applicationName), before: true)),
            "docker-compose.yml" => Rewrite(artifact, Compose),
            "Directory.Packages.props" => Rewrite(artifact, PackageVersion),
            "README.md" => Rewrite(artifact, text => Insert(text, ReadmeMarker, Readme(), before: true)),
            _ when path.EndsWith(".csproj", StringComparison.Ordinal) => Rewrite(artifact, text => Insert(text, ProjectReference, "    <PackageReference Include=\"Cratis.Arc.Neo4j\" />\n", before: false)),
            _ => artifact
        };
    }

    static string Program(string text)
    {
        text = Replace(text, ProgramUsing, "using Cratis.Arc;\nusing Cratis.Arc.MongoDB;\nusing Cratis.Arc.Neo4j;\n");
        return Replace(text, ProgramBuilder, "arc => arc.WithMongoDB().WithNeo4j(),");
    }

    static string PackageVersion(string text)
    {
        var start = text.IndexOf(PackageVersionPrefix, StringComparison.Ordinal);
        var end = start < 0 ? -1 : text.IndexOf('\n', start);
        if (end < 0) throw UnsupportedSemanticRendering.For(nameof(GraphScaffold), $"scaffold marker '{PackageVersionPrefix.Trim()}'");
        return text.Insert(end + 1, $"    <PackageVersion Include=\"Cratis.Arc.Neo4j\" Version=\"{ScaffoldPackageVersions.ArcNeo4j}\" />\n");
    }

    static string Settings(string applicationName) =>
        $$"""
            "Neo4j": {
              "Server": "bolt://localhost:7687",
              "EventStore": "{{applicationName}}",
              "UserName": "neo4j",
              "PasswordSecret": "{{PasswordVariable}}",
              "DatabaseMode": "Single"
            },

        """;

    static string Compose(string text)
    {
        text = Insert(text, ComposeImage, $"    environment:\n      Cratis__Chronicle__Neo4j__Server: bolt://neo4j:7687\n      Cratis__Chronicle__Neo4j__UserName: neo4j\n      Cratis__Chronicle__Neo4j__PasswordSecret: {PasswordVariable}\n      Cratis__Chronicle__Neo4j__DatabaseMode: Single\n      {PasswordVariable}: ${{{PasswordVariable}:?Set {PasswordVariable} before starting the services}}\n    depends_on:\n      neo4j:\n        condition: service_healthy\n", before: false);
        text = Insert(text, ComposeVolumes, Neo4jService(), before: true);
        return Replace(text, ComposeVolumes, $"{ComposeVolumes}  neo4j-data:\n");
    }

    static string Neo4jService() =>
        $$"""

          neo4j:
            image: neo4j:5-community
            ports:
              - "7687:7687"
            environment:
              NEO4J_AUTH: neo4j/${{{PasswordVariable}}:?Set {{PasswordVariable}} before starting the services}
            volumes:
              - neo4j-data:/data
            healthcheck:
              test: ["CMD-SHELL", "wget --no-verbose --tries=1 --spider http://localhost:7474 || exit 1"]
              interval: 5s
              timeout: 5s
              retries: 30
              start_period: 20s

        """;

    static string Readme() =>
        $"""
        ## Graphs

        The model declares a graph. Chronicle projects events onto it in Neo4j and your queries read it through `IGraphSession`.
        Set `{PasswordVariable}` in your environment before `docker compose up`: it is the password of the development Neo4j server
        and the name Arc and Chronicle read it by, and nothing writes it into a file. The development setup keeps every namespace in
        one database (`DatabaseMode: Single`), which is for development only; use `PerNamespace` against a server that supports it.


        """;

    static PlannedArtifact Rewrite(PlannedArtifact artifact, Func<string, string> patch) =>
        PlannedArtifact.CreateText(artifact.RelativePath, patch(Encoding.UTF8.GetString(artifact.Bytes.AsSpan())), artifact.Sources);

    static string Insert(string text, string marker, string insertion, bool before)
    {
        var index = text.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0 || text.IndexOf(marker, index + marker.Length, StringComparison.Ordinal) >= 0)
        {
            throw UnsupportedSemanticRendering.For(nameof(GraphScaffold), $"scaffold marker '{marker.Trim()}'");
        }

        return text.Insert(before ? index : index + marker.Length, insertion);
    }

    static string Replace(string text, string marker, string replacement)
    {
        var index = text.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0 || text.IndexOf(marker, index + marker.Length, StringComparison.Ordinal) >= 0)
        {
            throw UnsupportedSemanticRendering.For(nameof(GraphScaffold), $"scaffold marker '{marker.Trim()}'");
        }

        return string.Concat(text.AsSpan(0, index), replacement, text.AsSpan(index + marker.Length));
    }
}
