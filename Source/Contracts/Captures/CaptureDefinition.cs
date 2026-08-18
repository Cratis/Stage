// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Captures;

/// <summary>
/// Represents a single setting of a capture source, such as <c>route /invoices</c> or <c>poll 5m</c>.
/// </summary>
/// <param name="Name">The name of the setting.</param>
/// <param name="Value">The verbatim value of the setting.</param>
public record CaptureSourceSetting(string Name, string Value);

/// <summary>
/// Represents where a capture reads from - the modeled <c>source</c> declaration.
/// </summary>
/// <param name="Kind">The kind of source, such as <c>api</c>, <c>webhook</c> or <c>message</c>.</param>
/// <param name="Settings">The settings of the source, in declaration order.</param>
/// <remarks>
/// The kind stays the text the document wrote rather than becoming an enumeration, because the set of sources
/// a capture can read from is open - a host adds one without the language, or this contract, gaining a member.
/// </remarks>
public record CaptureSource(string Kind, IReadOnlyList<CaptureSourceSetting> Settings);

/// <summary>
/// Represents a <c>capture</c> declared within a slice - the translation of an external source into events.
/// </summary>
/// <param name="Id">The unique identifier of the capture.</param>
/// <param name="Name">The name of the capture.</param>
/// <param name="Source">Where the capture reads from, or <see langword="null"/> when it declares no source.</param>
/// <param name="Key">The source property identifying an instance, or <see langword="null"/> when none is declared.</param>
/// <param name="Map">The value mappings applied to the source before appending.</param>
/// <param name="Appends">The events appended for the captured instance itself.</param>
/// <param name="Children">The child collections of the source that are captured in their own right.</param>
/// <param name="Nested">The nested objects of the source that are captured in their own right.</param>
public record CaptureDefinition(
    Guid Id,
    string Name,
    CaptureSource? Source,
    string? Key,
    IReadOnlyList<CaptureMapOperation> Map,
    IReadOnlyList<CaptureAppend> Appends,
    IReadOnlyList<CaptureChildren> Children,
    IReadOnlyList<CaptureNested> Nested);

/// <summary>
/// Represents a <c>children</c> block of a capture - changes within a child collection of the source.
/// </summary>
/// <param name="Property">The child collection property.</param>
/// <param name="IdentifiedBy">The property identifying a child instance.</param>
/// <param name="Map">The value mappings applied to each child before appending.</param>
/// <param name="Appends">The events appended for the child collection.</param>
public record CaptureChildren(
    string Property,
    string IdentifiedBy,
    IReadOnlyList<CaptureMapOperation> Map,
    IReadOnlyList<CaptureAppend> Appends);

/// <summary>
/// Represents a <c>nested</c> block of a capture - a single nullable child object of the source.
/// </summary>
/// <param name="Property">The nested object property.</param>
/// <param name="Map">The value mappings applied to the nested object before appending.</param>
/// <param name="Appends">The events appended for the nested object.</param>
public record CaptureNested(
    string Property,
    IReadOnlyList<CaptureMapOperation> Map,
    IReadOnlyList<CaptureAppend> Appends);
