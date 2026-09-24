// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Host;

/// <summary>
/// Admits the complete generated Stage command/query surface before type emission or endpoint mapping.
/// </summary>
internal sealed class StageHttpSurface
{
    static readonly string[] _getMethods = ["GET"];
    static readonly string[] _queryMethods = ["GET", "QUERY"];
    readonly IReadOnlyDictionary<(string Method, string Path), string> _aliases;

    StageHttpSurface(StageHttpOperation[] operations, IReadOnlyDictionary<(string Method, string Path), string> aliases)
    {
        Operations = Array.AsReadOnly(operations);
        _aliases = aliases;
    }

    internal IReadOnlyList<StageHttpOperation> Operations { get; }

    internal static StageHttpSurface Create(EventModel model, StageHttpRouteOptions? routeOptions = null) =>
        Create([.. StageModelWalker.Slices(model).SelectMany(ArtifactsFor)], routeOptions);

    internal static StageHttpSurface Create(ExecutableSemanticModel model, StageHttpRouteOptions? routeOptions = null)
    {
        var slices = SemanticHostModelWalker.Slices(model).ToArray();
        var readModels = slices.SelectMany(located => located.Slice.ReadModels).ToDictionary(readModel => readModel.Id);
        return Create([.. slices.SelectMany(located => ArtifactsFor(located, readModels))], routeOptions);
    }

    internal string? AliasFor(string method, string canonicalPath) =>
        _aliases.TryGetValue((method.ToUpperInvariant(), canonicalPath), out var alias) ? alias : null;

    static StageHttpSurface Create(Artifact[] artifacts, StageHttpRouteOptions? routeOptions)
    {
        var operationsByArtifact = Describe(artifacts, routeOptions ?? new StageHttpRouteOptions());
        var operations = operationsByArtifact.SelectMany(pair => pair.Operations)
            .OrderBy(operation => operation.Method, StringComparer.Ordinal)
            .ThenBy(operation => operation.CanonicalPath, StringComparer.Ordinal)
            .ThenBy(operation => operation.Description, StringComparer.Ordinal)
            .ToArray();

        // Commands and read models share DynamicTypeFactory's full-name cache. Check it even when HTTP methods
        // differ, and count modeled occurrences rather than distinct names or IDs.
        var typeConflicts = operationsByArtifact.GroupBy(pair => pair.Artifact.TypeName, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.SelectMany(pair => pair.Operations).ToArray());

        var claims = operations.SelectMany(operation => new[]
        {
            new Claim(operation, operation.CanonicalPath, true),
            new Claim(operation, operation.LegacyPath, false)
        }).GroupBy(claim => (claim.Operation.Method, claim.Path)).ToArray();

        var routeConflicts = claims.Where(group => group.Any(claim => claim.Canonical) &&
                (group.Count(claim => claim.Canonical) > 1 || group.Select(claim => claim.Operation).Distinct().Count() > 1))
            .Select(group => new AmbiguousStageHttpSurface(group.Key.Method, group.Key.Path, group.Select(claim => claim.Operation).Distinct().Select(operation => operation.Description)));
        var conflicts = typeConflicts.Select(group =>
        {
            var first = group.OrderBy(operation => operation.Method, StringComparer.Ordinal)
                .ThenBy(operation => operation.CanonicalPath, StringComparer.Ordinal).First();
            return new AmbiguousStageHttpSurface(first.Method, first.CanonicalPath, group.Select(operation => $"{operation.Description} (shared CLR identity)"));
        }).Concat(routeConflicts)
            .OrderBy(conflict => conflict.Method, StringComparer.Ordinal)
            .ThenBy(conflict => conflict.Path, StringComparer.Ordinal)
            .ThenBy(conflict => conflict.Message, StringComparer.Ordinal)
            .ToArray();

        if (conflicts.Length > 0)
        {
            throw conflicts[0];
        }

        var aliases = claims.Where(group => !group.Any(claim => claim.Canonical) &&
                group.Select(claim => claim.Operation).Distinct().Count() == 1)
            .Select(group => group.First().Operation)
            .ToDictionary(operation => (operation.Method, operation.CanonicalPath), operation => operation.LegacyPath);

        return new StageHttpSurface(operations, aliases);
    }

