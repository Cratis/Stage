// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticCratisAdmission
{
    static bool InvalidPayloadMappings(SemanticCommand command, SemanticProducedEvent produced, SemanticEventContract @event)
    {
        if (command.Handler is not null) return false;
        if (produced.Mappings.Length != @event.Properties.Length) return true;
        return !@event.Properties.All(property => produced.Mappings.Any(mapping => mapping.TargetProperty == property.Id && SupportsPayloadSource(mapping.Source, property.Type, command)));
    }

    static bool SupportsPayloadSource(SemanticExpression source, SemanticTypeReference type, SemanticCommand command) =>
        IsProperty(source, SemanticExpressionRootKind.Command, command.Properties.Select(property => property.Id)) ||
        (source is SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred } occurrence && occurrence.Type == type) ||
        source is SemanticValueExpression || source is SemanticReadPathExpression;
}
