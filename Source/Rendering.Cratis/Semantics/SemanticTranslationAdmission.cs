// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Admits the translation subset without changing the reference executor's feature inventory.
/// </summary>
internal static class SemanticTranslationAdmission
{
    internal static IEnumerable<SemanticVersionFeature> AdmittedFeatures(SemanticSlice slice, SemanticApplicationContext context)
    {
        if (!SemanticTranslationSupport.RenderableSlice(slice, context.Events)) yield break;
        foreach (var feature in SemanticVersionFeatures.InSlice(slice))
        {
            if (feature.Artifact == slice.Id ||
                slice.Projections.Any(projection => projection.Id == feature.Artifact && SemanticTranslationSupport.RenderableProjection(projection, context.Events)) ||
                slice.Captures.Any(capture => capture.Id == feature.Artifact && SemanticTranslationSupport.RenderableCapture(capture, context.Events)))
            {
                yield return feature;
            }
        }
    }

    internal static bool AdmitsEvent(SemanticId id, SemanticSlice slice, SemanticApplicationContext context) =>
        (SemanticTranslationSupport.RenderableSlice(slice, context.Events) || DeclarationOnly(slice, id, context) || ExpectationOnly(slice, id, context)) &&
        !LocallyProduces(slice, id) && context.Events.TryGetValue(id, out var contract) &&
        SemanticTranslationSupport.RenderableEvent(contract, context.AllSlices.Select(located => located.Slice), context.Events);

    internal static void Validate(SemanticApplicationContext context, SemanticSlice slice, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (!SemanticTranslationSupport.RenderableSlice(slice, context.Events))
        {
            diagnostics.Add(new(
                SemanticVersionFeatures.Automation,
                ArtifactRenderDiagnosticSeverity.Error,
                $"Translation slice '{slice.Name}' is outside the supported foreign-event capture and public-event projection subset.",
                slice.Id));
            return;
        }
        ValidateConstructs(slice, diagnostics);
        ValidateCaptures(context, slice, diagnostics);
        foreach (var projection in slice.Projections)
        {
            ValidateName(projection.Id, projection.Name, projection.Scope?.From.Select(from => from.EventContract) ?? [], context, diagnostics);
            var name = Identifiers.ToPascalCase(projection.Name);
            if (name == "SamePublicValue" || name == "PublicationAppendFailed")
            {
                diagnostics.Add(new(
                    "STAGE-ESM-012",
                    ArtifactRenderDiagnosticSeverity.Error,
                    name == "SamePublicValue"
                        ? $"Publication '{projection.Name}' collides with its generated record comparison member."
                        : $"Publication '{projection.Name}' collides with its generated append failure member.",
                    projection.Id));
            }
        }
        foreach (var specification in slice.Specifications)
        {
            if (SemanticTranslationSpecificationAdmission.CannotRender(context, slice, specification) is { } reason)
            {
                diagnostics.Add(new(
                    "STAGE-ESM-011",
                    ArtifactRenderDiagnosticSeverity.Error,
                    $"Specification '{specification.Name}' cannot render: {reason}",
                    specification.Id));
            }
        }
    }

    internal static IEnumerable<GeneratedTypeDeclaration> Declarations(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices) =>
        slices.Where(located => located.Slice.Kind == SemanticSliceKind.Translate).SelectMany(located =>
            located.Slice.Captures.Select(capture => new GeneratedTypeDeclaration(capture.Id, "Capture", string.Join('.', located.Path.Select(Identifiers.ToPascalCase)), context.CaptureMembers(capture).TypeName)));

    static bool DeclarationOnly(SemanticSlice slice, SemanticId id, SemanticApplicationContext context) =>
        slice.Events.Any(candidate => candidate.Id == id) &&
        !SemanticVersionFeatures.EventReferences(slice with { Events = [] }, context.Events).Any(feature => feature.Artifact == id);

    static bool ExpectationOnly(SemanticSlice slice, SemanticId id, SemanticApplicationContext context) =>
        context.Events.TryGetValue(id, out var contract) && contract.Visibility == SemanticEventVisibility.Public &&
        slice.Specifications.Any(specification => specification.ThenEvents.Any(expected => expected.EventContract == id)) &&
        !SemanticVersionFeatures.EventReferences(slice with { Specifications = [] }, context.Events).Any(feature => feature.Artifact == id) &&
        slice.Specifications.All(specification => specification.GivenEvents.All(given => given.EventContract != id) &&
            specification.WhenAppended?.EventContract != id);

    static bool LocallyProduces(SemanticSlice slice, SemanticId id) =>
        slice.Commands.SelectMany(command => command.Produces).Any(produced => produced.EventContract == id) ||
        slice.Captures.SelectMany(capture => capture.Appends).Any(append => append.EventContract == id) ||
        (slice.Reactions.IsDefault ? [] : slice.Reactions).SelectMany(reaction => reaction.Triggers)
            .Any(trigger => trigger.Produces.Concat(trigger.Invokes.SelectMany(invocation => invocation.OnRefused.SelectMany(branch => branch.Produces)))
                .Any(produced => produced.EventContract == id));

