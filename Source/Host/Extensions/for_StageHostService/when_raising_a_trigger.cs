// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Extensions.Protocol;
using Cratis.Stage.Host.Extensions.given;
using NSubstitute;
using Xunit;
using RequestContext = Cratis.Stage.Extensions.Protocol.RequestContext;

namespace Cratis.Stage.Host.Extensions.for_StageHostService;

public class when_raising_a_trigger : an_extension_protocol
{
    RaiseTriggerResponse _raised = null!;
    RaiseTriggerResponse _notRouted = null!;
    RaiseTriggerResponse _refused = null!;
    RaiseTriggerResponse _unknown = null!;
    TriggerRaise _signalled = null!;

    async Task Establish()
    {
        await StartBackend();
        await StartStage();
        _backend.Description.Triggers.Add(new TriggerDeclaration { Name = "MessageReceived" });
        _backend.Description.Triggers.Add(new TriggerDeclaration { Name = "ReactionAdded" });
        _backend.Description.Triggers.Add(new TriggerDeclaration { Name = "ChannelArchived" });
        _triggers.Signal(Arg.Is<TriggerRaise>(raise => raise.Trigger == "MessageReceived")).Returns(call =>
        {
            _signalled = call.Arg<TriggerRaise>();
            return new TriggerRaised(TriggerRaiseStatus.Raised);
        });
        _triggers.Signal(Arg.Is<TriggerRaise>(raise => raise.Trigger == "ReactionAdded")).Returns(new TriggerRaised(TriggerRaiseStatus.NotRouted, "Nothing reacts to Slack.ReactionAdded."));
        _triggers.Signal(Arg.Is<TriggerRaise>(raise => raise.Trigger == "ChannelArchived")).Returns(new TriggerRaised(TriggerRaiseStatus.Refused, "The engine did not admit it."));
    }

    async Task Because()
    {
        var host = await Connected();
        var context = new RequestContext { Tenant = "Default", InvocationId = "invocation-1" };
        _raised = await host.RaiseTriggerAsync(new RaiseTriggerRequest { Context = context, Trigger = "MessageReceived", DeduplicationKey = "msg-1", PayloadJson = "{\"text\":\"hi\"}", CorrelationKey = "pay-1" });
        _notRouted = await host.RaiseTriggerAsync(new RaiseTriggerRequest { Context = context, Trigger = "ReactionAdded", DeduplicationKey = "r-1" });
        _refused = await host.RaiseTriggerAsync(new RaiseTriggerRequest { Context = context, Trigger = "ChannelArchived", DeduplicationKey = "c-1" });
        _unknown = await host.RaiseTriggerAsync(new RaiseTriggerRequest { Context = context, Trigger = "Undeclared", DeduplicationKey = "u-1" });
    }

    [Fact] void should_raise_a_declared_trigger() => _raised.Status.ShouldEqual(RaiseTriggerResponse.Types.Status.Raised);
    [Fact] void should_raise_it_for_the_system_of_the_credential() => _signalled.System.ShouldEqual("Slack");
    [Fact] void should_pass_the_deduplication_key_and_the_payload_on() => (_signalled.DeduplicationKey == "msg-1" && _signalled.PayloadJson == "{\"text\":\"hi\"}").ShouldBeTrue();
    [Fact] void should_pass_the_tenant_and_the_causation_on() => (_signalled.Tenant == "Default" && _signalled.Causation == "invocation-1").ShouldBeTrue();
    [Fact] void should_report_that_nothing_reacts() => (_notRouted.Status == RaiseTriggerResponse.Types.Status.NotRouted && _notRouted.Detail.Length > 0).ShouldBeTrue();
    [Fact] void should_report_a_refusal() => _refused.Status.ShouldEqual(RaiseTriggerResponse.Types.Status.Refused);
    [Fact] void should_not_know_a_trigger_the_contract_does_not_declare() => _unknown.Status.ShouldEqual(RaiseTriggerResponse.Types.Status.UnknownTrigger);
    [Fact] void should_not_signal_an_undeclared_trigger() => _triggers.ReceivedCalls().Any(call => call.GetArguments().OfType<TriggerRaise>().Any(raise => raise.Trigger == "Undeclared")).ShouldBeFalse();
    [Fact] void should_pass_what_the_trigger_is_about_on() => _signalled.Correlation.ShouldEqual("pay-1");
}
