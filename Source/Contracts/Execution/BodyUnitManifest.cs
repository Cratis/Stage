// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>Admission and executable identity for one implementation requirement.</summary>
public sealed record BodyUnitEntry(
    string RequirementId,
    string Role,
    bool Admitted,
    string? DiagnosticCode = null,
    string? DiagnosticMessage = null,
    string? UnitHash = null,
    string? PeSha256 = null,
    string? EntryType = null,
    string? EntryMethod = null,
    string? WrapperType = null)
{
    /// <summary>Gets the hash of the original resolved implementation content.</summary>
    public string? BodySha256 { get; init; }

    /// <summary>Gets the authored-to-wire property mapping used by the rendered wrapper.</summary>
    public ImmutableDictionary<string, string> PropertyNames { get; init; } = [];

    /// <summary>Gets authored property paths normalized through the renderer's wire-name mapping.</summary>
    public ImmutableDictionary<string, string> PropertyPaths { get; init; } = [];

    /// <summary>Gets the reducer state mapping from a rendered type's full name and member to its semantic property.</summary>
    public ImmutableDictionary<string, string> StateMembers { get; init; } = [];

    /// <summary>Gets the mapping from a rendered enum's full name and member to its authored value.</summary>
    public ImmutableDictionary<string, string> EnumValues { get; init; } = [];
}

/// <summary>The immutable compilation result for one model revision.</summary>
public sealed record BodyUnitManifest(string Revision, ImmutableArray<BodyUnitEntry> Entries);
