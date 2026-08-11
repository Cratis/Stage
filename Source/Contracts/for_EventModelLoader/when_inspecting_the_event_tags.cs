// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_inspecting_the_event_tags : given.a_compiled_model_using_2x_constructs
{
    IReadOnlyList<string> _tagged = [];
    IReadOnlyList<string> _untagged = [];

    void Because()
    {
        _tagged = _slice.Events.Single(@event => @event.Name == "InvoiceActivated").Tags;
        _untagged = _slice.Events.Single(@event => @event.Name == "ContractPolicyActivated").Tags;
    }

    // A bare identifier and a quoted literal are both constant tags and both survive.
    [Fact] void should_carry_every_declared_tag() => _tagged.ShouldContainOnly(["invoicing", "billing"]);
    [Fact] void should_leave_an_untagged_event_with_no_tags() => _untagged.ShouldBeEmpty();
}
