// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Specifications;

// Stage's independent occurrence enumeration, matching Screenplay 4.206.0 SemanticClock.
internal static class SemanticRunClock
{
    internal const int MaximumOccurrences = 10_000;

    internal static bool TryFindDue(
        IEnumerable<SemanticReaction> reactions,
        DateTimeOffset from,
        DateTimeOffset to,
        out List<(DateTimeOffset At, SemanticReaction Reaction, SemanticReactionTrigger Trigger)> occurrences)
    {
        var due = new List<(DateTimeOffset At, int Order, SemanticReaction Reaction, SemanticReactionTrigger Trigger)>();
        var order = 0;
        foreach (var reaction in reactions)
        {
            foreach (var trigger in reaction.Triggers)
            {
                var position = order++;
                foreach (var at in Due(trigger, from, to))
                {
                    if (due.Count >= MaximumOccurrences)
                    {
                        occurrences = [];
                        return false;
                    }
                    due.Add((at, position, reaction, trigger));
                }
            }
        }
        occurrences = [.. due.OrderBy(value => value.At).ThenBy(value => value.Order).Select(value => (value.At, value.Reaction, value.Trigger))];

        return true;
    }

    static IEnumerable<DateTimeOffset> Due(SemanticReactionTrigger trigger, DateTimeOffset from, DateTimeOffset to)
    {
        if (trigger is { Kind: SemanticReactionTriggerKind.Interval, Every: { } every })
        {
            var fromSeconds = from.ToUnixTimeSeconds();
            var quotient = fromSeconds / every;
            if (fromSeconds % every < 0) quotient--;
            var start = (quotient + 1) * every;
            for (var seconds = start; seconds <= to.ToUnixTimeSeconds(); seconds += every)
            {
                var at = DateTimeOffset.FromUnixTimeSeconds(seconds);
                if (at > from && at <= to) yield return at;
            }
            yield break;
        }
        if (trigger is not { Kind: SemanticReactionTriggerKind.Schedule, At: { } secondOfDay }) yield break;
        for (var day = from.UtcDateTime.Date; day <= to.UtcDateTime.Date;)
        {
            var at = new DateTimeOffset(day.AddSeconds(secondOfDay), TimeSpan.Zero);
            if (at > from && at <= to &&
                (trigger.OnDayOfWeek is null || (int)day.DayOfWeek == trigger.OnDayOfWeek) &&
                (trigger.OnDayOfMonth is null || day.Day == trigger.OnDayOfMonth))
            {
                yield return at;
            }
            if (day == to.UtcDateTime.Date) yield break;
            day = day.AddDays(1);
        }
    }
}
