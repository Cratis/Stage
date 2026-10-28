// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis;

static class CratisSemanticInventory
{
    internal static ArtifactRenderInventory Create(
        ArtifactRenderRequest request,
        ArtifactRenderPlan plan,
        CratisRenderingOptions options,
        IReadOnlyDictionary<SemanticId, ImmutableArray<SemanticId>>? omissionCauses = null)
    {
        var context = new SemanticApplicationContext(request, options);
        var assignments = request.SemanticIdentities.Concat(CratisBootstrapLocations.Resolve(context, request)).GroupBy(assignment => assignment.Id).ToDictionary(group => group.Key, group => group.ToArray());
        var owners = CratisSelectedSemanticOwners.Enumerate(context).Distinct().ToArray();
        var constructs = owners.Select(owner => Construct(owner.Id, owner.Kind, Address(request, assignments, owner.Id), plan, omissionCauses));
        return ArtifactRenderInventory.Create([.. constructs]);
    }

    static SemanticAddress Address(ArtifactRenderRequest request, Dictionary<SemanticId, SemanticIdentityAssignment[]> assignments, SemanticId identity)
    {
        var addresses = new List<SemanticAddress>();
        if (assignments.TryGetValue(identity, out var candidates))
        {
            addresses.AddRange(candidates.Select(assignment => assignment.Address));
        }
        addresses.AddRange(request.ImplementationRequirements.Where(requirement => requirement.Source.SemanticId == identity).Select(requirement => requirement.Owner));
        var distinct = addresses.Distinct().ToArray();
        if (distinct.Length != 1)
        {
            throw new InvalidArtifactRenderContract("A selected semantic owner requires one consistent retained catalog or native implementation-owner address.");
        }
        return distinct[0];
    }

    static ArtifactRenderConstruct Omitted(ArtifactRenderPlan plan, ArtifactRenderConstructKind kind, SemanticAddress address, ArtifactRenderRefusal omission) =>
        new(omission.Artifact, kind, null, ArtifactRenderOutcome.Refused)
        {
            SemanticLocator = address,
            RefusalReasons = [.. plan.Diagnostics.Where(diagnostic => diagnostic.Artifact == omission.Artifact && omission.Codes.Contains(diagnostic.Code))]
        };

    static ArtifactRenderConstruct Construct(
        SemanticId identity,
        ArtifactRenderConstructKind kind,
        SemanticAddress address,
        ArtifactRenderPlan plan,
        IReadOnlyDictionary<SemanticId, ImmutableArray<SemanticId>>? omissionCauses)
    {
        var omission = plan.Refusals.SingleOrDefault(refusal => refusal.Artifact == identity);
        var evidence = plan.Degradations.Where(gap => gap.Diagnostic.Artifact == identity).ToArray();
        if (omission is not null && !IsGraphProvenOmission(omission, evidence)) return Omitted(plan, kind, address, omission);
        var placeholder = omission is null ? ArtifactRenderPlaceholderKind.ThrowingOperationBody : ArtifactRenderPlaceholderKind.GraphProvenOmittedLeaf;
        return new(identity, kind, null, evidence.Length == 0 ? ArtifactRenderOutcome.Rendered : ArtifactRenderOutcome.Degraded)
        {
            SemanticLocator = address,
            Degradations = [.. evidence.Select(gap => new ArtifactRenderConstructGap(gap, placeholder, gap.Diagnostic.Message, null)
            {
                SemanticLocator = address,
                Roots = Roots(gap, plan.Degradations, omissionCauses)
            })]
        };
    }

    static bool IsGraphProvenOmission(ArtifactRenderRefusal omission, ArtifactRenderDegradation[] evidence) =>
        (omission.Codes.Contains(ArtifactRenderDegradationCodes.UnknownOwnerMember) || omission.Codes.Contains(ArtifactRenderDegradationCodes.DependencyDegradation)) &&
        omission.Codes.All(code => evidence.Any(gap => gap.Diagnostic.Code == code));

    static ImmutableArray<ArtifactRenderGapReference> Roots(
        ArtifactRenderDegradation gap,
        ImmutableArray<ArtifactRenderDegradation> evidence,
        IReadOnlyDictionary<SemanticId, ImmutableArray<SemanticId>>? omissionCauses)
    {
        if (gap.Diagnostic.Code != ArtifactRenderDegradationCodes.DependencyDegradation ||
            omissionCauses is null || !omissionCauses.TryGetValue(gap.Diagnostic.Artifact, out var roots))
        {
            return [];
        }
        return [.. evidence.Where(root => roots.Contains(root.Diagnostic.Artifact) && root.Diagnostic.Code != ArtifactRenderDegradationCodes.DependencyDegradation)
            .Select(root => new ArtifactRenderGapReference(root.Diagnostic.Artifact, root.Diagnostic.Code)).Distinct()
            .OrderBy(root => root.Owner.ToString(), StringComparer.Ordinal).ThenBy(root => root.Code, StringComparer.Ordinal)];
    }
}
