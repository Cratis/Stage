// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

package corpusorders.orders.events;
import corpusorders.concepts.CustomerName;
import corpusorders.concepts.Money;
import corpusorders.concepts.OrderChannel;
import corpusorders.concepts.OrderId;
import corpusorders.concepts.OrderNumber;
import corpusorders.concepts.OrderStatus;
import java.time.LocalDate;
import org.axonframework.eventsourcing.annotation.EventTag;
import org.axonframework.messaging.eventhandling.annotation.Event;

@Event(namespace = "orders", name = "OrderPlaced", version = "1")
public record OrderPlaced(@EventTag(key = EventTags.ORDER_ID) OrderId orderId, OrderNumber number, CustomerName customer, LocalDate delivery, Money total, OrderChannel channel, OrderStatus status) {}
