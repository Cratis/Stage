// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.Occurrences;
using Cratis.Chronicle.Occurrences;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceHandlerArtifactRenderer;

public class when_delivering_application_trigger_invocations : Specification
{
    Assembly _assembly = null!;
    readonly List<EffectKey> _keys = [];
    readonly List<string[]> _roles = [];
    readonly OccurrenceContext _context = new(new("invocation-only"), new("batch-requested"), OccurrenceKind.Application, null, DateTimeOffset.UtcNow, default, 1, new(new("invocation-only"), 1));
    int _deliveries;
    OccurrenceCommandResult _result = null!;
    Exception? _failure;

    void Establish()
    {
        var source = for_SemanticOccurrenceAdmission.given.an_application_trigger_reaction.InvocationSource
            .Replace("      reaction ProcessBatch", "      reaction ProcessBatch\n        runs as system role \"Worker\"", StringComparison.Ordinal)
            + "\n          invokes FinishBatch\n            batchId = \"second\"\n";
        var model = for_SemanticAutomationArtifactRenderer.given.an_automation.Compile(source);
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Reactions", "Reactions"));
        plan.Diagnostics.WithoutAuthenticatedDefaultNotes().ShouldBeEmpty();
        _assembly = for_CratisRenderer.RenderedOutput.Load(when_rendering_application_trigger_productions.Files(plan));
    }

    async Task Deliver()
    {
        var executor = Substitute.For<IOccurrenceCommandExecutor>();
        executor.Execute(Arg.Any<object>(), Arg.Any<OccurrenceContext>(), Arg.Any<EffectName>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _keys.Add(call.ArgAt<OccurrenceContext>(1).EffectKey(call.ArgAt<EffectName>(2)));
                _roles.Add([.. call.ArgAt<IEnumerable<string>>(3)]);
                return _result;
            });
        var type = _assembly.GetTypes().Single(item => item.Name == "ProcessBatch");
        var payload = Activator.CreateInstance(_assembly.GetTypes().Single(item => item.Name == "BatchRequested"), "batch-42");
        var handler = Activator.CreateInstance(type, executor);
        _deliveries++;
        try
        {
            await (Task)type.GetMethod("Handle").Invoke(handler, [payload, _context, CancellationToken.None]);
        }
        catch (InvalidOperationException exception)
        {
            _failure = exception;
        }
    }

    [Theory]
    [InlineData(OccurrenceCommandDisposition.Recorded)]
    [InlineData(OccurrenceCommandDisposition.AlreadyRecorded)]
    public async Task should_continue_after_a_settled_success(OccurrenceCommandDisposition disposition)
    {
        _result = new(disposition, new NoEvents(), null, null);
        await Deliver();
        _failure.ShouldBeNull();
        _keys.Count.ShouldEqual(2);
        _keys.Distinct().Count().ShouldEqual(2);
        _roles.TrueForAll(roles => roles.SequenceEqual(["Worker"])).ShouldBeTrue();
    }

    [Theory]
    [InlineData(OccurrenceCommandDisposition.Recorded, true, "was refused")]
    [InlineData(OccurrenceCommandDisposition.AlreadyRecorded, true, "was refused")]
    [InlineData(OccurrenceCommandDisposition.Failed, false, "did not settle")]
    public async Task should_stop_later_invocations_and_fail_the_handler(OccurrenceCommandDisposition disposition, bool refused, string message)
    {
        _result = new(disposition, refused ? new Refused(["validation"]) : null, null, null);
        await Deliver();
        _keys.Count.ShouldEqual(1);
        _failure.ShouldNotBeNull();
        _failure.Message.ShouldContain(message);
    }

    [Fact]
    public async Task should_reuse_the_same_effect_keys_on_redelivery()
    {
        _result = new(OccurrenceCommandDisposition.Recorded, new NoEvents(), null, null);
        await Deliver();
        _result = new(OccurrenceCommandDisposition.AlreadyRecorded, new NoEvents(), null, null);
        await Deliver();
        _deliveries.ShouldEqual(2);
        _failure.ShouldBeNull();
        _keys.Count.ShouldEqual(4);
        _keys.Take(2).SequenceEqual(_keys.Skip(2)).ShouldBeTrue();
    }
}
