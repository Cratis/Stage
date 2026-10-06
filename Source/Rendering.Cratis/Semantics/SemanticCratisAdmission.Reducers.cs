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

            // A Chronicle reducer is stored under its event source, not under a key returned
            // by its body. Only identities whose canonical wire representation we can compare
            // at every public On boundary are safe for keyed model lookups.
            var identifiers = model.Properties.Where(_ => _.IsIdentifier).ToArray();
            if (identifiers.Length != 1 || !new SemanticTypeSystem(context).SupportsReducerIdentifier(identifiers[0].Type) ||
                !slice.Queries.Any(query => query.ReadModel == model.Id && query.KeyProperty == identifiers[0].Id &&
                    query.Argument.Type == identifiers[0].Type && query.Cardinality == SemanticQueryCardinality.ZeroOrOne &&
                    query.Delivery == SemanticQueryDelivery.Snapshot))
            {
                diagnostics.Add(Error("STAGE-ESM-019", $"Reducer '{reducer.Name}' needs a required Guid/Text identifier (or identifier concept) and a matching keyed query.", reducer.ReadModel));
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
                    !context.Events.TryGetValue(transition.EventContract, out var @event))
                {
                    diagnostics.Add(Error("STAGE-ESM-019", $"Reducer '{reducer.Name}' has an unsupported transition envelope, language or event revision.", reducer.ReadModel));
                    continue;
                }

                // A nested composite with an interface-typed collection can still expose a mutable
                // List or array. Refuse it until the whole composite closure is snapshotted.
                if (context.Types.Values.Any(composite => composite.Properties.Any(property => property.Type.IsCollection)) &&
                    model.Properties.Concat(@event.Properties).Any(property => property.Type.Kind == SemanticTypeReferenceKind.CompositeType))
                {
                    diagnostics.Add(Error("STAGE-ESM-019", $"Reducer '{reducer.Name}' cannot read nested mutable composite collections.", reducer.ReadModel));
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
                var expected = new (string Name, string Kind, SemanticId? Id, string Path, EventContractRevision? Revision, bool Derived)[]
                {
                    ("State", SemanticContextSourceKinds.ReadModel, model.Id, "State", null, false),
                    ("Event", SemanticContextSourceKinds.CurrentEvent, @event.Id, "Event", @event.Revision, false),
                    ("Key", SemanticContextSourceKinds.EventSourceId, @event.Id, "eventSourceId", null, false),
                    ("Tenant", SemanticContextSourceKinds.ContextContract, null, "Tenant", null, false),
                    ("Occurred", SemanticContextSourceKinds.ContextContract, null, "Occurred", null, false),
                    ("SequenceNumber", SemanticContextSourceKinds.ContextContract, null, "SequenceNumber", null, false),
                    ("IsFirst", SemanticContextSourceKinds.Derived, null, "State", null, true)
                };
                if (members.Length != expected.Length || !members.Zip(expected).All(pair =>
                    pair.First.Name == pair.Second.Name && pair.First.Source?.Kind == pair.Second.Kind &&
                    pair.First.Source.SemanticId == pair.Second.Id && pair.First.Source.Path == pair.Second.Path &&
                    pair.First.Source.EventRevision == pair.Second.Revision && pair.First.Source.ConstantValue is null &&
                    pair.First.IsDerived == pair.Second.Derived && MatchesReducerMemberType(pair.First, model, @event)))
                {
                    diagnostics.Add(Error("STAGE-ESM-021", $"Reducer '{reducer.Name}' has an unexpected typed context member or source (State, Event, Key, Tenant, Occurred, SequenceNumber, IsFirst).", reducer.ReadModel));
                    continue;
                }

                // StateView artifacts emit read models and reducers, not event declarations,
                // including when the event is declared in a different StateView slice.
                if (context.DeclaringSlice(@event.Id).Slice.Kind != SemanticSliceKind.StateChange)
                {
                    diagnostics.Add(Error("STAGE-ESM-019", $"Reducer '{reducer.Name}' observes an event declared in a StateView slice, which cannot render an event contract.", reducer.ReadModel));
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

    static bool MatchesReducerMemberType(SemanticTypedContextMember member, SemanticReadModel model, SemanticEventContract @event)
    {
        var type = member.Type;
        if (type is null) return false;
        if (member.Name == "State" || member.Name == "Event")
        {
            var shape = member.Name == "State" ? model.Id : @event.Id;
            var properties = member.Name == "State" ? model.Properties : @event.Properties;
            return type.Kind == SemanticContextTypeKinds.Shape && type.Shape == shape && type.ModelType is null &&
                type.RuntimeToken is null && !type.Properties.IsDefault && type.Properties.Length == properties.Length &&
                type.Properties.Zip(properties).All(pair => pair.First.Id == pair.Second.Id &&
                    pair.First.Name == pair.Second.Name && pair.First.Type == pair.Second.Type) &&
                member.IsNullable == (member.Name == "State");
        }

        var token = member.Name switch
        {
            "Key" => SemanticContextRuntimeTokens.Text,
            "Tenant" => SemanticContextRuntimeTokens.TenantId,
            "Occurred" => SemanticContextRuntimeTokens.DateTime,
            "SequenceNumber" => SemanticContextRuntimeTokens.WholeNumber,
            "IsFirst" => SemanticContextRuntimeTokens.Boolean,
            _ => null
        };
        return token is not null && type.Kind == SemanticContextTypeKinds.Runtime && type.RuntimeToken == token &&
            type.ModelType is null && type.Shape is null && !type.Properties.IsDefault && type.Properties.IsEmpty &&
            !member.IsNullable;
    }
}
