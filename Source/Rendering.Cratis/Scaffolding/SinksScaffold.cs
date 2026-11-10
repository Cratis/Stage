// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// Configures the development kernel with one configuration per named sink the model declares, so a rendered application whose
/// read models or graphs choose a sink runs without further setup.
/// </summary>
/// <remarks>
/// The kernel is configured in the compose file, where it runs: a read-model sink on the development image's own MongoDB and a
/// graph sink on the development Neo4j server, with the same password variable <see cref="GraphScaffold"/> uses. The application's
/// <c language="text">appsettings.json</c> maps each read-model sink to the same development MongoDB under
/// <c language="text">Cratis:MongoDB:Sinks:&lt;name&gt;:Server</c>, so Arc reads a read model marked <c language="csharp">[Sink("name")]</c> where the
/// kernel writes it. The kernel's default sinks stay the ones the scaffold already configures. A deployment maps each name to its own storage.
/// </remarks>
internal static class SinksScaffold
{
    internal const string DevelopmentMongoDB = "mongodb://localhost:27017";
    internal const string DevelopmentNeo4j = "bolt://neo4j:7687";

    const string ComposeImage = "-development\n";
    const string Environment = "    environment:\n";

    static readonly JsonSerializerOptions _indented = new() { WriteIndented = true, NewLine = "\n" };

    /// <summary>
    /// Patches the compose file with the kernel's sink configurations and the application settings with the servers of its read-model
    /// sinks; any other artifact passes through unchanged.
    /// </summary>
    /// <param name="artifact">The scaffold artifact.</param>
    /// <param name="sinks">The sinks of the application.</param>
    /// <returns>The patched artifact, or the artifact itself.</returns>
    internal static PlannedArtifact Patch(PlannedArtifact artifact, RealizedSinks sinks)
    {
        if (sinks.Declarations.IsDefaultOrEmpty) return artifact;
        return artifact.RelativePath switch
        {
            "docker-compose.yml" => PlannedArtifact.CreateText(artifact.RelativePath, Compose(Encoding.UTF8.GetString(artifact.Bytes.AsSpan()), sinks), artifact.Sources),
            "appsettings.json" when sinks.Declarations.Any(sink => sink.Kind == SemanticSinkKind.ReadModels) =>
                PlannedArtifact.CreateText(artifact.RelativePath, AppSettings(Encoding.UTF8.GetString(artifact.Bytes.AsSpan()), sinks), artifact.Sources),
            _ => artifact
        };
    }

    /// <summary>
    /// Gets the kernel environment variables that configure the declared sinks, in declaration order.
    /// </summary>
    /// <param name="sinks">The sinks of the application.</param>
    /// <returns>The variable names and values.</returns>
    internal static IEnumerable<(string Name, string Value)> Variables(RealizedSinks sinks)
    {
        foreach (var sink in sinks.Declarations)
        {
            var prefix = $"Cratis__Chronicle__Sinks__Configurations__{sink.Name}__";
            if (sink.Kind == SemanticSinkKind.Graphs)
            {
                yield return ($"{prefix}Type", "Neo4j");
                yield return ($"{prefix}Server", DevelopmentNeo4j);
                yield return ($"{prefix}UserName", "neo4j");
                yield return ($"{prefix}PasswordSecret", GraphScaffold.PasswordVariable);
                yield return ($"{prefix}DatabaseMode", "Single");
            }
            else
            {
                yield return ($"{prefix}Type", "MongoDB");
                yield return ($"{prefix}ConnectionDetails", DevelopmentMongoDB);
            }
        }
    }

    // Arc reads a read model marked [Sink("name")] from Cratis:MongoDB:Sinks:<name>, in the database the default connection names.
    // The development kernel writes every read-model sink to its own MongoDB, which the application reaches on localhost.
    static string AppSettings(string text, RealizedSinks sinks)
    {
        var root = JsonNode.Parse(text)?.AsObject();
        if (root?["Cratis"]?["MongoDB"] is not JsonObject mongoDB)
        {
            throw UnsupportedSemanticRendering.For(nameof(SinksScaffold), "application settings section 'Cratis:MongoDB'");
        }

        var configured = new JsonObject();
        foreach (var sink in sinks.Declarations.Where(sink => sink.Kind == SemanticSinkKind.ReadModels))
        {
            configured[sink.Name] = new JsonObject { ["Server"] = DevelopmentMongoDB };
        }

        mongoDB["Sinks"] = configured;
        var trailing = text.EndsWith('\n') ? "\n" : string.Empty;
        return root.ToJsonString(_indented) + trailing;
    }

    static string Compose(string text, RealizedSinks sinks)
    {
        var image = text.IndexOf(ComposeImage, StringComparison.Ordinal);
        if (image < 0 || text.IndexOf(ComposeImage, image + ComposeImage.Length, StringComparison.Ordinal) >= 0)
        {
            throw UnsupportedSemanticRendering.For(nameof(SinksScaffold), $"scaffold marker '{ComposeImage.Trim()}'");
        }

        var variables = string.Concat(Variables(sinks).Select(variable => $"      {variable.Name}: {variable.Value}\n"));

        // The graph scaffold may already have given the kernel an environment; the sinks join it rather than start a second one.
        var after = image + ComposeImage.Length;
        return string.CompareOrdinal(text, after, Environment, 0, Environment.Length) == 0
            ? text.Insert(after + Environment.Length, variables)
            : text.Insert(after, Environment + variables);
    }
}