    static void ValidateConstructs(SemanticSlice slice, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (!slice.Commands.IsEmpty || !slice.ReadModels.IsEmpty || !slice.Queries.IsEmpty || !slice.Reducers.IsEmpty || !slice.Reactions.IsDefaultOrEmpty ||
            slice.Projections.Any(projection => projection.Target != SemanticProjectionTargetKind.Event))
        {
            diagnostics.Add(new(
                SemanticVersionFeatures.Automation,
                ArtifactRenderDiagnosticSeverity.Error,
                $"Translation slice '{slice.Name}' mixes translation with an unsupported command, read surface or reaction.",
                slice.Id));
        }
    }

    static void ValidateCaptures(SemanticApplicationContext context, SemanticSlice slice, List<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var capture in slice.Captures)
        {
            foreach (var reason in context.CaptureMembers(capture).Refusals)
            {
                diagnostics.Add(new(
                    "STAGE-ESM-004",
                    ArtifactRenderDiagnosticSeverity.Error,
                    $"Capture '{capture.Name}' cannot render: {reason}.",
                    capture.Id));
            }
            if (SemanticCaptureIdentitySupport.Check(capture, context.Request.Model, context.Events) is { } identityFailure)
            {
                diagnostics.Add(new(SemanticCaptureIdentitySupport.Code, ArtifactRenderDiagnosticSeverity.Error, identityFailure, capture.Id));
            }
            foreach (var reason in SemanticCaptureConditionSupport.Refusals(capture, context).Distinct(StringComparer.Ordinal))
            {
                diagnostics.Add(new(
                    SemanticVersionFeatures.Automation,
                    ArtifactRenderDiagnosticSeverity.Error,
                    $"Capture '{capture.Name}' cannot render: {reason}.",
                    capture.Id));
            }
            ValidateSource(capture, context, diagnostics);
        }
    }

    static void ValidateSource(SemanticCapture capture, SemanticApplicationContext context, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (capture.EventsSource is not { } source) return;
        var contracts = source.Events.Select(id => context.Events[id]).ToArray();
        if (contracts.Select(contract => contract.Origin).Distinct(StringComparer.Ordinal).Count() != 1)
        {
            diagnostics.Add(new(
                SemanticVersionFeatures.ForeignEvents,
                ArtifactRenderDiagnosticSeverity.Error,
                $"Capture '{capture.Name}' reads several foreign event stores; an inbox reactor must read one origin.",
                capture.Id));
        }
        foreach (var append in capture.Appends)
        {
            var target = context.Events[append.EventContract];
            foreach (var mapping in append.Mappings.Where(mapping => mapping.Field is not null))
            {
                var destination = target.Properties.Single(property => property.Id == mapping.TargetProperty);
                foreach (var contract in contracts)
                {
                    if (contract.Properties.SingleOrDefault(property => property.Name == mapping.Field) is { } property && !Assignable(property.Type, destination.Type))
                    {
                        diagnostics.Add(new(
                            SemanticVersionFeatures.Automation,
                            ArtifactRenderDiagnosticSeverity.Error,
                            $"Capture '{capture.Name}' field '{contract.Name}.{mapping.Field}' and target '{target.Name}.{destination.Name}' have different types; implicit capture field coercion is not rendered.",
                            capture.Id));
                    }
                }
            }
        }
        foreach (var mapping in capture.Appends.SelectMany(append => append.Mappings).Where(mapping => mapping.Field is not null))
        {
            if (contracts.All(contract => contract.Properties.All(property => property.Name != mapping.Field)))
            {
                diagnostics.Add(new(
                    SemanticVersionFeatures.Automation,
                    ArtifactRenderDiagnosticSeverity.Error,
                    $"Capture '{capture.Name}' mapping '{mapping.Field}' names a field no source event carries, so the capture state can never hold it.",
                    capture.Id));
            }
        }
    }

    static void ValidateName(SemanticId artifact, string name, IEnumerable<SemanticId> events, SemanticApplicationContext context, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (events.Any(id => $"On{Identifiers.ToPascalCase(context.Events[id].Name)}" == Identifiers.ToPascalCase(name)))
        {
            diagnostics.Add(new(
                "STAGE-ESM-004",
                ArtifactRenderDiagnosticSeverity.Error,
                $"Translation artifact '{name}' collides with one of its generated reactor members.",
                artifact));
        }
    }

    static bool Assignable(SemanticTypeReference source, SemanticTypeReference target) =>
        source with { IsOptional = false } == target with { IsOptional = false } && (!source.IsOptional || target.IsOptional);
}
