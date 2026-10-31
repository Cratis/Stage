// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRuntime;

public class when_evaluating_worker_rules : Specification
{
    [Theory]
    [InlineData("satisfied", true, false)]
    [InlineData("unsatisfied", false, true)]
    [InlineData("exceeded-time", false, false)]
    [InlineData("exceeded-memory", false, false)]
    [InlineData("faulted", false, false)]
    public async Task should_use_the_worker_answer_without_appending_a_refusal(string verdict, bool accepted, bool rejected)
    {
        var loaded = Compile();
        var plan = loaded.Plan;
        var body = loaded.ImplementationRequirements.Single();
        var worker = Substitute.For<IBodyWorkerClient, IAsyncDisposable>();
        worker.EvaluateAsync(Arg.Any<BodyEvaluationRequest>(), Arg.Any<CancellationToken>()).Returns(new BodyEvaluation(verdict));
        var manifest = new BodyUnitManifest("revision", [new(body.RequirementId, "RulePredicate", true) { BodySha256 = body.ContentHash }]);
        var execution = SemanticBodyExecution.From(loaded, manifest, () => worker);
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        appender.Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>()).Returns(Task.CompletedTask);
        using var runtime = (SemanticRuntime)SemanticRuntimeHosting.Create(plan, appender, SemanticWorld.Empty, execution);
        var result = await runtime.Execute(plan.Commands.Values.Single(), new Dictionary<string, JsonElement> { ["id"] = JsonSerializer.SerializeToElement("one"), ["amount"] = JsonSerializer.SerializeToElement(5) }, new ClaimsPrincipal(), new(DateTimeOffset.UnixEpoch, "owner", "Owner", "owner"), false);
        Assert.True((result is SemanticAccepted) == accepted, result.ToString());
        (result is SemanticRejected).ShouldEqual(rejected);
        if (rejected) ((SemanticRejected)result).Details.ShouldEqual("Amount refused");
        if (!accepted && !rejected) ((SemanticUnsupported)result).Details.Contains("STAGE-", StringComparison.Ordinal).ShouldBeTrue();
        await worker.Received(1).EvaluateAsync(Arg.Is<BodyEvaluationRequest>(request => request.RequirementId == body.RequirementId && request.Members["Property"].GetProperty("value").GetString() == "amount"), Arg.Any<CancellationToken>());
        if (accepted) await appender.Received(1).Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
        else await appender.DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
    }

    [Fact]
    public async Task should_leave_a_body_unsupported_without_a_provider()
    {
        var plan = Compile().Plan;
        var appender = Substitute.For<IAppendSemanticFacts>();
        using var runtime = (SemanticRuntime)SemanticRuntimeHosting.Create(plan, appender);
        var result = await runtime.Execute(plan.Commands.Values.Single(), new Dictionary<string, JsonElement> { ["amount"] = JsonSerializer.SerializeToElement(5) }, new ClaimsPrincipal(), new(DateTimeOffset.UnixEpoch, "owner", "Owner", "owner"), false);
        result.ShouldBeOfExactType<SemanticUnsupported>();
        await appender.DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
    }

    static LoadedSemanticModel Compile()
    {
        const string source = """
            module M
              feature F
                slice StateChange Add
                  command Add
                    id String identifier
                    amount Int
                    validate
                      amount rule Positive message "Amount refused"
                        ```csharp
                        return context.Value > 0;
                        ```
                    produces Added
                      for id
                      amount = amount
                  event Added
                    amount Int
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Rules"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("rules"), "rules", "Rules.play", source);
        var result = new SemanticModelCompiler().Compile("Rules", SemanticDocumentSet.Create([document], catalog));
        Assert.True(result.Success, string.Join(';', result.Diagnostics));
        var model = result.Value.Model;
        return new(model, SemanticExecutionPlan.Compile(model).Plan)
        {
            ImplementationRequirements = result.ImplementationRequirements,
            ImplementationContents = SemanticImplementationBodies.Resolve(result.Value.Documents, result.ImplementationRequirements)
        };
    }
}
