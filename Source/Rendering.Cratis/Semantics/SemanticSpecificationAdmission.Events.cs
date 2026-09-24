// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Checks event expectations and property values.
/// </summary>
internal static partial class SemanticSpecificationAdmission
{
    static bool EventMatches(SemanticApplicationContext context, SemanticSpecificationEvent expected) =>
        context.Events.TryGetValue(expected.EventContract, out var @event) && ValuesMatch(expected.Values, @event.Properties);

    // The reference evaluator puts an unqualified given fact on the Null destination. A command scenario
    // cannot seed that destination without changing its identity, so only explicitly sourced facts are admitted.
    static bool HasRenderableGivenEvents(SemanticApplicationContext context, SemanticSpecification specification) =>
        specification.GivenEvents.All(given => given.EventSource is { } source &&
            IsScalar(source.Value) && IsLosslessEventSource(context, source.Type) && EventMatches(context, given));

    static bool ValuesMatch(
        System.Collections.Immutable.ImmutableArray<SemanticPropertyValue> values,
        System.Collections.Immutable.ImmutableArray<SemanticProperty> properties,
        bool exactly = true) =>
        (!exactly || values.Length == properties.Length) &&
        values.Select(value => value.TargetProperty).Distinct().Count() == values.Length &&
        values.All(value =>
            properties.Any(property => value.TargetProperty == property.Id && IsCompatible(value.Value, property.Type)));

    static bool IsCompatible(SemanticValue value, SemanticTypeReference type)
    {
        if (value is SemanticNullValue)
        {
            return type.IsOptional;
        }

        if (type.IsCollection)
        {
            return value is SemanticArrayValue array && array.Values.All(IsScalar);
        }

        return IsScalar(value);
    }

    static bool IsScalar(SemanticValue value) => value is SemanticTextValue or SemanticNumberValue or SemanticBooleanValue;
}
