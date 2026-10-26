// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Host.for_SemanticRuntime.given;

namespace Cratis.Stage.Host.for_SemanticFactAppender.given;

public class a_translating_application : for_SemanticChronicleRegistration.given.a_compliance_store
{
    internal const string Translations = """
        module Integration
          feature Translation
            slice StateChange PackOrder
              command PackOrder
                orderId String identifier
                produces OrderPacked
                  for orderId
                  orderId = orderId
              event OrderPacked
                orderId String
            slice Translate PublishOrderShipped
              direction outbound
              public event OrderShipped
                status String
              projection OrderShippedPublisher => OrderShipped
                from OrderPacked
                  status = "packed"
            slice Translate TrackShipments
              direction inbound
              event ShipmentDispatched from "shipping"
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

    void Establish() => _plan = compiled_plan.From(Translations);
}
