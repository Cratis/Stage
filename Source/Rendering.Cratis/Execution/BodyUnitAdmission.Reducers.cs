// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Execution;

/// <summary>Admits a reducer transition through the renderer's own reducer admission and pure analysis.</summary>
internal static partial class BodyUnitAdmission
{
    internal static (SemanticTypedContextDescriptor? Descriptor, string? Body, BodyUnitEntry? Refusal) AdmitReducer(SemanticApplicationContext context, SemanticImplementationRequirement requirement)
    {
        var id = requirement.RequirementId;
        var binding = BindReducer(context, requirement);
        if (binding.Refusal is not null) return (null, null, binding.Refusal);
        var reducer = binding.Reducer!;
        var model = binding.Model!;
        var diagnostics = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
        if (!SemanticImplementationAdmission.TryGetVerifiedBody(context.Request, id, reducer.ReadModel, diagnostics, out var body))
        {
            var diagnostic = diagnostics.First();
            return (null, null, Refuse(requirement, diagnostic.Code, diagnostic.Message));
        }
        var transition = reducer.Transitions.Single(candidate => candidate.RequirementId == id);
        var descriptors = context.Request.TypedContextDescriptors.Where(descriptor => descriptor.RequirementId == id && descriptor.OperationId == reducer.ReadModel).ToArray();
        if (!ValidDescriptor(descriptors, requirement) || !context.Events.TryGetValue(transition.EventContract, out var @event))
            return (null, null, Refuse(requirement, "STAGE-ESM-021", "No unique wrapper-ready typed context is available."));

        // The renderer's own reducer admission decides first, so the worker never runs what the renderer refuses.
        var error = RendererError(context, reducer.ReadModel);
        if (error is not null) return (null, null, Refuse(requirement, error.Code, error.Message));
        var verdict = PureTransitionAdmission.Analyze(body!, context, model, @event, descriptors[0], requirement);
        if (!verdict.Accepted) return (null, null, Refuse(requirement, verdict.Code!, verdict.Reason!));
        return (descriptors[0], body, null);
    }

    static (SemanticReducer? Reducer, SemanticReadModel? Model, BodyUnitEntry? Refusal) BindReducer(SemanticApplicationContext context, SemanticImplementationRequirement requirement)
    {
        var reducers = context.Reducers.Where(reducer => reducer.Transitions.Any(transition => transition.RequirementId == requirement.RequirementId)).ToArray();
        if (reducers.Length != 1) return (null, null, Refuse(requirement, "STAGE-ESM-021", "The reducer transition is not uniquely bound to a reducer."));
        var reducer = reducers[0];
        if (reducer.Target != SemanticProjectionTargetKind.ReadModel || !context.ReadModels.TryGetValue(reducer.ReadModel, out var model))
            return (null, null, Refuse(requirement, "STAGE-IMPL-003", $"Reducer '{reducer.Name}' does not build a read model; event-target reducers are not executed by `cratis run` yet (Stage#891, not scheduled)."));
        return (reducer, model, null);
    }
}
