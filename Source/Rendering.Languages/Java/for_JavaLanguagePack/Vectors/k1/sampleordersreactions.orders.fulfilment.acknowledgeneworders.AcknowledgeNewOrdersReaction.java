// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

package sampleordersreactions.orders.fulfilment.acknowledgeneworders;
import java.time.Clock;
import java.time.Instant;
import java.util.List;
import org.axonframework.messaging.commandhandling.gateway.CommandGateway;
import org.axonframework.messaging.core.annotation.Namespace;
import org.axonframework.messaging.eventhandling.annotation.EventHandler;
import org.axonframework.messaging.eventhandling.annotation.Timestamp;
import org.axonframework.messaging.eventhandling.replay.annotation.DisallowReplay;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.authority.SimpleGrantedAuthority;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.stereotype.Component;
import sampleordersreactions.orders.events.OrderPlaced;
import sampleordersreactions.orders.fulfilment.acknowledgeorder.AcknowledgeOrder;

@Component
@Namespace("reactions")
@DisallowReplay
public class AcknowledgeNewOrdersReaction {
    private final CommandGateway commands;
    private final Clock clock;
    public AcknowledgeNewOrdersReaction(CommandGateway commands, Clock clock) {
        this.commands = commands;
        this.clock = clock;
    }
    @EventHandler
    public void on0(OrderPlaced input, @Timestamp Instant occurred) {
        var previous = SecurityContextHolder.getContext();
        var identity = SecurityContextHolder.createEmptyContext();
        identity.setAuthentication(new UsernamePasswordAuthenticationToken("system", null, List.of(new SimpleGrantedAuthority("ROLE_Sales"))));
        try {
            SecurityContextHolder.setContext(identity);
            commands.sendAndWait(new AcknowledgeOrder(input.number(), input.number(), input.customer()));
        }
        finally {
            SecurityContextHolder.setContext(previous);
        }
    }
}
