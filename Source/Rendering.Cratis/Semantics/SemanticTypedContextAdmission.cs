// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Admits only descriptors tied to this compilation's model and implementation envelope.</summary>
internal static class SemanticTypedContextAdmission
{
    internal const uint SupportedTypedContextContractRevision = 1;

    internal static ImmutableArray<ArtifactRenderDiagnostic> Verify(ArtifactRenderRequest request)
    {
        var errors = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
        var owner = request.Model.Application.Id;
        if (request.TypedContextContractRevision != SupportedTypedContextContractRevision)
        {
            errors.Add(Error($"Typed-context contract revision '{request.TypedContextContractRevision}' is not admitted (expected {SupportedTypedContextContractRevision}).", owner));
            return errors.ToImmutable();
        }

        if (request.TypedContextDescriptors.IsDefault)
        {
            errors.Add(Error("The typed-context descriptor envelope is missing.", owner));
            return errors.ToImmutable();
        }

        var requirements = request.ImplementationRequirements.IsDefault
            ? []
            : request.ImplementationRequirements.Where(requirement => requirement is not null).ToArray();
        var keys = new HashSet<(string, SemanticId?)>();
        foreach (var descriptor in request.TypedContextDescriptors)
        {
            if (descriptor is null)
            {
                errors.Add(Error("The typed-context descriptor envelope contains a null descriptor.", owner));
                continue;
            }

            var requirement = requirements.FirstOrDefault(candidate => candidate.RequirementId == descriptor.RequirementId);
            if (requirement is null || descriptor.Role != requirement.Role || descriptor.ContextVersion != 1 ||
                descriptor.ContextVersion != requirement.ContextVersion || descriptor.OperationId is null ||
                !keys.Add((descriptor.RequirementId, descriptor.OperationId)))
            {
                errors.Add(Error($"Typed context '{descriptor.RequirementId}' has no unique matching compiler requirement, role, context version and use site.", owner));
                continue;
            }

            if (!descriptor.IsWrapperReady || descriptor.ModelRevision != request.Model.Revision)
            {
                errors.Add(Error($"Typed context '{descriptor.RequirementId}' for '{descriptor.OperationId}' is not wrapper-ready from this model compilation (revision {request.Model.Revision}).", owner));
            }

            if (descriptor.Types.IsDefault || descriptor.Members.IsDefault ||
                (!descriptor.Types.IsDefault && descriptor.Types.Any(type => type is null)) ||
                (!descriptor.Members.IsDefault && descriptor.Members.Any(member => member is null)) ||
                (!descriptor.Types.IsDefault && descriptor.Types.Select(type => type.Id).Distinct().Count() != descriptor.Types.Length) ||
                (!descriptor.Members.IsDefault && descriptor.Members.Any(member => member.Type?.Properties.IsDefault != false)) ||
                (!descriptor.Types.IsDefault && descriptor.Types.Any(type => type.Properties.IsDefault)))
            {
                errors.Add(Error($"Typed context '{descriptor.RequirementId}' contains missing arrays, members or duplicate type identities.", owner));
            }
            else
            {
                // Only query shapes generate local declarations; repeated use of one would collide.
                var queryShapes = descriptor.Members.Where(member => member.Type.Shape is { } shape &&
                    request.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices)
                        .SelectMany(slice => slice.Queries).Any(query => query.Id == shape))
                    .Select(member => member.Type.Shape!.Value).ToArray();
                if (queryShapes.Distinct().Count() != queryShapes.Length)
                {
                    errors.Add(Error($"Typed context '{descriptor.RequirementId}' contains duplicate query shapes.", owner));
                }
            }
        }

        // A partial sidecar must not let a bound code body silently fall back to an untyped context.
        if (!request.TypedContextDescriptors.IsEmpty)
        {
            foreach (var requirement in requirements)
            {
                var sites = requirement.Role == SemanticImplementationRole.PolicyPredicate
                    ? PolicySites(request.Model.Application, requirement.RequirementId)
                    : [];
                foreach (var site in sites.Distinct())
                {
                    if (!request.TypedContextDescriptors.Any(descriptor => descriptor is not null &&
                        descriptor.RequirementId == requirement.RequirementId && descriptor.OperationId == site))
                    {
                        errors.Add(Error($"Implementation requirement '{requirement.RequirementId}' has no typed context for use site '{site}' from this compilation.", owner));
                    }
                }
                if (requirement.Role != SemanticImplementationRole.PolicyPredicate &&
                    !request.TypedContextDescriptors.Any(descriptor => descriptor is not null && descriptor.RequirementId == requirement.RequirementId))
                {
                    errors.Add(Error($"Implementation requirement '{requirement.RequirementId}' has no typed context from this compilation.", owner));
                }
            }
        }

        return errors.ToImmutable();
    }

    static IEnumerable<SemanticId> PolicySites(SemanticApplication application, string requirementId)
    {
        var policies = application.Policies.Where(policy => PolicyBodies(policy.Condition).Contains(requirementId))
            .Select(policy => policy.Name).ToHashSet(StringComparer.Ordinal);
        return application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices)
            .SelectMany(slice => slice.Commands.Select(command => (command.Id, command.Authorization))
                .Concat(slice.Queries.Select(query => (query.Id, query.Authorization))))
            .Where(site => site.Authorization is not null && PolicyReferences(site.Authorization).Any(policies.Contains))
            .Select(site => site.Id);
    }

    static IEnumerable<SemanticSlice> AllSlices(SemanticFeature feature) =>
        feature.Slices.Concat(feature.Features.SelectMany(AllSlices));

    static IEnumerable<string> PolicyReferences(SemanticAuthorization authorization) => authorization switch
    {
        SemanticPolicyReference reference => [reference.Name],
        SemanticLogicalAuthorization logical => PolicyReferences(logical.Left).Concat(PolicyReferences(logical.Right)),
        _ => []
    };

    static IEnumerable<string> PolicyBodies(SemanticPolicyCondition condition) => condition switch
    {
        SemanticOpaquePolicyCondition opaque => [opaque.RequirementId],
        SemanticLogicalPolicyCondition logical => PolicyBodies(logical.Left).Concat(PolicyBodies(logical.Right)),
        _ => []
    };

    static ArtifactRenderDiagnostic Error(string message, SemanticId artifact) =>
        new("STAGE-ESM-021", ArtifactRenderDiagnosticSeverity.Error, message, artifact);
}
