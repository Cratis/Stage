// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticFactAppender;

public class when_counting_what_reaches_the_event_log : given.a_translating_application
{
    int _logged;

    void Because()
    {
        var packed = _plan.Events.Values.Single(contract => contract.Name == "OrderPacked");
        var shipped = _plan.Events.Values.Single(contract => contract.Name == "OrderShipped");
        var dispatched = _plan.Events.Values.Single(contract => contract.Name == "ShipmentDispatched");
        _logged = SemanticPublicEvents.Logged(_plan,
        [
            new(packed.Id, SemanticValue.Text("order-1"), []),
            new(shipped.Id, SemanticValue.Text("order-1"), []),
            new(dispatched.Id, SemanticValue.Text("order-1"), [])
        ]);
    }

    [Fact] void should_count_only_the_private_event() => _logged.ShouldEqual(1);
}
