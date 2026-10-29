// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_executing_reducer_bodies : Specification
{
    const string Talk = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

    [Fact]
    async Task should_fold_state_and_delete_through_the_worker()
    {
        var directory = Directory.CreateTempSubdirectory("stage-reducer-bodies-").FullName;
        try
        {
            var path = Path.Combine(directory, "Model.play");
            await File.WriteAllTextAsync(path, Model);
            var loaded = await SemanticModelLoader.LoadFromPathAsync(path);
            var notes = new List<SemanticModelLoadDiagnostic>();
            await using var bodies = await SemanticBodyWorkerHosting.Start(
                loaded,
                path,
                new(true, Path.Combine(AppContext.BaseDirectory, "bodyworker", "Cratis.Stage.BodyWorker.dll"))
                {
                    CacheDirectory = Path.Combine(directory, "cache"), CallTimeoutMilliseconds = 30000
                },
                notes);
            Assert.True(bodies is not null, string.Join(';', notes.Select(note => note.Message)));
            Assert.All(bodies.Manifest.Entries, entry => Assert.True(entry.Admitted, entry.DiagnosticMessage));

            // A transition folds prior state, an optional member left unset reads as null, and a null return deletes the row.
            var (runtime, appender) = Runtime(loaded, bodies);
            using var lifetime = (IDisposable)runtime;
            Assert.IsType<SemanticAccepted>(await Execute(runtime, loaded, "ScoreTalk", ("score", 7)));
            Assert.IsType<SemanticAccepted>(await Execute(runtime, loaded, "ScoreTalk", ("score", 5)));
            var readModel = loaded.Plan.ReadModels.Values.Single();
            var row = Assert.Single(await runtime.ReadModels(readModel.Id)).Values;
            Value(row, readModel, "score").ShouldEqual(SemanticValue.Number(12));
            Value(row, readModel, "note").ShouldEqual(SemanticValue.Null);
            Assert.IsType<SemanticAccepted>(await Execute(runtime, loaded, "WithdrawTalk", ("reason", "withdrawn")));
            Assert.Empty(await runtime.ReadModels(readModel.Id));
            await appender.Received(3).Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    internal static (ISemanticRuntime Runtime, IAppendSemanticFacts Appender) Runtime(LoadedSemanticModel loaded, SemanticBodyExecution? bodies)
    {
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        var tail = ulong.MaxValue;
        ((ISemanticFactTail)appender).Tail().Returns(_ => tail);
        appender.Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>()).Returns(call =>
        {
            tail = tail == ulong.MaxValue ? (ulong)call.Arg<IReadOnlyList<SemanticFact>>().Count - 1 : tail + (ulong)call.Arg<IReadOnlyList<SemanticFact>>().Count;
            return Task.CompletedTask;
        });
        return (SemanticRuntimeHosting.Create(loaded.Plan, appender, SemanticWorld.Empty, bodies), appender);
    }

    internal static Task<SemanticExecutionResult> Execute(ISemanticRuntime runtime, LoadedSemanticModel loaded, string command, (string Name, object Value) value) => runtime.Execute(
        loaded.Plan.Commands.Values.Single(candidate => candidate.Name == command),
        new Dictionary<string, JsonElement>
        {
            ["talkId"] = JsonSerializer.SerializeToElement(Talk),
            [value.Name] = JsonSerializer.SerializeToElement(value.Value)
        },
        new ClaimsPrincipal(),
        new(DateTimeOffset.UnixEpoch, "owner", "Owner", "owner"),
        false);

    static SemanticValue Value(System.Collections.Immutable.ImmutableArray<SemanticPropertyValue> row, SemanticReadModel model, string name) =>
        row.Single(value => value.TargetProperty == model.Properties.Single(property => property.Name == name).Id).Value;

    internal const string Model = """
        concept TalkId : Uuid
        module CallForPapers
          feature Review
            slice StateChange Scoring
              command ScoreTalk
                talkId TalkId identifier
                score Int
                produces TalkScored
                  for talkId
                  score = score
              event TalkScored
                score Int
            slice StateChange Withdrawing
              command WithdrawTalk
                talkId TalkId identifier
                reason String
                produces TalkWithdrawn
                  for talkId
                  reason = reason
              event TalkWithdrawn
                reason String
            slice StateView TalkScores
              readmodel TalkScore
                talkId TalkId key
                score Int
                note String optional
              reducer TalkScoreReducer => TalkScore
                on TalkScored
                  ```csharp
                  return new TalkScore(TalkId: Guid.Parse(context.Key), Score: (context.State?.Score ?? 0) + context.Event.Score, Note: null);
                  ```
                on TalkWithdrawn
                  ```csharp
                  return null;
                  ```
              query ScoreOfTalk => TalkScore optional
                by talkId TalkId
              specification ScoreAccumulates
                given TalkScored
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  score = 7
                given TalkScored
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  score = 5
                then readmodel TalkScore
                  talkId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  score = 12
              specification WithdrawnTalkIsRemoved
                given TalkScored
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  score = 7
                given TalkWithdrawn
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  reason = "withdrawn"
                then no readmodel TalkScore for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        """;
}
