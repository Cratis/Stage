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

public class when_executing_validation_iterators : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_preserve_each_message_in_iterator_order(bool concept)
    {
        using var fixture = new a_real_worker();
        var body = concept ? "if (context.Value <= 0) { yield return \"first\"; yield return \"second\"; }" : "if (context.Artifact.Amount <= 0) { yield return \"first\"; yield return \"second\"; }";
        var path = await Model(fixture, body, concept);
        await using var worker = fixture.Create();
        var manifest = await worker.CompileAsync(path);
        var entry = Assert.Single(manifest.Entries);
        Assert.True(entry.Admitted, entry.DiagnosticMessage);
        var loaded = await SemanticModelLoader.LoadFromPathAsync(path);
        (await worker.ValidateAsync(Request(loaded, entry, 1, concept))).Outcome.ShouldEqual("satisfied");
        var rejected = await worker.ValidateAsync(Request(loaded, entry, 0, concept));
        rejected.Outcome.ShouldEqual("unsatisfied");
        Assert.Equal(["first", "second"], rejected.Messages.ToArray());
        // A different request kind cannot invoke an iterator as a predicate.
        (await worker.EvaluateAsync(Request(loaded, entry, 0, concept))).Outcome.ShouldEqual("faulted");
    }

    [Theory]
    [InlineData("for (var i = 0; i < 101; i++) yield return \"message\";")]
    [InlineData("yield return new string('x', 1025);")]
    [InlineData("yield return null!;")]
    [InlineData("yield return \"prefix\"; throw new System.InvalidOperationException(\"private detail\");")]
    async Task should_refuse_invalid_or_unbounded_messages(string body)
    {
        using var fixture = new a_real_worker();
        // Stress units intentionally bypass purity, as in the existing worker resource-limit specs.
        var unit = fixture.StressUnit(body, iterator: true);
        await using var worker = fixture.Create();
        var response = await worker.SendAsync(fixture.Request(unit) with { Kind = "validate" });
        var result = response.Evaluation!;
        result.Outcome.ShouldEqual("faulted");
        result.Messages.ShouldBeEmpty();
        Assert.StartsWith(BodyDiagnostics.WorkerFailed, result.Diagnostic, StringComparison.Ordinal);
    }

    [Fact]
    async Task should_kill_a_timed_out_iterator_and_allow_the_next_call()
    {
        using var fixture = new a_real_worker();
        var good = fixture.StressUnit("yield break;", iterator: true);
        var bad = fixture.StressUnit("System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite); yield break;", iterator: true);
        await using var worker = fixture.Create(timeout: 5000);
        (await worker.SendAsync(fixture.Request(good) with { Kind = "validate" })).Evaluation!.Outcome.ShouldEqual("satisfied");
        var response = await worker.SendAsync(fixture.Request(bad) with { Kind = "validate" });
        response.Evaluation!.Outcome.ShouldEqual("exceeded-time");
        Assert.StartsWith(BodyDiagnostics.ExceededTime, response.Evaluation.Diagnostic, StringComparison.Ordinal);
        worker.ProcessId.ShouldBeNull();
        (await worker.SendAsync(fixture.Request(good) with { Kind = "validate" })).Evaluation!.Outcome.ShouldEqual("satisfied");
    }

    static async Task<string> Model(a_real_worker fixture, string body, bool concept)
    {
        var path = Path.Combine(fixture.CacheDirectory, "Model.play");
        await File.WriteAllTextAsync(path, concept ? validation_body_model.IteratorConcept(body) : validation_body_model.IteratorCommand(body).Replace("OrderId generated identifier", "OrderId identifier", StringComparison.Ordinal));
        return path;
    }

    static BodyEvaluationRequest Request(LoadedSemanticModel loaded, BodyUnitEntry entry, decimal amount, bool concept)
    {
        var command = loaded.Plan.Commands.Values.Single();
        var values = command.Properties.Select(property => new SemanticPropertyValue(property.Id, property.Name switch
        {
            "amount" => SemanticValue.Number(amount),
            "recordedAt" => SemanticValue.Text(DateTimeOffset.UnixEpoch.ToString("O")),
            _ => SemanticValue.Text(Guid.Empty.ToString())
        }));
        var artifact = concept ? SemanticValue.Number(amount) : SemanticValue.Composite([.. values]);
        return new(entry.RequirementId, new Dictionary<string, SemanticValue>
        {
            ["Artifact"] = artifact, ["Value"] = artifact, ["Property"] = SemanticValue.Text(string.Empty)
        }.ToImmutableDictionary(pair => pair.Key, pair => SemanticValueWire.Write(pair.Value), StringComparer.Ordinal));
    }
}
#endif
