// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Cratis.Stage.Extensions.Protocol;
using Cratis.Stage.Host.Extensions.given;
using Cratis.Stage.Host.Invocations;
using NSubstitute;
using Xunit;
using RequestContext = Cratis.Stage.Extensions.Protocol.RequestContext;

namespace Cratis.Stage.Host.Extensions.for_StageHostService;

public class when_answering_an_invocation : an_extension_protocol
{
    AnswerResponse _answered = null!;
    AnswerResponse _notWaiting = null!;
    AnswerResponse _forbidden = null!;
    AnswerResponse _unknownOutlet = null!;
    AnswerResponse _unknownInvocation = null!;
    ClaimsPrincipal _answeredBy = null!;
    string _payload = null!;

    async Task Establish()
    {
        await StartBackend();
        await StartStage();
        _invocations.Answer("answered", "posted", Arg.Any<string>(), Arg.Any<ClaimsPrincipal>()).Returns(call =>
        {
            _answeredBy = call.Arg<ClaimsPrincipal>();
            _payload = call.ArgAt<string>(2);
            return InvocationAnswer.Answered;
        });
        _invocations.Answer("not-waiting", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ClaimsPrincipal>()).Returns(InvocationAnswer.NotWaiting);
        _invocations.Answer("forbidden", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ClaimsPrincipal>()).Returns(InvocationAnswer.Forbidden);
        _invocations.Answer("unknown-outlet", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ClaimsPrincipal>()).Returns(InvocationAnswer.UnknownOutlet);
        _invocations.Answer("nobody", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ClaimsPrincipal>()).Returns(InvocationAnswer.Unknown);
    }

    async Task Because()
    {
        var host = await Connected();
        _answered = await Answer(host, "answered", "posted", "{\"ts\":\"1\"}");
        _notWaiting = await Answer(host, "not-waiting", "posted", "{}");
        _forbidden = await Answer(host, "forbidden", "posted", "{}");
        _unknownOutlet = await Answer(host, "unknown-outlet", "nope", "{}");
        _unknownInvocation = await Answer(host, "nobody", "posted", "{}");
    }

    static Task<AnswerResponse> Answer(StageHost.StageHostClient host, string invocation, string outlet, string payload) =>
        host.AnswerAsync(new AnswerRequest { Context = new RequestContext { InvocationId = invocation }, Outlet = outlet, PayloadJson = payload }).ResponseAsync;

    [Fact] void should_answer_the_invocation() => _answered.Status.ShouldEqual(AnswerResponse.Types.Status.Answered);
    [Fact] void should_pass_the_payload_on() => _payload.ShouldEqual("{\"ts\":\"1\"}");
    [Fact] void should_answer_as_the_backend_of_the_system() => _answeredBy.Identities.OfType<ExtensionIdentity>().Single().System.ShouldEqual("Slack");
    [Fact] void should_report_an_invocation_that_does_not_wait() => _notWaiting.Status.ShouldEqual(AnswerResponse.Types.Status.NotWaiting);
    [Fact] void should_report_an_answer_it_may_not_give() => _forbidden.Status.ShouldEqual(AnswerResponse.Types.Status.Forbidden);
    [Fact] void should_report_an_outlet_the_ability_does_not_declare() => _unknownOutlet.Status.ShouldEqual(AnswerResponse.Types.Status.UnknownOutlet);
    [Fact] void should_report_an_invocation_it_does_not_know() => _unknownInvocation.Status.ShouldEqual(AnswerResponse.Types.Status.UnknownInvocation);
}
