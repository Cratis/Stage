// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text;
using Cratis.Arc.Chronicle.Occurrences;
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceHandlerArtifactRenderer;

public class when_rendering_mixed_application_trigger_effects : Specification
{
    string _handler = string.Empty;
    Assembly _assembly = null!;
    readonly List<(object Command, EffectKey Effect)> _calls = [];
    readonly List<string> _order = [];
    readonly HashSet<EffectKey> _commandReceipts = [];
    int _begun;
    int _committed;
    bool _receipted;
    string _productionEffect = string.Empty;

    async Task Because()
    {
        var model = for_SemanticAutomationArtifactRenderer.given.an_automation.Compile(for_SemanticOccurrenceAdmission.when_admitting_application_trigger_reactions.with_productions_and_invocations.MixedSource);
        var reaction = model.Application.Modules.Single().Features.Single().Slices.Single().Reactions.Single();
        _productionEffect = $"stage:reaction:{reaction.Id}:productions";
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Reactions", "Reactions"));
        plan.Diagnostics.ShouldBeEmpty();
        plan.Success.ShouldBeTrue();
        _handler = Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => Path.GetFileName(artifact.RelativePath) == "ProcessBatch.cs").Bytes.AsSpan());
        var files = when_rendering_application_trigger_productions.Files(plan);
        for_CratisRenderer.RenderedOutput.Warnings(files).ShouldBeEmpty();
        _assembly = for_CratisRenderer.RenderedOutput.Load(files);
        await Deliver();
        _receipted = true;
        await Deliver();
    }

    async Task Deliver(int? stopAt = null)
    {
        var invocationIndex = 0;
        var client = Substitute.For<IChronicleClient>();
        var injected = DispatchProxy.Create<IEventStore, StoreProxy>();
        var recording = DispatchProxy.Create<IEventStore, StoreProxy>();
        var occurrences = Substitute.For<IOccurrences>();
        var units = Substitute.For<IUnitOfWorkManager>();
        var unit = DispatchProxy.Create<IUnitOfWork, StoreProxy>();
        var executor = Substitute.For<IOccurrenceCommandExecutor>();
        var occurrenceId = new OccurrenceId("12345678-1234-1234-1234-123456789abc");
        var lease = new LeaseToken(occurrenceId, 1);
        var context = new OccurrenceContext(occurrenceId, new("batch-requested"), OccurrenceKind.Application, null, DateTimeOffset.UtcNow, default, 1, lease) { Namespace = new("recording") };
        ((StoreProxy)(object)injected).Properties["get_Name"] = new EventStoreName("stage343-mixed");
        ((StoreProxy)(object)recording).Properties["get_Occurrences"] = occurrences;
        ((StoreProxy)(object)recording).Properties["get_UnitOfWorkManager"] = units;
        client.GetEventStore(injected.Name, context.Namespace).Returns(recording);
        var production = context.EffectKey(new(_productionEffect));
        if (_receipted) occurrences.GetEffectReceipt(production).Returns(new EffectReceipt(production, new Appended(EventSequenceId.Log, new(0), new(0)), null, lease, DateTimeOffset.UtcNow));
        units.Begin(Arg.Any<CorrelationId>()).Returns(_ =>
        {
            _begun++;
            _order.Add("production");
            return unit;
        });
        var unitCalls = ((StoreProxy)(object)unit).Properties;
        unitCalls["get_IsSuccess"] = true;
        unitCalls["UseEffectReceipt"] = (Func<object?>)(() => null);
        unitCalls["AddEvents"] = (Func<object?>)(() => null);
        unitCalls["Commit"] = (Func<object?>)(() =>
        {
            _committed++;
            return Task.CompletedTask;
        });
        executor.Execute(Arg.Any<object>(), Arg.Any<OccurrenceContext>(), Arg.Any<EffectName>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var key = call.ArgAt<OccurrenceContext>(1).EffectKey(call.ArgAt<EffectName>(2));
                _calls.Add((call.ArgAt<object>(0), key));
                _order.Add("invocation");
                if (invocationIndex++ == stopAt) return new OccurrenceCommandResult(OccurrenceCommandDisposition.Failed, null, null, null);
                return new OccurrenceCommandResult(_commandReceipts.Add(key) ? OccurrenceCommandDisposition.Recorded : OccurrenceCommandDisposition.AlreadyRecorded, new NoEvents(), null, null);
            });
        var type = _assembly.GetTypes().Single(item => item.Name == "ProcessBatch");
        var payload = Activator.CreateInstance(_assembly.GetTypes().Single(item => item.Name == "BatchRequested"), "batch-42")!;
        var handler = Activator.CreateInstance(type, client, injected, executor)!;
        await (Task)type.GetMethod("Handle")!.Invoke(handler, [payload, context, CancellationToken.None])!;
    }

    [Fact] void should_compile_with_no_diagnostics() => _assembly.ShouldNotBeNull();
    [Fact] void should_commit_the_production_only_once() => _committed.ShouldEqual(1);
    [Fact] void should_skip_the_receipted_production() => _begun.ShouldEqual(1);
    [Fact] void should_still_invoke_both_commands_on_redelivery() => _calls.Count.ShouldEqual(4);
    [Fact] void should_produce_before_invoking() => string.Join(',', _order).ShouldEqual("production,invocation,invocation,invocation,invocation");
    [Fact] void should_keep_the_invocation_keys_on_redelivery() => _calls.Select(call => call.Effect).Take(2).SequenceEqual(_calls.Select(call => call.Effect).Skip(2)).ShouldBeTrue();
    [Fact] void should_preserve_invocation_mapping_order() => string.Join(',', _calls.Select(call => (string)call.Command.GetType().GetProperty("BatchId")!.GetValue(call.Command)!)).ShouldEqual("batch-42,second,batch-42,second");

    [Fact]
    void should_keep_the_receipt_guard_local_to_productions()
    {
        _handler.ShouldContain("if (await store.Occurrences.GetEffectReceipt(effect) is null)");
        _handler.ShouldNotContain("is not null) return;");
        _handler.IndexOf("unitOfWork.Commit", StringComparison.Ordinal).ShouldBeLessThan(_handler.IndexOf("commands.Execute", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task should_resume_after_productions_or_a_partial_invocation_delivery(int stopAt)
    {
        _calls.Clear();
        _commandReceipts.Clear();
        _receipted = false;
        _begun = 0;
        _committed = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => Deliver(stopAt));
        _commandReceipts.Count.ShouldEqual(stopAt);
        _calls.Count.ShouldEqual(stopAt + 1);
        _committed.ShouldEqual(1);
        _receipted = true;
        await Deliver();
        _commandReceipts.Count.ShouldEqual(2);
        _calls.Count.ShouldEqual(stopAt + 3);
        _begun.ShouldEqual(1);
        _committed.ShouldEqual(1);
    }

    public class StoreProxy : DispatchProxy
    {
        public Dictionary<string, object> Properties { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            var value = Properties.TryGetValue(targetMethod!.Name, out var configured) ? configured : throw new NotSupportedException(targetMethod.Name);
            return value is Func<object?> callback ? callback() : value;
        }
    }
}
