// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Admits verified, typed, pure reducer transitions only.</summary>
internal static partial class SemanticCratisAdmission
{
    static int ValidateReducers(SemanticApplicationContext context, SemanticSlice slice, List<ArtifactRenderDiagnostic> diagnostics)
    {
        var analysed = 0;
        foreach (var reducer in slice.Reducers)
        {
            var projections = context.Projections.Values.Count(_ => _.ReadModel == reducer.ReadModel);
            if (!context.ReadModels.TryGetValue(reducer.ReadModel, out var model) || projections != 0 ||
                context.Reducers.Count(_ => _.ReadModel == reducer.ReadModel) != 1 || reducer.Transitions.IsEmpty ||
                reducer.Transitions.Select(_ => _.EventContract).Distinct().Count() != reducer.Transitions.Length ||
                reducer.Key != SemanticReducerKey.EventSourceId || reducer.Result != SemanticReducerResult.StateOrDelete)
            {
                diagnostics.Add(Error("STAGE-ESM-019", $"Reducer '{reducer.Name}' needs one exclusive, resolvable read model and distinct event transitions.", slice.Id));
                continue;
            }

            foreach (var transition in reducer.Transitions)
            {
                var requirement = context.Request.ImplementationRequirements.FirstOrDefault(_ => _.RequirementId == transition.RequirementId);
                var verified = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
                if (!SemanticImplementationAdmission.TryGetVerifiedBody(context.Request, transition.RequirementId, reducer.ReadModel, verified, out var body))
                {
                    diagnostics.AddRange(verified);
                    continue;
                }

                if (requirement is null || requirement.Role != SemanticImplementationRole.ReducerTransition ||
                    requirement.ContextVersion != 1 || requirement.ResultVersion != 1 ||
                    requirement.RequiredCapability != "pure" ||
                    !((requirement.Language == "csharp" && requirement.File is null) ||
                        (requirement.Language is null && requirement.File?.EndsWith(".cs", StringComparison.Ordinal) == true)) ||
                    !context.Events.TryGetValue(transition.EventContract, out var @event) ||
                    @event.Revision.Value != 1 || !@event.PriorRevisions.IsEmpty)
                {
                    diagnostics.Add(Error("STAGE-ESM-019", $"Reducer '{reducer.Name}' has an unsupported transition envelope, language or event revision.", reducer.ReadModel));
                    continue;
                }

                var descriptors = context.Request.TypedContextDescriptors.Where(_ => _.RequirementId == transition.RequirementId &&
                    _.OperationId == reducer.ReadModel).ToArray();
                if (descriptors.Length != 1 || !descriptors[0].IsWrapperReady)
                {
                    diagnostics.Add(Error("STAGE-ESM-021", $"Reducer '{reducer.Name}' has no unique wrapper-ready typed context for transition '{transition.RequirementId}'.", reducer.ReadModel));
                    continue;
                }

                var verdict = PureTransitionAdmission.Analyze(body!, context, model, @event, descriptors[0], requirement);
                analysed++;
                if (!verdict.Accepted)
                {
                    diagnostics.Add(Error(verdict.Code!, $"Reducer '{reducer.Name}' transition '{transition.RequirementId}': {verdict.Reason}", reducer.ReadModel));
                }
            }
        }

        return analysed;
    }
}
