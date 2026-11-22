// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering;

static class ArtifactRenderExclusions
{
    internal const string Remedy = "Correct the listed Screenplay diagnostics, then recompile and re-render the model.";

    internal static ImmutableArray<SemanticExclusion> Normalize(ArtifactRenderRequest request)
    {
        if (request.Exclusions.IsDefault) throw new InvalidArtifactRenderContract("Semantic exclusions must be initialized.");
        if (request.Exclusions.IsEmpty) return [];
        foreach (var exclusion in request.Exclusions) Validate(exclusion, request.Model.Application);
        if (request.Exclusions.Select(exclusion => (exclusion.Kind, exclusion.Address)).Distinct().Count() != request.Exclusions.Length)
            throw new InvalidArtifactRenderContract("Semantic exclusions contain a duplicate kind and address.");
        return [.. request.Exclusions.OrderBy(exclusion => exclusion.Kind.ToString(), StringComparer.Ordinal)
            .ThenBy(exclusion => exclusion.Address, StringComparer.Ordinal).Select(Normalize)];
    }

    internal static SemanticId Parent(SemanticExclusion exclusion, SemanticApplication application) =>
        exclusion.ParentId is { IsSet: true } parent ? parent : application.Id;

    internal static ImmutableArray<ArtifactRenderDegradation> Evidence(ImmutableArray<SemanticExclusion> exclusions, SemanticApplication application) =>
        [.. exclusions.Select(exclusion => new ArtifactRenderDegradation(new(
            ArtifactRenderDegradationCodes.UnsupportedConstruct, ArtifactRenderDiagnosticSeverity.Warning,
            $"{exclusion.Kind} '{exclusion.Address}' excluded from the executable model ({string.Join(", ", exclusion.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct().Order(StringComparer.Ordinal))}).",
            Parent(exclusion, application)), Remedy))];

    internal static string Digest(string original, ImmutableArray<SemanticExclusion> exclusions)
    {
        if (exclusions.IsEmpty) return original;
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, new UTF8Encoding(false), leaveOpen: true);
        writer.Write("stage-render-semantic-exclusions/v1");
        writer.Write(original);
        writer.Write(JsonSerializer.Serialize(exclusions));
        writer.Flush();
        return Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()));
    }

    static void Validate(SemanticExclusion exclusion, SemanticApplication application)
    {
        if (exclusion is null || !HasAddress(exclusion) || !HasMetadata(exclusion))
            throw new InvalidArtifactRenderContract("A semantic exclusion requires a kind, address, initialized diagnostics and dependencies.");
        if (exclusion.Diagnostics.Any(InvalidDiagnostic))
            throw new InvalidArtifactRenderContract("A semantic exclusion requires usable compiler diagnostics.");
        if (exclusion.DependsOn.Any(InvalidDependency))
            throw new InvalidArtifactRenderContract("A semantic exclusion requires usable dependency references.");
        if (!Parents(application).Contains(Parent(exclusion, application)))
            throw new InvalidArtifactRenderContract("A semantic exclusion parent must be a retained application, module, or feature.");
    }

    static bool HasAddress(SemanticExclusion exclusion) => Enum.IsDefined(exclusion.Kind) && !string.IsNullOrWhiteSpace(exclusion.Address);

    static bool HasMetadata(SemanticExclusion exclusion) => !exclusion.Diagnostics.IsDefaultOrEmpty && !exclusion.DependsOn.IsDefault && Enum.IsDefined(exclusion.Cause);

    static bool InvalidDiagnostic(Diagnostic diagnostic) =>
        diagnostic is null || string.IsNullOrWhiteSpace(diagnostic.Code) || string.IsNullOrWhiteSpace(diagnostic.Message);

    static bool InvalidDependency(SemanticExclusionReference dependency) =>
        dependency is null || !Enum.IsDefined(dependency.Kind) || string.IsNullOrWhiteSpace(dependency.Address);

    static IEnumerable<SemanticId> Parents(SemanticApplication application) =>
        new[] { application.Id }.Concat(application.Modules.SelectMany(module => new[] { module.Id }.Concat(module.Features.SelectMany(FeatureParents))));

    static IEnumerable<SemanticId> FeatureParents(SemanticFeature feature) =>
        new[] { feature.Id }.Concat(feature.Features.SelectMany(FeatureParents));

    static SemanticExclusion Normalize(SemanticExclusion exclusion) => exclusion with
    {
        ParentId = exclusion.ParentId is { IsSet: true } parent ? parent : null,
        Diagnostics = [.. exclusion.Diagnostics.OrderBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ThenBy(diagnostic => JsonSerializer.Serialize(diagnostic), StringComparer.Ordinal)],
        DependsOn = [.. exclusion.DependsOn.OrderBy(dependency => dependency.Kind.ToString(), StringComparer.Ordinal)
            .ThenBy(dependency => dependency.Address, StringComparer.Ordinal)]
    };
}
