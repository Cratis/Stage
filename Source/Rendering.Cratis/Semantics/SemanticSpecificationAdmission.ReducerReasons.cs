// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticSpecificationAdmission
{
    static string? ReducerRejectionReason(SemanticApplicationContext context, SemanticId readModel)
    {
        var reducer = context.Reducers.SingleOrDefault(_ => _.ReadModel == readModel);
        if (reducer is null) return null;
        foreach (var transition in reducer.Transitions)
        {
            var reason = ReducerTransitionRejectionReason(context, readModel, transition.RequirementId, transition.EventContract);
            if (reason is not null)
            {
                var eventName = context.Events.TryGetValue(transition.EventContract, out var @event) ? @event.Name : transition.EventContract.ToString();
                var readModelName = context.ReadModels.TryGetValue(readModel, out var model) ? model.Name : readModel.ToString();
                return $"Reducer '{reducer.Name}' for read model '{readModelName}' triggered by '{eventName}' cannot be verified: {reason}";
            }
        }

        return null;
    }

    static string? ReducerTransitionRejectionReason(SemanticApplicationContext context, SemanticId readModel, string requirementId, SemanticId eventContract)
    {
        var descriptor = context.Request.TypedContextDescriptors.SingleOrDefault(_ => _.RequirementId == requirementId && _.OperationId == readModel);
        var requirement = context.Request.ImplementationRequirements.SingleOrDefault(_ => _.RequirementId == requirementId);
        if (descriptor is null) return "The typed context descriptor is missing.";
        if (requirement is null) return "The implementation requirement is missing.";
        var diagnostics = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
        if (!SemanticImplementationAdmission.TryGetVerifiedBody(context.Request, requirementId, readModel, diagnostics, out var body))
            return diagnostics.Count == 0 ? "The reducer body could not be verified." : string.Join(' ', diagnostics.Select(_ => $"{_.Code}: {_.Message}"));

        // Specification admission must fail closed too when analysis cannot classify context reads.
        try
        {
            var verdict = PureTransitionAdmission.Analyze(body!, context, context.ReadModels[readModel], context.Events[eventContract], descriptor, requirement);
            if (!verdict.Accepted) return $"{verdict.Code}: {verdict.Reason}";
            return UnstableReducerContextReason(verdict.ContextReads);
        }
        catch (Exception exception)
        {
            return $"Reducer analysis failed: {exception.GetType().Name}: {exception.Message}";
        }
    }

    static string? UnstableReducerContextReason(IEnumerable<string> contextReads)
    {
        var unstable = string.Join(", ", contextReads.Where(read => read == "Occurred" || read == "SequenceNumber" || read == "Tenant").Order(StringComparer.Ordinal).Select(read => $"context.{read}"));
        return unstable.Length == 0 ? null : $"A reducer body reads {unstable}, which Chronicle's ReadModelScenario cannot supply deterministically.";
    }
}
