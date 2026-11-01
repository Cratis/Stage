// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Canonicalizes typed DateTime assertion values using the Screenplay reference's round-trip text representation.
/// </summary>
/// <remarks>
/// Mirrors Cratis/Screenplay at 52af009f, Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Specifications.cs:17,504-509.
/// Only complete ISO instants with an explicit zone are canonicalized; other literals remain unchanged.
/// Zero offsets use UTC "O" text; non-zero offsets remain distinct for ordinal assertion equality.
/// Capture coercion separately follows Source/DotNET/Screenplay/Semantics/Execution/SemanticCaptureEvaluation.cs:435-443:
/// Stage's SemanticRunCaptures.Values.cs rejects invalid capture instants and converts valid ones to UTC before assertion comparison.
/// </remarks>
public static partial class SemanticDateTimeCanonicalization
{
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.NonBacktracking)]
    private static partial Regex IsoInstant { get; }

    /// <summary>
    /// Canonicalizes DateTime values, including concept-backed and nested values, without changing other primitives.
    /// </summary>
    /// <param name="value">The assertion or actual value.</param>
    /// <param name="type">The value's declared type.</param>
    /// <param name="application">The application declaring concepts and composite types.</param>
    /// <returns>The value with typed DateTime text in the reference's canonical form.</returns>
    public static SemanticValue Normalize(SemanticValue value, SemanticTypeReference type, SemanticApplication application) => value switch
    {
        SemanticArrayValue array when type.IsCollection =>
            SemanticValue.Array([.. array.Values.Select(item => Normalize(item, type with { IsCollection = false }, application))]),
        SemanticCompositeValue composite when type.Kind == SemanticTypeReferenceKind.CompositeType => NormalizeComposite(composite, type, application),
        SemanticTextValue text when !type.IsCollection && IsDateTime(type, application) => SemanticValue.Text(CanonicalText(text.Value)),
        _ => value
    };

    static SemanticValue NormalizeComposite(SemanticCompositeValue composite, SemanticTypeReference type, SemanticApplication application)
    {
        var properties = application.Types.Single(candidate => candidate.Id == type.Target).Properties;
        return SemanticValue.Composite([.. composite.Properties.Select(member => member with
        {
            Value = Normalize(member.Value, properties.Single(property => property.Id == member.TargetProperty).Type, application)
        })]);
    }

    static bool IsDateTime(SemanticTypeReference type, SemanticApplication application) =>
        (type.Kind == SemanticTypeReferenceKind.Concept
            ? application.Concepts.Single(concept => concept.Id == type.Target).Primitive : type.Primitive) == SemanticPrimitiveType.DateTime;

    static string CanonicalText(string value)
    {
        if (!IsoInstant.IsMatch(value) || !DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var instant)) return value;
        return instant.Offset == TimeSpan.Zero
            ? instant.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)
            : instant.ToString("O", CultureInfo.InvariantCulture);
    }
}
