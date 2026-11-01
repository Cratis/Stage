// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticInbox;

public class when_reading_the_origins_of_foreign_events : Specification
{
    const string Source = """
        module Tracking
          feature Receiving
            slice Translate TrackShipments
              direction inbound
              event ShipmentDispatched from "shipping"
                orderId String
              event ShipmentReturned from "shipping"
                orderId String
              event InvoiceIssued from "billing"
                orderId String
              event OrderDispatched
                orderId String
              capture ShipmentTracking
                source events
                  from ShipmentDispatched
                key orderId
                append OrderDispatched
                  orderId = $.orderId
        """;

    IReadOnlyDictionary<string, string[]> _origins = new Dictionary<string, string[]>();

    void Because() => _origins = SemanticInbox.Origins(compiled_plan.From(Source));

    [Fact] void should_name_every_store_a_foreign_event_comes_from() => _origins.Keys.OrderBy(key => key).ShouldEqual(["billing", "shipping"]);
    [Fact] void should_list_every_event_a_store_publishes_to_this_application() => _origins["shipping"].OrderBy(name => name).ShouldEqual(["ShipmentDispatched", "ShipmentReturned"]);
    [Fact] void should_not_subscribe_to_a_local_event() => _origins.Values.SelectMany(names => names).ShouldNotContain("OrderDispatched");
}
