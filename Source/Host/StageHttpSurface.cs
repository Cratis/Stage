// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts;

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

    /// <summary>
    /// Plans routes from modeled occurrences, not emitted types: collection/type collisions must remain visible.
    /// </summary>
    /// <param name="model">The model to admit.</param>
    /// <param name="routeOptions">The host's startup route options, or the default contract.</param>
    /// <returns>The admitted canonical surface and its uniquely owned compatibility aliases.</returns>
    /// <exception cref="AmbiguousStageHttpSurface">Canonical, CLR, or foreign canonical/legacy ownership conflicts.</exception>
    internal static StageHttpSurface Create(EventModel model, StageHttpRouteOptions? routeOptions = null)
    {
        var artifacts = StageModelWalker.Slices(model).SelectMany(ArtifactsFor).ToArray();
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

        // Legacy/legacy conflicts alone are omitted, never resolved by declaration order. A canonical route may
        // not take over somebody else's historical URL, even if that historical URL itself was ambiguous.
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

    internal string? AliasFor(string method, string canonicalPath) =>
        _aliases.TryGetValue((method.ToUpperInvariant(), canonicalPath), out var alias) ? alias : null;

    static IEnumerable<Artifact> ArtifactsFor(LocatedSlice located)
    {
        if (located.Slice.Command is { } command)
        {
            yield return new Artifact(located, ModelNaming.ToIdentifier(command.Name), true);
        }

        if (located.Slice.ReadModel is { } readModel)
        {
            yield return new Artifact(located, ModelNaming.ToIdentifier(readModel.Name), false);
        }
    }

    static (Artifact Artifact, StageHttpOperation[] Operations)[] Describe(Artifact[] artifacts, StageHttpRouteOptions routeOptions)
    {
        var canonical = routeOptions.Canonical;
        var legacy = routeOptions.Legacy;
        var commandsByLocation = EndpointRouteHelper.GroupByNamespace(
            artifacts.Where(artifact => artifact.Command), artifact => artifact.Located.Location, legacy.SegmentsToSkipForRoute);

        return [.. artifacts.Select(artifact =>
        {
            var located = artifact.Located;
            var includeLegacyName = !artifact.Command || EndpointRouteHelper.ShouldIncludeNameInRoute(
                legacy.IncludeCommandNameInRoute, located.Location.Skip(legacy.SegmentsToSkipForRoute), commandsByLocation);
            string[] names = artifact.Command
                ? [artifact.Name]
                : [$"Get{artifact.Name}ById", $"All{ModelNaming.Pluralize(artifact.Name)}"];
            var operations = names.SelectMany(IEnumerable<StageHttpOperation> (string name) =>
            {
                var canonicalPath = EndpointRouteHelper.BuildRouteUrl(canonical, located.CanonicalLocation, canonical.SegmentsToSkipForRoute, name, true);
                var legacyPath = EndpointRouteHelper.BuildRouteUrl(legacy, located.Location, legacy.SegmentsToSkipForRoute, name, includeLegacyName);
                if (artifact.Command)
                {
                    return
                    [
                        new StageHttpOperation("POST", canonicalPath, legacyPath, "Execute", located.Slice.Id, artifact.TypeName),
                        new StageHttpOperation("POST", $"{canonicalPath}/validate", $"{legacyPath}/validate", "Validate", located.Slice.Id, artifact.TypeName)
                    ];
                }

                var kind = name.StartsWith("Get", StringComparison.Ordinal) ? "QueryById" : "QueryAll";
                var methods = canonical.EnableQueryHttpMethod ? _queryMethods : _getMethods;
                return methods.Select(method => new StageHttpOperation(method, canonicalPath, legacyPath, kind, located.Slice.Id, $"{artifact.TypeName}.{name}"));
            }).ToArray();

            return (artifact, operations);
        })];
    }

    sealed record Artifact(LocatedSlice Located, string Name, bool Command)
    {
        internal string TypeName => $"{Located.TypeNamespace}.{Name}";
    }

    sealed record Claim(StageHttpOperation Operation, string Path, bool Canonical);
}
