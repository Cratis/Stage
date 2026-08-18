// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Commands;

namespace Cratis.Stage.Contracts.Captures;

/// <summary>
/// Defines what makes a capture append an event.
/// </summary>
public enum CaptureTriggerKind
{
    /// <summary>
    /// Append when a named property changes.
    /// </summary>
    PropertyChanged = 0,

    /// <summary>
    /// Append when an item appears in the source.
    /// </summary>
    Added = 1,

    /// <summary>
    /// Append when an item disappears from the source.
    /// </summary>
    Removed = 2,

    /// <summary>
    /// Append when an item changes in the source.
    /// </summary>
    Changed = 3,

    /// <summary>
    /// Append when any of several named properties changes, combined with <c>or</c>.
    /// </summary>
    LogicalOr = 4,

    /// <summary>
    /// Append when all of several named properties change, combined with <c>and</c>.
    /// </summary>
    LogicalAnd = 5,

    /// <summary>
    /// Append when a named property transitions from one specific value to another.
    /// </summary>
    ValueTransition = 6,

    /// <summary>
    /// Append when a template expression evaluates to true.
    /// </summary>
    Expression = 7
}

/// <summary>
/// Represents what makes a <see cref="CaptureAppend"/> happen - the modeled <c>when</c> clause.
/// </summary>
/// <param name="Kind">What kind of change triggers the append.</param>
/// <param name="Properties">The properties taking part in the trigger - one for a property change or a value
/// transition, several for a combination, none for membership of the source.</param>
/// <param name="FromValue">The value transitioned away from, or <see langword="null"/> when the trigger is not
/// a value transition.</param>
/// <param name="ToValue">The value transitioned to, or <see langword="null"/> when the trigger is not a value
/// transition.</param>
/// <param name="Expression">The template expression that must evaluate to true, or <see langword="null"/> when
/// the trigger is not an expression.</param>
public record CaptureTrigger(
    CaptureTriggerKind Kind,
    IReadOnlyList<string> Properties,
    string? FromValue,
    string? ToValue,
    string? Expression);

/// <summary>
/// Represents an event a capture appends - the modeled <c>append</c> declaration.
/// </summary>
/// <param name="Event">The name of the event type to append.</param>
/// <param name="When">What makes the append happen, or <see langword="null"/> when the capture appends on every
/// change it observes.</param>
/// <param name="Mappings">How each property of the event payload gets its value.</param>
/// <param name="Tags">The tags to append the event with.</param>
/// <remarks>
/// Property mappings reuse <see cref="ProducedEventProperty"/> so a value filled from a capture and one filled
/// from a command are described the same way. A capture's source properties arrive as
/// <see cref="ProducedValueKind.CommandProperty"/> for that reason - the kind names a path into whatever the
/// append is reading, and the source it reads is stated by the capture rather than by every mapping.
/// </remarks>
public record CaptureAppend(
    string Event,
    CaptureTrigger? When,
    IReadOnlyList<ProducedEventProperty> Mappings,
    IReadOnlyList<string> Tags);
