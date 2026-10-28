// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

package corpusorders.orders.orders.placeorder;

import corpusorders.concepts.CustomerName;
import corpusorders.concepts.Money;
import corpusorders.concepts.OrderChannel;
import corpusorders.concepts.OrderId;
import corpusorders.concepts.OrderNumber;
import corpusorders.concepts.OrderStatus;
import jakarta.validation.Valid;
import jakarta.validation.constraints.NotNull;
import java.time.LocalDate;
import org.axonframework.messaging.commandhandling.annotation.Command;
import org.axonframework.modelling.annotation.TargetEntityId;

@Command(name = "PlaceOrder")
public record PlaceOrder(@TargetEntityId @NotNull @Valid OrderId orderId, @NotNull @Valid OrderNumber number, @NotNull @Valid CustomerName customer, @NotNull LocalDate delivery, @NotNull @Valid Money total, @NotNull OrderChannel channel, @NotNull OrderStatus status) {
}
