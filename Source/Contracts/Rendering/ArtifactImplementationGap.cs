// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Describes one unimplemented requirement and its generated payload-body extension points.
/// This v1 contract grants no permission to edit or publish artifacts.
/// </summary>
/// <remarks>Fields added after v1 must be init properties, not additional positional parameters.</remarks>
/// <param name="Origin">The reason implementation is required.</param>
/// <param name="RequirementId">The stable compiler requirement identity.</param>
/// <param name="Owner">Exactly the owning declaration's <see cref="SemanticId.ToString()"/> value. Treat it as opaque; do not parse it.</param>
/// <param name="Role">The compiler implementation role.</param>
/// <param name="Hints">Ordered implementation hints from the requirements sidecar.</param>
/// <param name="ContextVersion">The typed-context contract version.</param>
/// <param name="ResultVersion">The result contract version.</param>
/// <param name="Stubs">The generated payload stubs for this requirement.</param>
public sealed record ArtifactImplementationGap(
    ArtifactImplementationGapOrigin Origin,
    string RequirementId,
    string Owner,
    SemanticImplementationRole Role,
    ImmutableArray<string> Hints,
    uint ContextVersion,
    uint ResultVersion,
    ImmutableArray<ArtifactImplementationStub> Stubs)
{
    /// <summary>
    /// The gap payload schema version, independent of the compiler context and result versions.
    /// </summary>
    public const uint ContractVersion = 1;

    internal ArtifactImplementationGap Normalize()
    {
        if (!HasValidMetadata() || !HasValidCollections())
        {
            throw new InvalidArtifactRenderContract("An implementation gap requires an origin, requirement identity, owner, role, hints, positive contract versions, and stubs.");
        }

        var stubs = Stubs.Select(stub => stub.Normalize())
            .OrderBy(stub => stub.RelativePath, StringComparer.Ordinal)
            .ThenBy(stub => stub.BodyLocator.Start).ToImmutableArray();
        if (stubs.GroupBy(stub => (stub.RelativePath, stub.TypeName, stub.MemberName, stub.Signature)).Any(group => group.Count() > 1))
        {
            throw new InvalidArtifactRenderContract($"Implementation gap '{RequirementId}' contains duplicate stubs.");
        }

        return this with { Stubs = stubs };
    }

    bool HasValidMetadata() => HasKnownRoleAndOrigin() && HasIdentity() && HasPositiveContracts();
    bool HasKnownRoleAndOrigin() => Origin is ArtifactImplementationGapOrigin.Described or ArtifactImplementationGapOrigin.Unverified && Enum.IsDefined(Role);
    bool HasIdentity() => !string.IsNullOrWhiteSpace(RequirementId) && !string.IsNullOrWhiteSpace(Owner);
    bool HasPositiveContracts() => ContextVersion >= 1 && ResultVersion >= 1;
    bool HasValidCollections() => HasValidHints() && HasValidStubs();
    bool HasValidHints() => !Hints.IsDefault && !Hints.Any(string.IsNullOrWhiteSpace);
    bool HasValidStubs() => !Stubs.IsDefaultOrEmpty && Stubs.All(stub => stub is not null);
}
