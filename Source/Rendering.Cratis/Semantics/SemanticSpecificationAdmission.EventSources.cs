// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

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
        if (!specification.ThenErrors.IsEmpty)
        {
            return true;
        }

        var sources = SemanticDestinations.Explicit(specification).Distinct().ToArray();
        if (sources.Length == 0)
        {
            return true;
        }

        var destination = command.Produces.Length == 1 ? SemanticDestinations.Of(command, command.Produces[0]) as SemanticResolvedExpression : null;
        var property = command.Properties.SingleOrDefault(_ => _.Id == destination?.Target);
        return sources.Length == 1 && !specification.ThenEvents.IsEmpty && property is not null &&
            sources[0].Type == property.Type && IsScalar(sources[0].Value) && IsLosslessEventSource(context, property.Type);
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
