// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticSpecificationAdmission
{
    static bool TryRefuseUnboundEventMetadata(SemanticApplicationContext context, SemanticSpecification specification, ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        if (specification.GivenEvents.Any(fact => MissingObservableSource(context, fact)))
        {
            diagnostics.Add(RefusalDiagnostic(context, specification, SourcelessGivenReason));
            return true;
        }
        if (AssertsUncontrolledProjectionContext(context, specification))
        {
            diagnostics.Add(RefusalDiagnostic(context, specification, "Projected event-context occurrence, correlation and sequence expectations require explicitly bound event metadata."));
            return true;
        }

        return false;
    }

    static bool AssertsUncontrolledProjectionContext(SemanticApplicationContext context, SemanticSpecification specification) =>
        specification.ThenReadModels.Concat(specification.ThenQueries.SelectMany(expected => expected.Results)).Any(expected =>
            context.Projections.Values.Any(projection => projection.ReadModel == expected.ReadModel && projection.Scope is { } scope &&
                UncontrolledMappings(scope).Any(mapping => expected.Values.Any(value => mapping.Target.Contains(value.TargetProperty)))));

    static IEnumerable<SemanticProjectionMapping> UncontrolledMappings(SemanticProjectionScope scope) =>
        scope.From.SelectMany(from => from.Mappings).Concat(scope.Every?.Mappings ?? [])
            .Concat(scope.Joins.SelectMany(join => join.Mappings))
            .Where(mapping => mapping.Source is SemanticProjectionEventContextValue value &&
                (value.Path == "occurred" || value.Path == "correlationId" || value.Path == "sequenceNumber"));
}
