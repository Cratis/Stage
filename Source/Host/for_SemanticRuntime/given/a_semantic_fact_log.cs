// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Host.for_SemanticRuntime.given;

internal sealed class a_semantic_fact_log : IAppendSemanticFacts, IAppendSemanticFactsAtTail, ISemanticFactTail, ISemanticFactReader
{
    internal readonly List<SemanticFact> Facts = [];
    internal readonly List<(SemanticFact[] Facts, ulong Tail)> Attempts = [];
    internal Action? BeforeAppend { get; set; }
    internal Func<Task>? PauseBeforeAppend { get; set; }
    internal int Writes { get; private set; }
    internal ulong CurrentTail => Facts.Count == 0 ? ulong.MaxValue : (ulong)Facts.Count - 1;

    internal SemanticRuntime Runtime(SemanticExecutionPlan plan, SemanticWorld world) => new(plan, this, world) { FactReader = this };

    public Task<ulong> Tail() => Task.FromResult(CurrentTail);

    public Task<ImmutableArray<SemanticFact>> Read(ulong from, ulong through) =>
        Task.FromResult(Facts.Skip((int)from).Take((int)(through - from + 1)).ToImmutableArray());

    public Task Append(IReadOnlyList<SemanticFact> facts, SemanticCommandOccurrence occurrence) => Append(facts, occurrence, CurrentTail);

    public async Task Append(IReadOnlyList<SemanticFact> facts, SemanticCommandOccurrence occurrence, ulong expectedTail)
    {
        Attempts.Add(([.. facts], expectedTail));
        BeforeAppend?.Invoke();
        if (PauseBeforeAppend is { } pause)
        {
            await pause();
        }

        if (expectedTail != CurrentTail)
        {
            throw new SemanticFactTailChanged(CurrentTail);
        }

        Facts.AddRange(facts);
        Writes++;
    }
}
