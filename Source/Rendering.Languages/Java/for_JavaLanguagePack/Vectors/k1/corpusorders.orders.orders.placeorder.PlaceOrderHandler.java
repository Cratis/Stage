// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

package corpusorders.orders.orders.placeorder;

import corpusorders.orders.events.EventTags;
import java.util.Optional;
import org.axonframework.messaging.commandhandling.annotation.CommandHandler;
import org.axonframework.messaging.eventhandling.gateway.EventAppender;
import org.axonframework.modelling.annotation.InjectEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.stereotype.Component;

@Component
public class PlaceOrderHandler {
    @CommandHandler
    @PreAuthorize("hasRole('Sales')")
    public void handle(corpusorders.orders.orders.placeorder.PlaceOrder command, EventAppender appender, @InjectEntity(idProperty = EventTags.ORDER_ID) Optional<PlaceOrderStream> state) {
        rules.validate(command);
        appender.append(new corpusorders.orders.events.OrderPlaced(command.orderId(), command.number(), command.customer(), command.delivery(), command.total(), command.channel(), command.status()));
    }
    private final PlaceOrderRules rules;
    public PlaceOrderHandler(PlaceOrderRules rules) {
        this.rules = rules;
    }
}
