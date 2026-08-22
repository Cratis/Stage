// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Stage.Contracts.Commands;

namespace Cratis.Stage.Contracts.Reactions;

/// <summary>
/// Defines the units an interval trigger counts in.
/// </summary>
public enum IntervalUnit
{
    /// <summary>
    /// Seconds.
    /// </summary>
    Seconds = 0,

    /// <summary>
    /// Minutes.
    /// </summary>
    Minutes = 1,

    /// <summary>
    /// Hours.
    /// </summary>
    Hours = 2,

    /// <summary>
    /// Days.
    /// </summary>
    Days = 3
}

/// <summary>
/// Represents what causes a <see cref="ReactionDefinition"/> to run.
/// </summary>
/// <remarks>
/// A tree rather than an event name, because not every reaction is set off by an event. Flattening a schedule
/// to a name would make a reaction that runs every hour indistinguishable from one that responds to an event
/// called <c>Hour</c>.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(NamedTriggerSource), "named")]
[JsonDerivedType(typeof(IntervalTriggerSource), "interval")]
[JsonDerivedType(typeof(ScheduleTriggerSource), "schedule")]
public abstract record ReactionTriggerSource;

/// <summary>
/// Represents a <c>when &lt;Name&gt;</c> trigger - an event, a declared trigger, or one a consumer registered.
/// </summary>
/// <param name="Name">The name of the event or trigger.</param>
/// <remarks>
/// One kind for all of them on purpose, matching the language. A reaction says what it responds to; whether
/// that turns out to be a domain event, a message from an integration or a signal the host raises belongs to
/// the trigger rather than to the reaction.
/// </remarks>
public record NamedTriggerSource(string Name) : ReactionTriggerSource;

/// <summary>
/// Represents an <c>every &lt;n&gt; &lt;unit&gt;</c> trigger - a reaction that runs on an interval.
/// </summary>
/// <param name="Amount">How many <paramref name="Unit"/> pass between runs.</param>
/// <param name="Unit">The unit the amount is counted in.</param>
public record IntervalTriggerSource(int Amount, IntervalUnit Unit) : ReactionTriggerSource;

/// <summary>
/// Represents an <c>at &lt;time&gt;</c> trigger - a reaction that runs at a time of day, optionally narrowed to
/// a day of the week or a day of the month.
/// </summary>
/// <param name="Time">The time of day the reaction runs at.</param>
/// <param name="DayOfWeek">The day of the week it runs on, or <see langword="null"/> when it runs every day.</param>
/// <param name="DayOfMonth">The day of the month it runs on, or <see langword="null"/> when it runs every day.</param>
public record ScheduleTriggerSource(
    TimeOnly Time,
    DayOfWeek? DayOfWeek,
    int? DayOfMonth) : ReactionTriggerSource;

/// <summary>
/// Represents a command a reaction dispatches - the modeled <c>invokes</c> declaration.
/// </summary>
/// <param name="Command">The name of the command being invoked.</param>
/// <param name="Mappings">How each property of the command gets its value.</param>
/// <remarks>
/// Kept apart from <see cref="ProducedEvent"/> because a command is not produced, it is asked for. An event is
/// a fact the reaction appends; a command is an intent it hands to something else, which may still reject it.
/// </remarks>
public record InvokedCommand(string Command, IReadOnlyList<ProducedEventProperty> Mappings);

/// <summary>
/// Represents one trigger clause of a <see cref="ReactionDefinition"/> - what sets it off, and what it does
/// when set off.
/// </summary>
/// <param name="Source">What causes the reaction to run.</param>
/// <param name="Data">The names of the values of the occurrence the reaction uses.</param>
/// <param name="Description">The description of what the trigger does, or an empty string when none is declared.</param>
/// <param name="File">The relative path of the file holding the implementation, or an empty string when the
/// implementation is not in a file.</param>
/// <param name="Produces">The events the reaction appends.</param>
/// <param name="Invokes">The commands the reaction invokes.</param>
/// <remarks>
/// The path of an external implementation is carried, the code inside it is not - and neither is an inline
/// code block. The contract states the model, not its realization, which is the same line
/// <see cref="CommandDefinition"/> draws by carrying a <c>LogicDescription</c> and no body. The path is
/// carried so a trigger that is implemented elsewhere stays distinguishable from one that does nothing.
/// </remarks>
public record ReactionTrigger(
    ReactionTriggerSource Source,
    IReadOnlyList<string> Data,
    string Description,
    string File,
    IReadOnlyList<ProducedEvent> Produces,
    IReadOnlyList<InvokedCommand> Invokes);
