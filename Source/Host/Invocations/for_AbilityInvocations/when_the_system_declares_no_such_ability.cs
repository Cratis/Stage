// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityInvocations;

public class when_the_system_declares_no_such_ability : an_invocation_silo
{
    InvocationAdmission _admission = null!;
    InvocationAdmission _notJson = null!;

    protected override IEnumerable<AbilityDescriptor> Abilities => [Describe("Post", EffectClass.Safe)];

    async Task Because()
    {
        _admission = await Invocations.Invoke(new("Slack", "Reply", Origin("event 1")));
        _notJson = await Invocations.Invoke(new("Slack", "Post", Origin("event 2"), "not json"));
    }

    [Fact] void should_not_admit_the_invocation() => _admission.Admitted.ShouldBeFalse();
    [Fact] void should_say_why() => _admission.Refusal.Contains("declares no ability Reply", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_record_anything() => _admission.Record.ShouldBeNull();
    [Fact] void should_not_admit_input_that_is_not_json() => _notJson.Admitted.ShouldBeFalse();
    [Fact] void should_not_call_the_extension() => _backend.Calls.IsEmpty.ShouldBeTrue();
}
