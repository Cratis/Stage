// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using NSubstitute;

namespace Cratis.Stage.Host.for_SemanticRuntime.given;

public class a_reaction_runtime : Specification
{
    protected ISemanticRuntime _runtime = null!;
    protected SemanticExecutionPlan _plan = null!;
    protected IAppendSemanticFactsAtTail _appender = null!;
    protected readonly List<IReadOnlyList<SemanticFact>> _batches = [];
    protected readonly List<ulong> _expectedTails = [];
    protected SemanticExecutionResult _result = null!;
    protected virtual string Model => StartSource + ProducingSource;

    protected const string StartSource = """
        module Work
          feature Completion
            slice StateChange StartWork
              command StartWork
                id String identifier
                amount Decimal
                produces Started
                  for id
                  id = id
                  amount = amount
              event Started
                id String
                amount Decimal
        """;

    protected const string ProducingSource = """

            slice Automation FollowUp
              reaction CompleteWork
                when Started
                  id
                  amount
                  produces Completed
                    for id
                    id = id
                    amount = amount
              event Completed
                id String
                amount Decimal
        """;

    protected const string InvokingSource = """

            slice Automation FollowUp
              command CompleteWork
                id String identifier
                amount Decimal
                produces Completed
                  for id
                  id = id
                  amount = amount
              reaction Worker
                when Started
                  id
                  amount
                  invokes CompleteWork
                    id = id
                    amount = amount
              event Completed
                id String
                amount Decimal
        """;

    void Establish()
    {
        _plan = Compile();
        var appender = Substitute.For<IAppendSemanticFacts, IAppendSemanticFactsAtTail, ISemanticFactTail>();
        var tail = ulong.MaxValue;
        ((ISemanticFactTail)appender).Tail().Returns(_ => tail);
        ((IAppendSemanticFactsAtTail)appender).Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>(), Arg.Any<ulong>()).Returns(call =>
        {
            var facts = call.Arg<IReadOnlyList<SemanticFact>>();
            _batches.Add(facts);
            _expectedTails.Add(call.Arg<ulong>());
            tail = tail == ulong.MaxValue ? (ulong)facts.Count - 1 : tail + (ulong)facts.Count;
            return Task.CompletedTask;
        });
        _appender = (IAppendSemanticFactsAtTail)appender;
        _runtime = SemanticRuntimeHosting.Create(_plan, appender);
    }

    protected virtual SemanticExecutionPlan Compile() => compiled_plan.From(Model);

    protected Task<SemanticExecutionResult> Execute(bool validateOnly = false, ClaimsPrincipal? principal = null) => _runtime.Execute(
        _plan.Commands.Values.Single(command => command.Name == "StartWork"),
        new Dictionary<string, JsonElement>
        {
            ["id"] = JsonSerializer.SerializeToElement("one"),
            ["amount"] = JsonSerializer.SerializeToElement(7)
        },
        principal ?? new ClaimsPrincipal(),
        new(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero), "subject", "name", "user"),
        validateOnly);

    protected IEnumerable<string> FactNames(IEnumerable<SemanticFact> facts) => facts.Select(fact => _plan.Events[fact.EventContract].Name);

    void Destroy() => (_runtime as IDisposable)?.Dispose();
}
