// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticSpecificationAdmission
{
    static bool EventMatches(SemanticApplicationContext context, SemanticSpecificationEvent expected) =>
        context.Events.TryGetValue(expected.EventContract, out var @event) && ValuesMatch(expected.Values, @event.Properties);

    static bool ValuesMatch(
        System.Collections.Immutable.ImmutableArray<SemanticPropertyValue> values,
        System.Collections.Immutable.ImmutableArray<SemanticProperty> properties) =>
        values.Length == properties.Length && properties.All(property =>
            values.Any(value => value.TargetProperty == property.Id && IsCompatible(value.Value, property.Type)));

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
