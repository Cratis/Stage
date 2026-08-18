// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Stage.Contracts.Commands;

namespace Cratis.Stage.Contracts.Captures;

/// <summary>
/// Represents one operation in a capture's <c>map</c> body - a value the capture reshapes before it appends.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(CaptureMapping), "mapping")]
[JsonDerivedType(typeof(CaptureSplit), "split")]
public abstract record CaptureMapOperation;

/// <summary>
/// Represents a single value translation within a mapping, such as <c>"utkast" =&gt; draft</c>.
/// </summary>
/// <param name="From">The source value being translated.</param>
/// <param name="To">The target value.</param>
public record CaptureTranslation(string From, string To);

/// <summary>
/// Represents a <c>map</c> entry translating a source value into a target property.
/// </summary>
/// <param name="Property">The target property.</param>
/// <param name="SourceKind">Where the value comes from.</param>
/// <param name="Source">The source, interpreted according to <paramref name="SourceKind"/>.</param>
/// <param name="Translations">The value translations applied to it, in declaration order; empty when the value
/// is carried across unchanged.</param>
public record CaptureMapping(
    string Property,
    ProducedValueKind SourceKind,
    string Source,
    IReadOnlyList<CaptureTranslation> Translations) : CaptureMapOperation;

/// <summary>
/// Represents a <c>split</c> operation, such as <c>split fullName by " "</c>, filling several target properties
/// from one source value.
/// </summary>
/// <param name="SourceKind">Where the value being split comes from.</param>
/// <param name="Source">The source, interpreted according to <paramref name="SourceKind"/>.</param>
/// <param name="Separator">The separator the value is split by.</param>
/// <param name="Targets">The target properties receiving the parts, in order.</param>
public record CaptureSplit(
    ProducedValueKind SourceKind,
    string Source,
    string Separator,
    IReadOnlyList<string> Targets) : CaptureMapOperation;
