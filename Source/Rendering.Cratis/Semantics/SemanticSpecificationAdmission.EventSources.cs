// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Checks specification event-source identity assertions.
/// </summary>
internal static partial class SemanticSpecificationAdmission
{
    // An explicit ESM v2 source is asserted on the appended event, so an accepted specification that states one
    // must also expect that event, and every source it states must name the same stream. A rejection appends
    // nothing, so the reference runner never compares its command source.
    // The reference runner compares a stated source with the fact's typed destination, so the stated type must be
    // the destination property's type, and it must convert to a Chronicle event source id without losing identity.
    static bool HasRenderableEventSources(
        SemanticApplicationContext context,
        SemanticSpecification specification,
        SemanticCommand command)
    {
        if (!specification.ThenErrors.IsEmpty || specification.ThenDenied)
        {
            return true;
        }

        if (command.Produces.Length == 1)
        {
            var sources = SemanticDestinations.Explicit(specification).Distinct().ToArray();
            if (sources.Length == 0)
            {
                return true;
            }

            var destination = SemanticDestinations.Of(command, command.Produces[0]) as SemanticResolvedExpression;
            var property = command.Properties.SingleOrDefault(_ => _.Id == destination?.Target);
            return sources.Length == 1 && !specification.ThenEvents.IsEmpty && property is not null &&
                sources[0].Type == property.Type && IsScalar(sources[0].Value) && IsLosslessEventSource(context, property.Type);
        }

        return command.Produces.Select((produced, index) => (produced, index)).All(item =>
        {
            var destination = SemanticDestinations.Of(command, item.produced) as SemanticResolvedExpression;
            var property = command.Properties.SingleOrDefault(_ => _.Id == destination?.Target);
            var value = property is null ? null : specification.When!.Values.SingleOrDefault(_ => _.TargetProperty == property.Id)?.Value;
            var source = specification.When!.EventSource;
            var expected = specification.ThenEventsInAnyOrder
                ? specification.ThenEvents.FirstOrDefault(_ => _.EventContract == item.produced.EventContract)
                : specification.ThenEvents[item.index];
            return property is not null && value is not null && IsLosslessEventSource(context, property.Type) &&
                (source is null || (source.Type == property.Type && Equals(source.Value, value))) &&
                (expected?.EventSource is null || (expected.EventSource.Type == property.Type && Equals(expected.EventSource.Value, value)));
        });
    }

    static bool IsLosslessEventSource(SemanticApplicationContext context, SemanticTypeReference type)
    {
        var primitive = type.Kind switch
        {
            SemanticTypeReferenceKind.Primitive => type.Primitive,
            SemanticTypeReferenceKind.Concept when context.Concepts.TryGetValue(type.Target, out var concept) => concept.Primitive,
            _ => SemanticPrimitiveType.Unknown
        };
        return primitive is SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid;
    }
}
