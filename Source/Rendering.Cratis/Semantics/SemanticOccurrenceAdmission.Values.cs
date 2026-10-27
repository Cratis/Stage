// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticOccurrenceAdmission
{
    static bool ValueMatches(SemanticValue value, SemanticTypeReference type, SemanticApplicationContext context)
    {
        if (value is SemanticNullValue) return type.IsOptional;
        if (type.IsCollection) return CollectionMatches(value, type, context);
        return type.Kind switch
        {
            SemanticTypeReferenceKind.CompositeType => CompositeMatches(value, type, context),
            SemanticTypeReferenceKind.Concept => ConceptMatches(value, type, context),
            SemanticTypeReferenceKind.Primitive => PrimitiveMatches(value, type.Primitive, context),
            _ => false
        };
    }

    static bool CollectionMatches(SemanticValue value, SemanticTypeReference type, SemanticApplicationContext context) =>
        value is SemanticArrayValue array && array.Values.All(item => ValueMatches(item, type with { IsCollection = false, IsOptional = false }, context));

    static bool CompositeMatches(SemanticValue value, SemanticTypeReference type, SemanticApplicationContext context) =>
        context.Types.TryGetValue(type.Target, out var composite) && value is SemanticCompositeValue structured &&
        CompositePropertiesMatch(structured, composite, context);

    static bool CompositePropertiesMatch(SemanticCompositeValue value, SemanticCompositeType type, SemanticApplicationContext context) =>
        value.Properties.Length == type.Properties.Length && type.Properties.All(property => PropertyMatches(value, property, context));

    static bool PropertyMatches(SemanticCompositeValue value, SemanticProperty property, SemanticApplicationContext context) =>
        value.Properties.Count(field => field.TargetProperty == property.Id && ValueMatches(field.Value, property.Type, context)) == 1;

    static bool ConceptMatches(SemanticValue value, SemanticTypeReference type, SemanticApplicationContext context)
    {
        if (!context.Concepts.TryGetValue(type.Target, out var concept)) return false;
        return concept.Values.IsEmpty ? PrimitiveMatches(value, concept.Primitive, context) : EnumeratedConceptMatches(value, concept);
    }

    static bool EnumeratedConceptMatches(SemanticValue value, SemanticConcept concept) =>
        value is SemanticTextValue text && concept.Values.Contains(text.Value, StringComparer.Ordinal);

    static bool WholeNumberMatches(decimal number, SemanticApplicationContext context) => decimal.Truncate(number) == number &&
        (context.Request.Model.SemanticVersion.IsAtLeast(SemanticVersion.V8) ? number is >= long.MinValue and <= long.MaxValue : number is >= int.MinValue and <= int.MaxValue);

    static bool PrimitiveMatches(SemanticValue value, SemanticPrimitiveType primitive, SemanticApplicationContext context) => (value, primitive) switch
    {
        (SemanticTextValue, SemanticPrimitiveType.Text) or (SemanticBooleanValue, SemanticPrimitiveType.Boolean) => true,
        (SemanticNumberValue number, _) => NumberMatches(number.Value, primitive, context),
        (SemanticTextValue text, _) => TextLiteralMatches(text.Value, primitive),
        _ => false
    };

    static bool NumberMatches(decimal value, SemanticPrimitiveType primitive, SemanticApplicationContext context) => primitive switch
    {
        SemanticPrimitiveType.DecimalNumber => true,
        SemanticPrimitiveType.WholeNumber => WholeNumberMatches(value, context),
        _ => false
    };

    static bool TextLiteralMatches(string value, SemanticPrimitiveType primitive) => primitive switch
    {
        SemanticPrimitiveType.Uuid => Guid.TryParse(value, out _),
        SemanticPrimitiveType.Date => DateOnly.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _),
        SemanticPrimitiveType.DateTime => DateTimeOffset.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _),
        _ => false
    };
}
