// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>Admission and executable identity for one implementation requirement.</summary>
/// <param name="RequirementId">The semantic id of the implementation requirement.</param>
/// <param name="Role">The implementation role the unit serves.</param>
/// <param name="Admitted">Whether the unit was admitted and compiled.</param>
/// <param name="DiagnosticCode">The refusal code when the unit was not admitted.</param>
/// <param name="DiagnosticMessage">The refusal message when the unit was not admitted.</param>
/// <param name="UnitHash">The hash identifying the compiled unit.</param>
/// <param name="PeSha256">The SHA-256 of the compiled assembly.</param>
/// <param name="EntryType">The full name of the type holding the entry point.</param>
/// <param name="EntryMethod">The name of the entry method.</param>
/// <param name="WrapperType">The full name of the snapshot wrapper type.</param>
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

    /// <summary>Gets the full names of the event types a command handler may return, including events without properties.</summary>
    public ImmutableArray<string> EventTypes { get; init; } = [];

    /// <summary>Gets the mapping from a rendered enum's full name and member to its authored value.</summary>
    public ImmutableDictionary<string, string> EnumValues { get; init; } = [];
}

/// <summary>The immutable compilation result for one model revision.</summary>
/// <param name="Revision">The model revision the units were compiled for.</param>
/// <param name="Entries">One entry per implementation requirement.</param>
public sealed record BodyUnitManifest(string Revision, ImmutableArray<BodyUnitEntry> Entries);
