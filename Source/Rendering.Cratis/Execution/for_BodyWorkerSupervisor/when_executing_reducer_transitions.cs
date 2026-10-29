// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.Execution.for_BodyWorkerSupervisor.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_BodyWorkerSupervisor;

public class when_executing_reducer_transitions : Specification
{
    const string Talk = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

    [Fact]
    async Task should_fold_the_analysed_transition_into_the_next_state()
    {
        using var fixture = new a_real_worker();
        var path = Path.Combine(fixture.CacheDirectory, "Model.play");
        await File.WriteAllTextAsync(path, a_reducer_fact_specification.Source.Replace(
            a_reducer_fact_specification.Body,
            "if (context.Event.Score < 0) return null!;\n          return new TalkScore(TalkId: Guid.Parse(context.Key), Score: (context.State?.Score ?? 0) + context.Event.Score);",
            StringComparison.Ordinal));
        await using var worker = fixture.Create();
        var entry = Assert.Single((await worker.CompileAsync(path)).Entries);
        Assert.True(entry.Admitted, entry.DiagnosticMessage);
        var loaded = await SemanticModelLoader.LoadFromPathAsync(path);
        var model = loaded.Plan.ReadModels.Values.Single();
        var score = model.Properties.Single(property => property.Name == "score").Id;
        var prior = SemanticValue.Composite([new(model.Properties.Single(property => property.Name == "talkId").Id, SemanticValue.Text(Talk)), new(score, SemanticValue.Number(7))]);
        var result = await worker.ReduceAsync(Request(loaded, entry, prior));
        result.Outcome.ShouldEqual("state");
        var state = Assert.IsType<SemanticCompositeValue>(SemanticValueWire.Read(result.Value!.Value));
        state.Properties.Single(value => value.TargetProperty == score).Value.ShouldEqual(SemanticValue.Number(12));
        var initial = await worker.ReduceAsync(Request(loaded, entry, SemanticValue.Null));
        initial.Outcome.ShouldEqual("state");
        var initialState = Assert.IsType<SemanticCompositeValue>(SemanticValueWire.Read(initial.Value!.Value));
        initialState.Properties.Single(value => value.TargetProperty == score).Value.ShouldEqual(SemanticValue.Number(5));
        var deleted = await worker.ReduceAsync(Request(loaded, entry, prior, score: -1));
        deleted.Outcome.ShouldEqual("delete");
        deleted.Value.ShouldBeNull();

        // An evaluation request cannot invoke a reducer transition.
        (await worker.EvaluateAsync(Request(loaded, entry, prior))).Outcome.ShouldEqual("faulted");
    }

    [Fact]
    async Task should_delete_when_the_transition_returns_null_and_kill_a_timed_out_transition()
    {
        using var fixture = new a_real_worker();

        // Stress units intentionally bypass purity, as in the existing worker resource-limit specs.
        var deleting = fixture.StressUnit("return null;", reducer: true);
        var hanging = fixture.StressUnit("System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite); return null;", reducer: true);
        await using var worker = fixture.Create(timeout: 5000);
        (await worker.SendAsync(fixture.Request(deleting) with { Kind = "reduce" })).Evaluation!.Outcome.ShouldEqual("delete");
        var response = await worker.SendAsync(fixture.Request(hanging) with { Kind = "reduce" });
        response.Evaluation!.Outcome.ShouldEqual("exceeded-time");
        Assert.StartsWith(BodyDiagnostics.ExceededTime, response.Evaluation.Diagnostic, StringComparison.Ordinal);
        worker.ProcessId.ShouldBeNull();
        (await worker.SendAsync(fixture.Request(deleting) with { Kind = "reduce" })).Evaluation!.Outcome.ShouldEqual("delete");
    }

    static BodyEvaluationRequest Request(LoadedSemanticModel loaded, BodyUnitEntry entry, SemanticValue state, decimal score = 5)
    {
        var @event = loaded.Plan.Events.Values.Single();
        return new(entry.RequirementId, new Dictionary<string, SemanticValue>
        {
            ["State"] = state,
            ["Event"] = SemanticValue.Composite([new(@event.Properties.Single().Id, SemanticValue.Number(score))]),
            ["Key"] = SemanticValue.Text(Talk),
            ["Tenant"] = SemanticValue.Text(string.Empty),
            ["Occurred"] = SemanticValue.Text(DateTimeOffset.UnixEpoch.ToString("O")),
            ["SequenceNumber"] = SemanticValue.Number(1)
        }.ToImmutableDictionary(pair => pair.Key, pair => SemanticValueWire.Write(pair.Value), StringComparer.Ordinal));
    }
}
#endif
