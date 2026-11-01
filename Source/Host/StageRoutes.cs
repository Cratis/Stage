// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Host;

/// <summary>
/// Where the modeled commands and queries live in the running application.
/// </summary>
/// <param name="Commands">The route each command is posted to, by command name.</param>
/// <param name="Queries">The route each query is read from, by query name.</param>
public record StageRoutes(
    IReadOnlyDictionary<string, string> Commands,
    IReadOnlyDictionary<string, string> Queries)
{
    /// <summary>
    /// Gets every name left out of <see cref="Commands"/> or <see cref="Queries"/> because several distinct
    /// operations answer to it.
    /// </summary>
    public IReadOnlyList<string> Diagnostics { get; init; } = [];

    /// <summary>
    /// Gets every modeled command's presentation schema, used to register native command proxies in the live host and to build a
    /// toolbar slice's task dialog - so the property each command acts on carries <see cref="Contracts.Screenplay.SchemaSynthesizer.IdentityKeyword"/>,
    /// and a command whose identity the runtime allocates or whose event uniquely initializes a listed row carries <see cref="Contracts.Screenplay.SchemaSynthesizer.CreatesKeyword"/>.
    /// </summary>
    public IReadOnlyDictionary<string, string> CommandSchemas { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets each command's JSON response kind, derived from its modeled response; commands without one echo an object.
    /// </summary>
    public IReadOnlyDictionary<string, string> CommandResponseKinds { get; init; } = new Dictionary<string, string>();

    internal static StageRoutes For(StageSceneRoutes routes, EventModel model) => Create(
        routes,
        [],
        StageModelWalker.Slices(model)
            .Where(located => located.Slice.Command is not null)
            .Select(located => ($"{located.TypeNamespace}.{located.Slice.Command!.Name}", IntentOf(located.Slice.Command.Schema, located.Slice.Command.Identifier, false), "object")));

    internal static StageRoutes For(StageSceneRoutes routes, ExecutableSemanticModel model) => For(routes, model, new SemanticListCreation(model));

    internal static StageRoutes For(StageSceneRoutes routes, ExecutableSemanticModel model, SemanticListCreation creation)
    {
        var diagnostics = new ConversionDiagnostics();
        var metadata = new List<(string Identity, string Schema, string ResponseKind)>();
        foreach (var located in SemanticModelWalker.Slices(model))
        {
            foreach (var command in located.Slice.Commands)
            {
                var identity = $"{located.TypeNamespace}.{command.Name}";
                var schema = CommandFormSchemas.For(model.Application, command.Properties.Where(property => !property.IsGenerated), diagnostics, identity, SourceLocation.Start);
                if (schema is null) continue;
                var identifier = command.Properties.SingleOrDefault(property => property.IsIdentifier);
                schema = IntentOf(schema, identifier?.Name, creation.Creates(command, diagnostics, identity));
                var kind = ResponseKind(model.Application, command, diagnostics, identity);
                if (kind is not null) metadata.Add((identity, schema, kind));
            }
        }
        var live = SemanticModelWalker.Slices(model)
            .SelectMany(located => located.Slice.Queries)
            .Where(query => query.Delivery == SemanticQueryDelivery.Live)
            .Select(query => query.Name).ToHashSet(StringComparer.Ordinal);
        var result = Create(routes, live, metadata);
        return result with { Diagnostics = [.. result.Diagnostics, .. diagnostics.All.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")] };
    }

    // Identity stays marked even when allocation or explicit list projection evidence proves creation.
    static string IntentOf(string schema, string? identifier, bool creates) =>
        Contracts.Screenplay.SchemaSynthesizer.WithCreationIntent(
            Contracts.Screenplay.SchemaSynthesizer.WithIdentity(schema, identifier),
            identifier is null || creates);

    static string? ResponseKind(SemanticApplication application, SemanticCommand command, ConversionDiagnostics diagnostics, string path)
    {
        if (command.Response is not SemanticScalarCommandResponse response) return "object";
        var property = command.Properties.Single(property => property.Id == response.Source);
        var text = CommandFormSchemas.For(application, [property], diagnostics, path, SourceLocation.Start);
        if (text is null) return null;
        using var schema = JsonDocument.Parse(text);
        var kind = schema.RootElement.GetProperty("properties").GetProperty(property.Name).GetProperty("type").GetString();
        return kind == "integer" ? "number" : kind;
    }

    static StageRoutes Create(StageSceneRoutes routes, HashSet<string> liveQueries, IEnumerable<(string Identity, string Schema, string ResponseKind)> metadata)
    {
        var entries = metadata.ToArray();
        var schemas = entries.ToDictionary(entry => entry.Identity, entry => entry.Schema, StringComparer.Ordinal);
        var responses = entries.ToDictionary(entry => entry.Identity, entry => entry.ResponseKind, StringComparer.Ordinal);
        foreach (var group in schemas.ToArray().GroupBy(entry => StageRouteCandidates.SimpleName(entry.Key), StringComparer.Ordinal))
        {
            var matches = group.ToArray();
            if (matches.Length == 1)
            {
                schemas[group.Key] = matches[0].Value;
                responses[group.Key] = responses[matches[0].Key];
            }
        }

        // Arc names command endpoints Execute<full type name>. Publish the modeled identity that scene elements carry,
        // not the endpoint operation name; unrelated Host POST operations have no modeled command schema.
        var commands = schemas.Keys
            .Where(identity => routes.QualifiedCommandRoutes.ContainsKey($"Execute{identity}"))
            .ToDictionary(identity => identity, identity => routes.QualifiedCommandRoutes[$"Execute{identity}"], StringComparer.Ordinal);
        foreach (var route in routes.CommandRoutes.Where(entry => schemas.ContainsKey(entry.Key)))
        {
            commands.TryAdd(route.Key, route.Value);
        }

        // A live query's route carries the marker the generated apps' routes carry, so the frontend subscribes to it.
        var queries = routes.QueryRoutes.ToDictionary(
            route => route.Key,
            route => liveQueries.Contains(route.Key) ? $"{route.Value}?waitForFirstResult=true" : route.Value,
            StringComparer.Ordinal);
        return new(commands, queries) { Diagnostics = routes.Diagnostics, CommandSchemas = schemas, CommandResponseKinds = responses };
    }
}
