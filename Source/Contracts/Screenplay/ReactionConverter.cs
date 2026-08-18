// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Reactions;
using ScreenplayIntervalUnit = Cratis.Screenplay.Syntax.IntervalUnit;
using StageIntervalUnit = Cratis.Stage.Contracts.Reactions.IntervalUnit;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Converts the Screenplay <c>reaction</c> declarations of a slice into Stage <see cref="ReactionDefinition"/>
/// records.
/// </summary>
/// <remarks>
/// The trigger keeps its shape rather than collapsing to an event name. A reaction that runs every hour and one
/// that responds to an event are different things, and a name is all they would have in common.
/// </remarks>
public static class ReactionConverter
{
    /// <summary>
    /// Converts a slice's reaction declarations into their Stage records.
    /// </summary>
    /// <param name="reactions">The reaction declarations.</param>
    /// <param name="slicePath">The fully-qualified slice path, used to derive stable identifiers.</param>
    /// <returns>The Stage reaction definitions, in declaration order.</returns>
    public static IReadOnlyList<ReactionDefinition> Convert(IEnumerable<ReactionSyntax> reactions, string slicePath) =>
    [
        .. reactions.Select(reaction => new ReactionDefinition(
            DeterministicId.From($"{slicePath}.reaction.{reaction.Name}"),
            reaction.Name,
            reaction.Description ?? string.Empty,
            ConditionConverter.Convert(reaction.Where),
            [.. reaction.Triggers.Select(Trigger)]))
    ];

    static ReactionTrigger Trigger(ReactionTriggerSyntax trigger) =>
        new(
            Source(trigger.Source),
            [.. trigger.Data.Select(data => data.Name)],
            trigger.Description ?? string.Empty,
            trigger.File?.Path ?? string.Empty,
            ProducesConverter.Convert(trigger.Produces ?? []),
            [.. (trigger.Invokes ?? []).Select(Invokes)]);

    static ReactionTriggerSource Source(TriggerSourceSyntax source) =>
        source switch
        {
            NamedTriggerSourceSyntax named => new NamedTriggerSource(named.Name),
            IntervalTriggerSourceSyntax interval => new IntervalTriggerSource(interval.Amount, Unit(interval.Unit)),
            ScheduleTriggerSourceSyntax schedule => new ScheduleTriggerSource(schedule.Time, schedule.DayOfWeek, schedule.DayOfMonth),
            _ => new NamedTriggerSource(string.Empty)
        };

    static InvokedCommand Invokes(InvokesSyntax invokes) =>
        new(invokes.Command, [.. invokes.Mappings.Select(ProducedValueConverter.Property)]);

    static StageIntervalUnit Unit(ScreenplayIntervalUnit unit) =>
        unit switch
        {
            ScreenplayIntervalUnit.Seconds => StageIntervalUnit.Seconds,
            ScreenplayIntervalUnit.Minutes => StageIntervalUnit.Minutes,
            ScreenplayIntervalUnit.Hours => StageIntervalUnit.Hours,
            ScreenplayIntervalUnit.Days => StageIntervalUnit.Days,
            _ => StageIntervalUnit.Seconds
        };
}
