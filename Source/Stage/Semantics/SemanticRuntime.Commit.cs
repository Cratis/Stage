// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Runtime;

namespace Cratis.Stage.Semantics;

internal sealed partial class SemanticRuntime
{
    // Also used by the corpus specs to replay one prepared decision against several durable worlds.
    internal async Task<SemanticExecutionResult> Commit(Func<SemanticWorld, SemanticExecutionResult> decision, SemanticCommandOccurrence occurrence)
    {
        var decidedWorld = _world;
        var initial = decision(decidedWorld);

        if (_appender is not ISemanticFactTail tail)
        {
            // Refusals need no write; without a tail capability, keep the world they were decided against.
            if (initial is SemanticRejected { Category: SemanticRejectionCategory.Unauthorized or SemanticRejectionCategory.Validation })
            {
                return initial;
            }

            Fault("The fact appender cannot verify Chronicle's event-log tail.");
            return Faulted();
        }

        for (var attempt = 0; attempt < 8; attempt++)
        {
            if (!await Refresh(tail))
            {
                return Faulted();
            }

            var evaluated = ReferenceEquals(_world, decidedWorld) ? initial : decision(_world);
            if (evaluated is not SemanticAccepted accepted)
            {
                return evaluated;
            }

            var result = Settle(accepted, occurrence, out var invokedCommands);
            if (result is not SemanticAccepted settled)
            {
                return result;
            }

            var before = _knownTail;
            try
            {
                if (_appender is IAppendSemanticFactsAtTail guarded)
                {
                    // Empty successes validate the same durable pre-batch boundary as nonempty batches.
                    await guarded.Append(settled.Facts, occurrence, before).WaitAsync(_appendTimeout);
                }
                else if (settled.Facts.Length > 0)
                {
                    // Legacy appenders retain their preflight check but cannot close the append race.
                    await _appender.Append(settled.Facts, occurrence).WaitAsync(_appendTimeout);
                }
                else if (await tail.Tail().WaitAsync(_appendTimeout) != before)
                {
                    Fault("The event-log tail changed outside this session; the legacy appender cannot guard an empty batch.");
                    return Faulted();
                }
            }
            catch (SemanticFactTailChanged)
            {
                // This marker guarantees nothing was written. Never re-decide invoked commands (0017).
                if (invokedCommands || FactReader is null)
                {
                    return TailConflict();
                }

                continue;
            }
            catch (Exception exception)
            {
                return await AppendFailure(tail, before, exception);
            }

            _world = settled.World;
            _knownTail = before;
            if (settled.Facts.Length > 0)
            {
                _knownTail = before == ulong.MaxValue ? (ulong)settled.Facts.Length - 1 : before + (ulong)settled.Facts.Length;
            }

            PublishChange();

            return settled;
        }

        return TailConflict();
    }

    // A fault changes every answer the world gives, so live subscriptions must hear it and fail closed.
    void Fault(string reason)
    {
        FaultReason = reason;
        PublishChange();
    }

    SemanticConflict TailConflict() => new(_world, SemanticConflictCategory.DecisionStateChanged, "The event-log tail changed before the settled batch could be persisted.");

    async Task<bool> Refresh(ISemanticFactTail tail)
    {
        try
        {
            var through = await tail.Tail().WaitAsync(_appendTimeout);
            if (through == _knownTail)
            {
                return true;
            }

            if (FactReader is null || through == ulong.MaxValue || (_knownTail != ulong.MaxValue && through < _knownTail))
            {
                Fault($"The event-log tail changed outside this session ({_knownTail} -> {through}) and cannot be extended safely.");
                return false;
            }

            var from = _knownTail == ulong.MaxValue ? 0 : _knownTail + 1;
            var facts = await FactReader.Read(from, through).WaitAsync(_appendTimeout);
            if ((ulong)facts.Length != through - from + 1)
            {
                Fault("The outside event-log suffix could not be read completely.");
                return false;
            }

            var extended = _evaluator.ExtendWorld(Plan, _world, facts);
            if (extended is not SemanticAccepted accepted)
            {
                Fault($"The outside event-log suffix cannot extend the semantic world: {extended}");
                return false;
            }

            _world = accepted.World;
            _knownTail = through;
            PublishChange();

            return true;
        }
        catch (Exception exception)
        {
            Fault($"Cannot read the event-log tail or outside facts before append: {exception.Message}");
            return false;
        }
    }

    async Task<SemanticExecutionResult> AppendFailure(ISemanticFactTail tail, ulong before, Exception exception)
    {
        try
        {
            var after = await tail.Tail().WaitAsync(_appendTimeout);
            if (after == before && exception is ProducedEventConstraintRejected)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
            }

            Fault($"Append outcome is unknown (tail {before} -> {after}): {exception.Message}");
        }
        catch (Exception tailFailure) when (!ReferenceEquals(tailFailure, exception))
        {
            Fault($"Append outcome is unknown; the event-log tail cannot be read: {tailFailure.Message}");
        }

        return Faulted();
    }
}
