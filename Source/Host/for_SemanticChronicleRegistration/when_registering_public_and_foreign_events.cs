// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_registering_public_and_foreign_events : for_SemanticFactAppender.given.a_translating_application
{
    IReadOnlyDictionary<string, EventTypeRegistration> _registered = new Dictionary<string, EventTypeRegistration>();

    async Task Because()
    {
        await SemanticChronicleRegistration.Register(_client, "Compliance", _plan);
        _registered = _registrations.SelectMany(request => request.Types).ToDictionary(registration => registration.Type.Id);
    }

    [Fact] void should_register_the_published_event_as_public() => _registered["OrderShipped"].Visibility.ShouldEqual(EventTypeVisibility.Public);
    [Fact] void should_register_a_private_event_as_private() => _registered["OrderPacked"].Visibility.ShouldEqual(EventTypeVisibility.Private);
    [Fact] void should_say_which_event_store_a_foreign_event_comes_from() => _registered["ShipmentDispatched"].EventStore.ShouldEqual("shipping");
    [Fact] void should_not_name_an_origin_for_a_local_event() => _registered["OrderPacked"].EventStore.ShouldBeEmpty();
}
