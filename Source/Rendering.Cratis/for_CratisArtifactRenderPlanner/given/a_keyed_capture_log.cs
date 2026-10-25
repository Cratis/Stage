// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Execution;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

/// <summary>
/// Models the destination's durable keyed append boundary, independently of each reactor instance.
/// </summary>
internal sealed class a_keyed_capture_log
{
    readonly Dictionary<IdempotencyKey, EventForEventSourceId> _committed = [];

    internal a_keyed_capture_log()
    {
        Log = DispatchProxy.Create<IEventLog, LogProxy>();
        ((LogProxy)(object)Log).Owner = this;
    }

    internal IEventLog Log { get; }
    internal bool SupportsIdempotentAppends { get; set; } = true;
    internal List<EventForEventSourceId[]> Attempts { get; } = [];
    internal IEnumerable<EventForEventSourceId> Committed => _committed.Values;
    internal bool LoseNextAcknowledgment { get; set; }
    internal AppendManyResult? Refusal { get; set; }

    Task<AppendManyResult> Append(IEnumerable<EventForEventSourceId> events)
    {
        lock (_committed)
        {
            var batch = events.ToArray();
            Attempts.Add(batch);
            if (Refusal is { } refusal) return Task.FromResult(refusal);
            var outcomes = batch.Select(Commit).ToArray();
            if (LoseNextAcknowledgment)
            {
                LoseNextAcknowledgment = false;
                return Task.FromException<AppendManyResult>(new LostAcknowledgment());
            }
            return Task.FromResult(AppendManyResult.Success(CorrelationId.New(), [EventSequenceNumber.First]) with { Outcomes = outcomes });
        }
    }

    AppendOutcome Commit(EventForEventSourceId effect)
    {
        if (effect.IdempotencyKey is not { } key || string.IsNullOrWhiteSpace(key.Value)) throw new InvalidOperationException("A capture effect must have a durable key.");
        if (_committed.TryGetValue(key, out var previous))
        {
            if (previous.EventSourceId != effect.EventSourceId || !Equals(previous.Event, effect.Event)) throw new InvalidOperationException("An effect key cannot identify a different event.");
            return AppendOutcome.AlreadyAppended;
        }
        _committed.Add(key, effect);
        return AppendOutcome.Appended;
    }

    internal sealed class LostAcknowledgment : Exception;

    // Castle's inherited default-interface-property interception fails on SupportsIdempotentAppends.
    // Dispatch only the two operations this destination boundary supports; fail loudly for anything else.
    public class LogProxy : DispatchProxy
    {
        internal a_keyed_capture_log Owner { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_SupportsIdempotentAppends" => Owner.SupportsIdempotentAppends,
            nameof(IEventSequence.AppendMany) when args?[0] is IEnumerable<EventForEventSourceId> events => Owner.Append(events),
            _ => throw new NotSupportedException(targetMethod?.Name)
        };
    }
}
#endif
