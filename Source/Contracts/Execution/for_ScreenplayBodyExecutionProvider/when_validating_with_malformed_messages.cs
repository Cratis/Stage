// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_ScreenplayBodyExecutionProvider;

public class when_validating_with_malformed_messages : Specification
{
    [Fact]
    void should_normalize_a_cleared_message_collection_to_empty()
    {
        var evaluation = new BodyEvaluation("satisfied", Messages: ["kept"]) with { Messages = default };
        evaluation.Messages.IsDefault.ShouldBeFalse();
        evaluation.Messages.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    void should_normalize_a_missing_wire_message_collection_to_empty()
    {
        var evaluation = JsonSerializer.Deserialize<BodyEvaluation>("""{"outcome":"satisfied"}""", BodyWorkerMessages.Json);
        evaluation.Messages.IsDefault.ShouldBeFalse();
        evaluation.Messages.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    async Task should_accept_a_satisfied_verdict_whose_messages_were_cleared()
    {
        var result = await Validate(new BodyEvaluation("satisfied") with { Messages = default });
        result.Diagnostic.ShouldBeNull();
        result.Messages!.Value.IsEmpty.ShouldBeTrue();
    }

    [Theory]
    [InlineData("none")]
    [InlineData("null-message")]
    [InlineData("too-long")]
    [InlineData("too-many")]
    async Task should_refuse_a_rejection_without_usable_messages(string shape)
    {
        ImmutableArray<string> messages = shape switch
        {
            "null-message" => ["first", null!],
            "too-long" => [new string('x', 1025)],
            "too-many" => [.. Enumerable.Repeat("message", 101)],
            _ => default
        };
        var result = await Validate(new BodyEvaluation("unsatisfied") with { Messages = messages });
        result.Diagnostic!.StartsWith(BodyDiagnostics.WorkerFailed, StringComparison.Ordinal).ShouldBeTrue();
        result.Messages.ShouldBeNull();
    }

    static async Task<SemanticValidationBodyResult> Validate(BodyEvaluation verdict)
    {
        var worker = Substitute.For<IBodyWorkerClient>();
        worker.ValidateAsync(Arg.Any<BodyEvaluationRequest>(), Arg.Any<CancellationToken>()).Returns(verdict);
        await using var provider = new ScreenplayBodyExecutionProvider(new("revision", [new("rule", "CommandValidation", true) { BodySha256 = Hash() }]), worker);
        return await provider.ValidateAsync(Body(), Context(), CancellationToken.None);
    }

    static SemanticImplementationBody Body() => new(
        new(SemanticImplementationRole.CommandValidation, default!, null, "csharp", null, Hash(), null!) { RequirementId = "rule" }, "yield break;");

    static string Hash() => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("yield break;"))).ToLowerInvariant();

    static SemanticRuleContext Context() => new(SemanticValue.Text("artifact"), SemanticValue.Number(1), "amount", "not-forwarded", null, DateTimeOffset.UnixEpoch);
}
