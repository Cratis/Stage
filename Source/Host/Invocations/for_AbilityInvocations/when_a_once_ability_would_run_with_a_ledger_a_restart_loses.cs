// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityInvocations;

public class when_a_once_ability_would_run_with_a_ledger_a_restart_loses : an_invocation_silo
{
    InvocationAdmission _once = null!;
    InvocationAdmission _idempotent = null!;

    protected override IEnumerable<AbilityDescriptor> Abilities => [Describe("Pay", EffectClass.Once), Describe("Post", EffectClass.Idempotent)];

    protected override void Configure(WebApplicationBuilder builder) => builder.Configuration["Stage:Invocations:AllowVolatileOnce"] = "false";

    async Task Because()
    {
        _once = await Invocations.Invoke(new("Slack", "Pay", Origin("event 81")));
        _idempotent = await Invocations.Invoke(new("Slack", "Post", Origin("event 82")));
        await Eventually(() => !_reactions.Settlements.IsEmpty, "the idempotent invocation to settle");
    }

    [Fact] void should_refuse_the_once_invocation() => _once.Admitted.ShouldBeFalse();
    [Fact] void should_say_how_to_make_the_ledger_durable() => _once.Refusal.Contains("Stage:Orleans:Storage", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_record_it() => _once.Record.ShouldBeNull();
    [Fact] void should_not_call_the_extension_for_it() => _backend.Calls.Select(call => call.Ability.Ability).ShouldContainOnly("Post");
    [Fact] void should_run_an_ability_a_repeat_cannot_hurt() => _idempotent.Admitted.ShouldBeTrue();
}