    static IEnumerable<Artifact> ArtifactsFor(LocatedSlice located)
    {
        if (located.Slice.Command is { } command)
        {
            yield return new(located.Location, located.CanonicalLocation, located.TypeNamespace, located.Slice.Id, ModelNaming.ToIdentifier(command.Name), true);
        }

        if (located.Slice.ReadModel is { } readModel)
        {
            yield return new(located.Location, located.CanonicalLocation, located.TypeNamespace, located.Slice.Id, ModelNaming.ToIdentifier(readModel.Name), false);
        }
    }

    static IEnumerable<Artifact> ArtifactsFor(LocatedSemanticSlice located, Dictionary<SemanticId, SemanticReadModel> readModels)
    {
        foreach (var command in located.Slice.Commands)
        {
            yield return new(located.Location, located.CanonicalLocation, located.TypeNamespace, Guid.Empty, ModelNaming.ToIdentifier(command.Name), true);
        }

        foreach (var readModel in located.Slice.ReadModels)
        {
            yield return new(
                located.Location,
                located.CanonicalLocation,
                located.TypeNamespace,
                Guid.Empty,
                ModelNaming.ToIdentifier(readModel.Name),
                false,
                [.. located.Slice.Queries.Where(query => query.ReadModel == readModel.Id).Select(query => ModelNaming.ToIdentifier(query.Name))]);
        }

        foreach (var queries in located.Slice.Queries.Where(query => located.Slice.ReadModels.All(readModel => readModel.Id != query.ReadModel))
            .GroupBy(query => query.ReadModel))
        {
            yield return new(
                located.Location,
                located.CanonicalLocation,
                located.TypeNamespace,
                Guid.Empty,
                ModelNaming.ToIdentifier(readModels[queries.Key].Name),
                false,
                [.. queries.Select(query => ModelNaming.ToIdentifier(query.Name))],
                false);
        }
    }

    static (Artifact Artifact, StageHttpOperation[] Operations)[] Describe(Artifact[] artifacts, StageHttpRouteOptions routeOptions)
    {
        var canonical = routeOptions.Canonical;
        var legacy = routeOptions.Legacy;
        var commandsByLocation = EndpointRouteHelper.GroupByNamespace(
            artifacts.Where(artifact => artifact.Command), artifact => artifact.Location, legacy.SegmentsToSkipForRoute);

        return [.. artifacts.Select(artifact =>
        {
            var includeLegacyName = !artifact.Command || EndpointRouteHelper.ShouldIncludeNameInRoute(
                legacy.IncludeCommandNameInRoute, artifact.Location.Skip(legacy.SegmentsToSkipForRoute), commandsByLocation);
            string[] names = artifact.Command ? [artifact.Name] : [.. artifact.Queries ?? []];
            if (!artifact.Command && artifact.Compatibility)
            {
                names = [$"Get{artifact.Name}ById", $"All{ModelNaming.Pluralize(artifact.Name)}", .. names];
            }
            var operations = names.SelectMany(IEnumerable<StageHttpOperation> (string name) =>
            {
                var canonicalPath = EndpointRouteHelper.BuildRouteUrl(canonical, artifact.CanonicalLocation, canonical.SegmentsToSkipForRoute, name, true);
                var legacyPath = EndpointRouteHelper.BuildRouteUrl(legacy, artifact.Location, legacy.SegmentsToSkipForRoute, name, includeLegacyName);
                if (artifact.Command)
                {
                    return
                    [
                        new StageHttpOperation("POST", canonicalPath, legacyPath, "Execute", artifact.SliceId, artifact.TypeName),
                        new StageHttpOperation("POST", $"{canonicalPath}/validate", $"{legacyPath}/validate", "Validate", artifact.SliceId, artifact.TypeName)
                    ];
                }

                var kind = artifact.Queries?.Contains(name) == true ? "QueryKeyed" : "QueryAll";
                if (kind == "QueryAll" && name.StartsWith("Get", StringComparison.Ordinal))
                {
                    kind = "QueryById";
                }
                var methods = canonical.EnableQueryHttpMethod ? _queryMethods : _getMethods;
                return methods.Select(method => new StageHttpOperation(method, canonicalPath, legacyPath, kind, artifact.SliceId, $"{artifact.TypeName}.{name}"));
            }).ToArray();

            return (artifact, operations);
        })];
    }

    sealed record Artifact(IReadOnlyList<string> Location, IReadOnlyList<string> CanonicalLocation, string TypeNamespace, Guid SliceId, string Name, bool Command, IReadOnlyList<string>? Queries = null, bool Compatibility = true)
    {
        internal string TypeName => $"{TypeNamespace}.{Name}";
    }

    sealed record Claim(StageHttpOperation Operation, string Path, bool Canonical);
}
