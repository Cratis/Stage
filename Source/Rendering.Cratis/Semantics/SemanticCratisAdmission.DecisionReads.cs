// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Explains each protected-read shape the Cratis target still refuses with STAGE-ESM-044.
/// </summary>
internal static partial class SemanticCratisAdmission
{
    static void ValidateDecisionReads(SemanticApplicationContext context, SemanticCommand command, List<ArtifactRenderDiagnostic> diagnostics)
    {
        var hasReads = !command.Reads.IsDefaultOrEmpty;
        var readPaths = command.Produces.SelectMany(produced => produced.Mappings.Select(mapping => (Produced: produced, Mapping: mapping)))
            .Where(item => item.Mapping.Source is SemanticReadPathExpression).ToArray();
        var scope = new SemanticReadScope(command, context);

        if (command.Produces.Any(produced => produced.When is { } condition && SemanticProductionSelection.ReadsView(condition)) ||
            command.Produces.Any(produced => produced.Destination is SemanticReadPathExpression) || command.Destination?.Value is SemanticReadPathExpression)
        {
            diagnostics.Add(DecisionRead(command, "reads a view in a produced-event condition or event-source destination; no reference vector preserves that decision boundary yet. Read the view in a requirement or an event mapping instead."));
        }

        foreach (var (produced, mapping) in readPaths)
        {
            var path = (SemanticReadPathExpression)mapping.Source;
            var viewProperty = scope.Property(path.Read, path.PropertyId);
            var eventProperty = context.Events.TryGetValue(produced.EventContract, out var @event) ? @event.Properties.FirstOrDefault(property => property.Id == mapping.TargetProperty) : null;
            if (viewProperty is null || eventProperty is null || viewProperty.Type.IsOptional ||
                viewProperty.Type != eventProperty.Type)
            {
                var label = viewProperty is null ? $"read path '{path.Read}'" : $"read path '{path.Read}.{viewProperty.Name}'";
                diagnostics.Add(DecisionRead(command, $"maps {label} to an event property with a different, optional, collection or composite type; the reference yields null for a missing value and Arc has no faithful shape for it. Map a required value of the same type."));
            }
        }

        if (hasReads && SemanticGeneratedFreshness.Destinations(command, context).Any())
        {
            diagnostics.Add(DecisionRead(command, "combines protected reads with generated-value freshness scopes; Arc's explicit scope must cover the reads and that interaction is unproven. Declare the reads on a command without generated event-source destinations."));
        }
    }

    static ArtifactRenderDiagnostic DecisionRead(SemanticCommand command, string reason) =>
        Error(SemanticVersionFeatures.ProtectedCommandReads, $"Command '{command.Name}' {reason}", command.Id);
}
