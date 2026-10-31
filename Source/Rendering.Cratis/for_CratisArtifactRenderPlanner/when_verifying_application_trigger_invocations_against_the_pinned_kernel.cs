// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Xml.Linq;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticAutomationArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_verifying_application_trigger_invocations_against_the_pinned_kernel : a_generated_application
{
    const string InnerTest = "Reactions.KernelProbe.ApplicationTriggerInvocationProbe.should_resume_commands_and_retain_refusals_per_namespace";
    protected override bool DeferPlan => true;

#pragma warning disable CRSPEC0004 // MongoFact derives from FactAttribute and is discovered by xUnit.
    [MongoFact]
    public async Task should_resume_commands_and_retain_refusals_when_configured()
#pragma warning restore CRSPEC0004
    {
        try
        {
            InstallProbe();
            var build = await Run("stage343-invocations-build.log", "build", "Reactions.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var output = await Run("stage343-invocations-probe.log", "test", "Reactions.csproj", "-c", "Debug", "--no-build", "--no-restore", "--filter", $"FullyQualifiedName={InnerTest}", "--logger", "trx;LogFileName=stage343-invocations.trx", "--results-directory", "stage343-results", "--nologo");
            Assert.Contains("Passed!", output, StringComparison.Ordinal);
            var path = Assert.Single(GeneratedFiles("stage343-invocations.trx"));
            var trx = ReadGeneratedFile(path);
            RequireExecutedProbe(trx);
            var root = Environment.GetEnvironmentVariable("AI_WORK_KEEP");
            Assert.False(string.IsNullOrWhiteSpace(root), "AI_WORK_KEEP is required to retain the inner runtime proof.");
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(Path.Combine(root, "stage343-invocations-inner.trx"), trx);
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public void should_compile_the_probe_without_running_the_kernel()
    {
        var plan = PlanProbe();
        RenderedFile[] files = [.. Semantics.for_SemanticOccurrenceHandlerArtifactRenderer.when_rendering_application_trigger_productions.Files(plan),
            new("Stage343Host.cs", "namespace Reactions; public static class Stage343Host { public static Microsoft.AspNetCore.Builder.WebApplication Create(string connection, string storeName) => throw new System.NotSupportedException(); }"), new("Stage343InvocationProbe.cs", Probe)];
        var errors = for_CratisRenderer.RenderedOutput.Errors(files);
        Assert.True(errors.Count == 0, string.Join('\n', errors));
    }

    static global::Cratis.Screenplay.Semantics.ExecutableSemanticModel ProbeModel()
    {
        var source = Semantics.for_SemanticOccurrenceAdmission.when_admitting_application_trigger_reactions.with_productions_and_invocations.MixedSource
            .Replace("      command FinishBatch", "      command FinishBatch\n        authorize CanFinish", StringComparison.Ordinal)
            .Replace("        produces event BatchFinished", "        validate\n          batchId != \"refused\" message \"Batch refused\"\n        produces event BatchFinished", StringComparison.Ordinal)
            .Replace("      reaction ProcessBatch", "      reaction ProcessBatch\n        runs as system role \"Worker\"", StringComparison.Ordinal);
        return an_automation.Compile("policy CanFinish\n  require role \"Worker\"\n" + source);
    }

    static ArtifactRenderPlan PlanProbe()
    {
        var model = ProbeModel();
        return CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Reactions", "Reactions"));
    }

    void InstallProbe()
    {
        var model = ProbeModel();
        var reaction = model.Application.Modules.Single().Features.Single().Slices.Single().Reactions.Single();
        var command = model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        var plan = PlanProbe();
        Assert.True(plan.Success, string.Join(';', plan.Diagnostics));
        RenderApplication(plan);
        AddGeneratedHostSupport("Stage343Host.cs", ApplicationTriggerProbeSource.Host);
        AddGeneratedSpecification("Stage343InvocationProbe.cs", Probe
            .Replace("__REACTION_ID__", reaction.Id.ToString(), StringComparison.Ordinal)
            .Replace("__COMMAND_ID__", command.Id.ToString(), StringComparison.Ordinal)
            .Replace("__TRIGGER_ID__", model.Application.Triggers.Single().Id.ToString(), StringComparison.Ordinal));
    }

    static void RequireExecutedProbe(string trx)
    {
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        var document = XDocument.Parse(trx);
        var result = Assert.Single(document.Descendants(ns + "UnitTestResult"));
        Assert.Equal(InnerTest, (string?)result.Attribute("testName"));
        Assert.Equal("Passed", (string?)result.Attribute("outcome"));
        var counters = Assert.Single(document.Descendants(ns + "Counters"));
        Assert.Equal("1", (string?)counters.Attribute("executed"));
        Assert.Equal("0", (string?)counters.Attribute("notExecuted"));
    }

    const string Probe = """
        using Cratis.Arc.Chronicle.Occurrences;
        using Cratis.Chronicle;
        using Cratis.Chronicle.EventSequences;
        using Cratis.Chronicle.Occurrences;
        using Cratis.Chronicle.Registrations;
        using Microsoft.Extensions.DependencyInjection;
        using Reactions.Work.Processing.Batches;
        using Xunit;
        namespace Reactions.KernelProbe;
        public class ApplicationTriggerInvocationProbe
        {
            const string Production = "stage:reaction:__REACTION_ID__:productions";
            const string First = "stage:reaction:__REACTION_ID__:invocation:0:__COMMAND_ID__";
            const string Second = "stage:reaction:__REACTION_ID__:invocation:1:__COMMAND_ID__";

            [Fact]
            public async Task should_resume_commands_and_retain_refusals_per_namespace()
            {
                var connection = Environment.GetEnvironmentVariable("STAGE_CHRONICLE_MONGO_CONNECTION");
                Assert.False(string.IsNullOrWhiteSpace(connection));
                await using var app = Stage343Host.Create(connection!, $"s343i-{Guid.NewGuid():N}");
                try
                {
                    await app.StartAsync().WaitAsync(TimeSpan.FromSeconds(90));
                    var client = app.Services.GetRequiredService<IChronicleClient>();
                    var injected = app.Services.GetRequiredService<IEventStore>();
                    var executor = app.Services.GetRequiredService<IOccurrenceCommandExecutor>();
                    var handler = ActivatorUtilities.CreateInstance<ProcessBatch>(app.Services);
                    var one = await client.GetEventStore(injected.Name, "recording-one");
                    var two = await client.GetEventStore(injected.Name, "recording-two");
                    foreach (var store in new[] { one, two })
                    {
                        var registration = await store.WaitForRegistration(TimeSpan.FromSeconds(90));
                        Assert.True(registration.IsSuccess, $"Registration failed: {registration.Failure}; {string.Join("; ", registration.Failures)}");
                    }
                    await VerifySuccess(one, handler, executor);
                    Assert.Empty(await two.EventLog.GetFromSequenceNumber(EventSequenceNumber.First));
                    await VerifySuccess(two, handler, executor);
                    Assert.Equal(3, (await one.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count());
                    await VerifyRefusal(one, handler, executor);
                    Assert.Equal(3, (await two.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count());
                    Assert.Empty(await injected.EventLog.GetFromSequenceNumber(EventSequenceNumber.First));
                }
                finally
                {
                    await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(30));
                }
            }

            static async Task VerifySuccess(IEventStore store, ProcessBatch handler, IOccurrenceCommandExecutor executor)
            {
                var payload = new BatchRequested("batch-42");
                var context = await Record(store, payload, "success", Second);
                var production = await Receipt(store, context, Production);
                var first = await Receipt(store, context, First);
                var second = await Receipt(store, context, Second);
                Assert.IsType<Appended>(production.Outcome);
                Assert.IsType<Appended>(first.Outcome);
                Assert.IsType<Appended>(second.Outcome);
                Assert.Equal("batch-42,second", string.Join(',', (await store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Where(fact => fact.Content is BatchFinished).Select(fact => ((BatchFinished)fact.Content).BatchId)));
                var settled = await executor.Execute(new FinishBatch("batch-42"), context, First, ["Worker"], CancellationToken.None);
                Assert.Equal(OccurrenceCommandDisposition.AlreadyRecorded, settled.Disposition);
                await handler.Handle(payload, context, CancellationToken.None);
                Assert.Equal(3, (await store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count());
                Assert.Equal(first.RecordedAt, (await Receipt(store, context, First)).RecordedAt);
                Assert.Equal(second.RecordedAt, (await Receipt(store, context, Second)).RecordedAt);
                Assert.Equal(production.RecordedAt, (await Receipt(store, context, Production)).RecordedAt);
            }

            static async Task VerifyRefusal(IEventStore store, ProcessBatch handler, IOccurrenceCommandExecutor executor)
            {
                var payload = new BatchRequested("refused");
                var context = await Record(store, payload, "refused", First);
                Assert.IsType<Refused>((await Receipt(store, context, First)).Outcome);
                Assert.Null(await store.Occurrences.GetEffectReceipt(context.EffectKey(Second)));
                var settled = await executor.Execute(new FinishBatch("refused"), context, First, ["Worker"], CancellationToken.None);
                Assert.Equal(OccurrenceCommandDisposition.AlreadyRecorded, settled.Disposition);
                Assert.IsType<Refused>(settled.Outcome);
                var error = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(payload, context, CancellationToken.None));
                Assert.Contains("ProcessBatch", error.Message, StringComparison.Ordinal);
                Assert.Contains("FinishBatch", error.Message, StringComparison.Ordinal);
                Assert.Contains("BatchRequested", error.Message, StringComparison.Ordinal);
                Assert.Null(await store.Occurrences.GetEffectReceipt(context.EffectKey(Second)));
                Assert.Equal(4, (await store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count());
            }

            static async Task<OccurrenceContext> Record(IEventStore store, BatchRequested payload, string ingress, string lastEffect)
            {
                var recorded = await store.Occurrences.Record(payload, ingress);
                Assert.Equal(OccurrenceRecordStatus.Recorded, recorded.Status);
                var duplicate = await store.Occurrences.Record(payload, ingress);
                Assert.Equal(OccurrenceRecordStatus.AlreadyRecorded, duplicate.Status);
                Assert.Equal(recorded.Id, duplicate.Id);
                var receipt = await WaitForReceipt(store, new(recorded.Id, lastEffect, new(recorded.Id, 0)));
                return new(recorded.Id, new("__TRIGGER_ID__"), OccurrenceKind.Application, null, receipt.RecordedAt, default, 2, new(recorded.Id, receipt.Epoch + 1)) { Namespace = store.Namespace };
            }

            static async Task<(object Outcome, DateTimeOffset RecordedAt)> Receipt(IEventStore store, OccurrenceContext context, string effect)
            {
                var receipt = await store.Occurrences.GetEffectReceipt(context.EffectKey(effect));
                Assert.NotNull(receipt);
                return (receipt.Outcome, receipt.RecordedAt);
            }

            static async Task<(DateTimeOffset RecordedAt, long Epoch)> WaitForReceipt(IEventStore store, EffectKey key)
            {
                var until = DateTimeOffset.UtcNow.AddSeconds(90);
                while (DateTimeOffset.UtcNow < until)
                {
                    if (await store.Occurrences.GetEffectReceipt(key) is { } receipt) return (receipt.RecordedAt, receipt.WrittenBy.Epoch);
                    await Task.Delay(100);
                }
                throw new TimeoutException($"No command receipt for {key} in {store.Namespace}.");
            }
        }
        """;
}
#endif
