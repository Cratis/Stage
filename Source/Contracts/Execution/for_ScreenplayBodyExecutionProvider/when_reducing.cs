// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_ScreenplayBodyExecutionProvider;

public class when_reducing : Specification
{
    const string Source = "return null;";

    [Fact]
    async Task should_return_the_worker_state_and_send_the_typed_context_members()
    {
        var state = SemanticValue.Composite([new(SemanticId.Create(SemanticKind.Property, "score"), SemanticValue.Number(7))]);
        BodyEvaluationRequest? sent = null;
        var (result, calls) = await Reduce(new BodyEvaluation("state", SemanticValueWire.Write(state)), Context(SemanticValue.Text("key")), request => sent = request);
        result.Outcome.ShouldEqual(SemanticReducerBodyOutcome.State);
        Wire(result.State).ShouldEqual(Wire(state));
        calls.ShouldEqual(1);
        Assert.Equal(["Event", "Key", "Occurred", "SequenceNumber", "State", "Tenant"], [.. sent.Members.Keys.Order(StringComparer.Ordinal)]);
        sent.Members["Key"].GetRawText().ShouldEqual(Wire(SemanticValue.Text("key")));
        sent.Members["State"].GetRawText().ShouldEqual(Wire(SemanticValue.Null));
    }

    [Fact]
    async Task should_delete_when_the_worker_deletes()
    {
        var (result, _) = await Reduce(new BodyEvaluation("delete"), Context(SemanticValue.Number(42)));
        result.Outcome.ShouldEqual(SemanticReducerBodyOutcome.Delete);
    }

    [Fact]
    async Task should_refuse_a_non_scalar_key_without_calling_the_worker()
    {
        var (result, calls) = await Reduce(new BodyEvaluation("delete"), Context(SemanticValue.Composite([])));
        result.Outcome.ShouldEqual(SemanticReducerBodyOutcome.Unsupported);
        calls.ShouldEqual(0);
    }

    [Theory]
    [InlineData("exceeded-time")]
    [InlineData("exceeded-memory")]
    [InlineData("faulted")]
    [InlineData("refused")]
    [InlineData("malformed")]
    async Task should_be_unsupported_when_the_worker_cannot_produce_state(string outcome)
    {
        var verdict = outcome == "malformed"
            ? new BodyEvaluation("state", JsonSerializer.SerializeToElement(new { kind = "unknown" }))
            : new BodyEvaluation(outcome, Diagnostic: BodyDiagnostics.Format(BodyDiagnostics.WorkerFailed, "failed"));
        var (result, _) = await Reduce(verdict, Context(SemanticValue.Text("key")));
        result.Outcome.ShouldEqual(SemanticReducerBodyOutcome.Unsupported);
        result.State.ShouldBeNull();
        Assert.NotNull(result.Diagnostic);
    }

    [Fact]
    async Task should_refuse_a_body_that_changed_since_compile()
    {
        var worker = Substitute.For<IBodyWorkerClient>();
        await using var provider = new ScreenplayBodyExecutionProvider(new("revision", [new("reducer", "ReducerTransition", true) { BodySha256 = "changed" }]), worker);
        var result = await provider.ReduceAsync(Body(), Context(SemanticValue.Text("key")), CancellationToken.None);
        result.Outcome.ShouldEqual(SemanticReducerBodyOutcome.Unsupported);
        await worker.DidNotReceive().ReduceAsync(Arg.Any<BodyEvaluationRequest>(), Arg.Any<CancellationToken>());
    }

    static async Task<(SemanticReducerBodyResult Result, int Calls)> Reduce(BodyEvaluation verdict, SemanticReducerContext context, Action<BodyEvaluationRequest>? sent = null)
    {
        var worker = Substitute.For<IBodyWorkerClient>();
        worker.ReduceAsync(Arg.Do<BodyEvaluationRequest>(request => sent?.Invoke(request)), Arg.Any<CancellationToken>()).Returns(verdict);
        await using var provider = new ScreenplayBodyExecutionProvider(new("revision", [new("reducer", "ReducerTransition", true) { BodySha256 = Hash() }]), worker);
        var result = await provider.ReduceAsync(Body(), context, CancellationToken.None);
        return (result, worker.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IBodyWorkerClient.ReduceAsync)));
    }

    static string Wire(SemanticValue value) => SemanticValueWire.Write(value).GetRawText();

    static SemanticImplementationBody Body() => new(
        new(SemanticImplementationRole.ReducerTransition, default!, null, "csharp", null, Hash(), null!) { RequirementId = "reducer" }, Source);

    static string Hash() => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Source))).ToLowerInvariant();

    static SemanticReducerContext Context(SemanticValue key) => new()
    {
        Event = SemanticValue.Composite([]),
        Key = key,
        Occurred = DateTimeOffset.UnixEpoch,
        SequenceNumber = 3
    };
}
