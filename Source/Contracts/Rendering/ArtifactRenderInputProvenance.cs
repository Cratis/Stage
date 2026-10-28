// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Records the effective inputs of a plan: the profile input digests, every implementation input and the planner components.
/// Prerelease. It is excluded from the output-only plan digest and has a digest of its own.
/// </summary>
/// <param name="Inputs">The profile inputs, in the profile's ordinal order.</param>
/// <param name="Implementations">The implementation inputs, sorted and distinct.</param>
/// <param name="Components">The planner components, sorted by kind and name.</param>
public sealed record ArtifactRenderInputProvenance(
    ImmutableArray<ArtifactRenderInputDigest> Inputs,
    ImmutableArray<ArtifactRenderImplementationInput> Implementations,
    ImmutableArray<ArtifactRenderComponent> Components)
{
    const string DigestDomain = "stage-input-provenance-v1";

    /// <summary>Gets provenance with no inputs, implementations or components.</summary>
    public static ArtifactRenderInputProvenance Empty { get; } = new([], [], []);

    /// <summary>Gets the lowercase SHA-256 over the domain, the counts and every field in order.</summary>
    public string Digest
    {
        get
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, new UTF8Encoding(false), leaveOpen: true);
            writer.Write(DigestDomain);
            writer.Write(Inputs.Length);
            foreach (var input in Inputs)
            {
                writer.Write(input.Name);
                writer.Write(input.Version);
                writer.Write(input.Sha256);
            }

            writer.Write(Implementations.Length);
            foreach (var implementation in Implementations)
            {
                writer.Write(implementation.RequirementId);
                writer.Write(implementation.Role.ToString());
                writer.Write(OwnerText(implementation));
                WriteOptional(writer, implementation.Language);
                WriteOptional(writer, implementation.File);
                writer.Write(implementation.ContentHash);
            }

            writer.Write(Components.Length);
            foreach (var component in Components)
            {
                writer.Write(component.Kind.ToString());
                writer.Write(component.Name);
                writer.Write(component.Version);
                WriteOptional(writer, component.Digest);
            }

            writer.Flush();
            return Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
        }
    }

    internal static ArtifactRenderInputProvenance From(ArtifactRenderRequest request)
    {
        var requirements = request.ImplementationRequirements.IsDefault ? [] : request.ImplementationRequirements;
        var implementations = requirements
            .Select(requirement => new ArtifactRenderImplementationInput(
                requirement.RequirementId,
                requirement.Role,
                requirement.Owner,
                requirement.Language,
                requirement.File,
                requirement.ContentHash ?? string.Empty))
            .Distinct()
            .OrderBy(_ => _.RequirementId, StringComparer.Ordinal)
            .ThenBy(_ => _.Role.ToString(), StringComparer.Ordinal)
            .ThenBy(OwnerText, StringComparer.Ordinal)
            .ThenBy(_ => _.Language, StringComparer.Ordinal)
            .ThenBy(_ => _.File, StringComparer.Ordinal)
            .ThenBy(_ => _.ContentHash, StringComparer.Ordinal)
            .ToImmutableArray();
        return new(
            [.. request.Profile.Inputs.Select(input => new ArtifactRenderInputDigest(input.Name, input.Version, input.Sha256))],
            implementations,
            []);
    }

    static string OwnerText(ArtifactRenderImplementationInput implementation) => SemanticId.Create(implementation.Owner).ToString();

    static void WriteOptional(BinaryWriter writer, string? value)
    {
        writer.Write(value is not null);
        if (value is not null) writer.Write(value);
    }
}
