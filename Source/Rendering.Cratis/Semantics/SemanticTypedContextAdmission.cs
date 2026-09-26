// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Admits only descriptors tied to this compilation's model and implementation envelope.</summary>
internal static class SemanticTypedContextAdmission
{
    internal static ImmutableArray<ArtifactRenderDiagnostic> Verify(ArtifactRenderRequest request)
    {
        var errors = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
        var owner = request.Model.Application.Id;
        if (request.TypedContextContractRevision != SemanticTypedContextDescriptor.ContractRevision)
        {
            errors.Add(Error($"Typed-context contract revision '{request.TypedContextContractRevision}' is not admitted (expected {SemanticTypedContextDescriptor.ContractRevision}).", owner));
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
        }

        // A partial sidecar must not let a bound code body silently fall back to an untyped context.
        if (!request.TypedContextDescriptors.IsEmpty)
        {
            foreach (var requirement in requirements.Where(candidate => candidate.Role != SemanticImplementationRole.PolicyPredicate &&
                !request.TypedContextDescriptors.Any(descriptor => descriptor is not null && descriptor.RequirementId == candidate.RequirementId)))
            {
                errors.Add(Error($"Implementation requirement '{requirement.RequirementId}' has no typed context from this compilation.", owner));
            }
        }

        return errors.ToImmutable();
    }

    static ArtifactRenderDiagnostic Error(string message, SemanticId artifact) =>
        new("STAGE-ESM-021", ArtifactRenderDiagnosticSeverity.Error, message, artifact);
}
