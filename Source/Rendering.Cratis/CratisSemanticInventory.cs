// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis;

static class CratisSemanticInventory
{
    internal static ArtifactRenderInventory Create(ArtifactRenderRequest request, ArtifactRenderPlan plan, CratisRenderingOptions options)
    {
        var context = new SemanticApplicationContext(request, options);
        var assignments = request.SemanticIdentities.Concat(CratisBootstrapLocations.Resolve(context, request)).GroupBy(assignment => assignment.Id).ToDictionary(group => group.Key, group => group.ToArray());
        var owners = CratisSelectedSemanticOwners.Enumerate(context).Distinct().ToArray();
        var constructs = owners.Select(owner => Construct(owner.Id, owner.Kind, Address(request, assignments, owner.Id), plan));
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

    static ArtifactRenderConstruct Construct(SemanticId identity, ArtifactRenderConstructKind kind, SemanticAddress address, ArtifactRenderPlan plan)
    {
        var omission = plan.Refusals.SingleOrDefault(refusal => refusal.Artifact == identity);
        if (omission is not null) return Omitted(plan, kind, address, omission);
        var evidence = plan.Degradations.Where(gap => gap.Diagnostic.Artifact == identity).ToArray();
        return new(identity, kind, null, evidence.Length == 0 ? ArtifactRenderOutcome.Rendered : ArtifactRenderOutcome.Degraded)
        {
            SemanticLocator = address,
            Degradations = [.. evidence.Select(gap => new ArtifactRenderConstructGap(gap, kind == ArtifactRenderConstructKind.ReadModel ? ArtifactRenderPlaceholderKind.RetainedSignatureOrType : ArtifactRenderPlaceholderKind.ThrowingOperationBody, gap.Diagnostic.Message, null) { SemanticLocator = address })]
        };
    }
}
