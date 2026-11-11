// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityInvocations;

public class when_an_ability_accepts_work_to_answer_later : an_invocation_silo
{
    string _identity = null!;
    InvocationRecord _waiting = null!;
    InvocationAnswer _anonymous;
    InvocationAnswer _notAManager;
    InvocationAnswer _undeclared;
    InvocationAnswer _unknown;
    InvocationAnswer _answered;
    InvocationAnswer _again;
    InvocationRecord _settled = null!;

    protected override IEnumerable<AbilityDescriptor> Abilities =>
        [Describe("Ask", EffectClass.Idempotent, "approved", "rejected") with { AnswersLater = true, AnswerExpiresAfter = TimeSpan.FromMinutes(5) }];

    static ClaimsPrincipal Person(string name, params string[] roles) =>
        new(new ClaimsIdentity([new(ClaimTypes.Name, name), .. roles.Select(role => new Claim(ClaimTypes.Role, role))], "spec"));

    async Task Because()
    {
        _backend.Behavior = _ => Task.FromResult<AbilityResult>(new AbilityAccepted());
        var admission = await Invocations.Invoke(new("Slack", "Ask", Origin("event 51", InvocationOriginKind.WorkflowAction), """{"question":"Back-order?"}""", "Managers"));
        _identity = admission.Record!.Identity;
        await Eventually(async () => (await Ledger.Get(_identity))!.Status == InvocationStatus.Suspended, "the invocation to wait for its answer");
        _waiting = (await Ledger.Get(_identity))!;

        _anonymous = await Invocations.Answer(_identity, "approved", "{}", new ClaimsPrincipal(new ClaimsIdentity()));
        _notAManager = await Invocations.Answer(_identity, "approved", "{}", Person("pat", "clerk"));
        _undeclared = await Invocations.Answer(_identity, "maybe", "{}", Person("maria", "manager"));
        _unknown = await Invocations.Answer("inv-nothing", "approved", "{}", Person("maria", "manager"));
        _answered = await Invocations.Answer(_identity, "approved", """{"note":"ok"}""", Person("maria", "manager"));
        await Eventually(() => !_workflows.Settlements.IsEmpty, "the workflow to take its outlet");
        _settled = _workflows.Settlements.Single();
        _again = await Invocations.Answer(_identity, "rejected", "{}", Person("maria", "manager"));
    }

    [Fact] void should_wait_without_ending() => _waiting.Status.ShouldEqual(InvocationStatus.Suspended);
    [Fact] void should_have_called_the_extension_once() => _backend.Calls.Count.ShouldEqual(1);
    [Fact] void should_refuse_an_identity_that_is_not_authenticated() => _anonymous.ShouldEqual(InvocationAnswer.Forbidden);
    [Fact] void should_refuse_an_identity_that_does_not_satisfy_the_policy() => _notAManager.ShouldEqual(InvocationAnswer.Forbidden);
    [Fact] void should_refuse_an_outlet_the_ability_does_not_declare() => _undeclared.ShouldEqual(InvocationAnswer.UnknownOutlet);
    [Fact] void should_not_know_an_invocation_that_was_never_recorded() => _unknown.ShouldEqual(InvocationAnswer.Unknown);
    [Fact] void should_take_the_answer_of_an_identity_that_satisfies_the_policy() => _answered.ShouldEqual(InvocationAnswer.Answered);
    [Fact] void should_continue_the_origin_through_the_outlet_of_the_answer() => InvocationBranch.Of(_settled).ShouldEqual("approved");
    [Fact] void should_carry_the_answer() => _settled.Output.ShouldEqual("""{"note":"ok"}""");
    [Fact] void should_record_the_identity_that_answered() => _settled.DecidedBy.ShouldEqual("maria");
    [Fact] void should_not_take_a_second_answer() => _again.ShouldEqual(InvocationAnswer.NotWaiting);
    [Fact] void should_settle_the_origin_once() => _workflows.Settlements.Count.ShouldEqual(1);
}
