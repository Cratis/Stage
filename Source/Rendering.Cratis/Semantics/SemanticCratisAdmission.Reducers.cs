// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Admits verified, typed, pure reducer transitions only.</summary>
internal static partial class SemanticCratisAdmission
{
    static int ValidateReducers(SemanticApplicationContext context, SemanticSlice slice, List<ArtifactRenderDiagnostic> diagnostics)
    {
        var analysed = 0;
        foreach (var reducer in slice.Reducers)
        {
            if (slice.Kind != SemanticSliceKind.StateView)
            {
                diagnostics.Add(Error("STAGE-ESM-019", $"Reducer '{reducer.Name}' must be declared in a StateView slice.", slice.Id));
                continue;
            }

            if (!slice.ReadModels.Any(_ => _.Id == reducer.ReadModel))
            {
                diagnostics.Add(Error("STAGE-ESM-019", $"Reducer '{reducer.Name}' needs its read model in the same slice.", slice.Id));
                continue;
            }

            var reducerName = Identifiers.ToPascalCase(reducer.Name);
            if (slice.ReadModels.Any(_ => Identifiers.ToPascalCase(_.Name) == reducerName) ||
                slice.Events.Any(_ => Identifiers.ToPascalCase(_.Name) == reducerName) ||
                slice.Projections.Any(_ => _.Scope is not null && Identifiers.ToPascalCase(_.Name) == reducerName) ||
                SliceNaming.FileName(slice.Name) == $"{reducerName}.cs")
            {
                diagnostics.Add(Error("STAGE-ESM-019", $"Reducer '{reducer.Name}' collides with another generated class name in the slice.", slice.Id));
                continue;
            }

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

                var members = descriptors[0].Members;
                var expected = new (string Name, string Kind, string Path, bool Derived)[]
                {
                    ("State", SemanticContextSourceKinds.ReadModel, "State", false),
                    ("Event", SemanticContextSourceKinds.CurrentEvent, "Event", false),
                    ("Key", SemanticContextSourceKinds.EventSourceId, "eventSourceId", false),
                    ("Tenant", SemanticContextSourceKinds.ContextContract, "Tenant", false),
                    ("Occurred", SemanticContextSourceKinds.ContextContract, "Occurred", false),
                    ("SequenceNumber", SemanticContextSourceKinds.ContextContract, "SequenceNumber", false),
                    ("IsFirst", SemanticContextSourceKinds.Derived, "State", true)
                };
                if (members.Length != expected.Length || !members.Zip(expected).All(pair =>
                    pair.First.Name == pair.Second.Name && pair.First.Source?.Kind == pair.Second.Kind &&
                    pair.First.Source.Path == pair.Second.Path && pair.First.IsDerived == pair.Second.Derived))
                {
                    diagnostics.Add(Error("STAGE-ESM-021", $"Reducer '{reducer.Name}' has an unexpected typed context member or source (State, Event, Key, Tenant, Occurred, SequenceNumber, IsFirst).", reducer.ReadModel));
                    continue;
                }

                try
                {
                    var verdict = PureTransitionAdmission.Analyze(body!, context, model, @event, descriptors[0], requirement);
                    analysed++;
                    if (!verdict.Accepted)
                    {
                        diagnostics.Add(Error(verdict.Code!, $"Reducer '{reducer.Name}' transition '{transition.RequirementId}': {verdict.Reason}", reducer.ReadModel));
                    }
                }
                catch (Exception exception)
                {
                    diagnostics.Add(Error("STAGE-ESM-021", $"Reducer '{reducer.Name}' transition '{transition.RequirementId}' could not be analysed: {exception.Message}", reducer.ReadModel));
                }
            }
        }

        return analysed;
    }
}
