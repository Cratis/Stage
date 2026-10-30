// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>Admission and executable identity for one implementation requirement.</summary>
/// <param name="RequirementId">The implementation requirement identity.</param>
/// <param name="Role">The implementation role name.</param>
/// <param name="Admitted">Whether the requirement was admitted for execution.</param>
/// <param name="DiagnosticCode">The diagnostic code when not admitted.</param>
/// <param name="DiagnosticMessage">The diagnostic message when not admitted.</param>
/// <param name="UnitHash">The hash of the compiled unit.</param>
/// <param name="PeSha256">The SHA-256 of the compiled assembly.</param>
/// <param name="EntryType">The full name of the entry type.</param>
/// <param name="EntryMethod">The name of the entry method.</param>
/// <param name="WrapperType">The full name of the rendered wrapper type.</param>
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
/// <param name="Revision">The model revision that was compiled.</param>
/// <param name="Entries">The per-requirement admission and identity entries.</param>
public sealed record BodyUnitManifest(string Revision, ImmutableArray<BodyUnitEntry> Entries);
