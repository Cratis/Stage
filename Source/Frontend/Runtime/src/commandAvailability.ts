// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useContext, useEffect, useState } from 'react';
import { ArcContext, type ArcConfiguration } from '@cratis/arc.react';
import type { Command } from '@cratis/arc/commands';
import { apiBasePathFor } from './hostedBasePath';

/** What the server says a caller may do with a command for the data a screen is showing. */
export interface CommandAvailability {
    /** Whether the server refused the command to this caller for these values. Anything else - answered, unanswered or failed - leaves the action offered. */
    unavailable: boolean;

    /** Why the server refused it, when it said. */
    reason?: string;
}

const offered: CommandAvailability = { unavailable: false };

/**
 * Asks the server whether the command is authorized for the values the screen is showing, without running it. The server decides, so a policy that
 * reads recorded state, such as the plan of the asset on the screen, answers for that asset; the answer is asked again when the data the screen reads
 * changes, so an action withheld for one plan is offered again when the plan changes, without a reload. Only a refusal withholds the action: an
 * unanswered or failed probe leaves it offered, because the guard controls what is offered and never what the system accepts.
 */
export function useCommandAvailability(commandType: (new () => Command) | undefined, values: Record<string, unknown>, version: unknown): CommandAvailability {
    const arc = useContext(ArcContext);
    const [availability, setAvailability] = useState<CommandAvailability>(offered);
    const key = JSON.stringify(values);

    useEffect(() => {
        if (!commandType) {
            setAvailability(offered);
            return undefined;
        }

        let current = true;
        void probe(commandType, values, arc).then(answer => {
            if (current) setAvailability(answer);
        });
        return () => { current = false; };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- the values are compared by content through the key
    }, [commandType, key, version, arc]);

    return availability;
}

async function probe(commandType: new () => Command, values: Record<string, unknown>, arc: ArcConfiguration): Promise<CommandAvailability> {
    try {
        const command = new commandType();
        command.setMicroservice(arc.microservice);
        command.setApiBasePath(apiBasePathFor(arc.apiBasePath));
        command.setOrigin(arc.origin ?? '');
        command.setHttpHeadersCallback(arc.httpHeadersCallback ?? (() => ({})));
        for (const descriptor of command.propertyDescriptors) {
            if (Object.prototype.hasOwnProperty.call(values, descriptor.name)) Reflect.set(command, descriptor.name, values[descriptor.name]);
        }

        const result = await command.validate();
        return result.isAuthorized === false ? { unavailable: true, reason: result.authorizationFailureReason || undefined } : offered;
    } catch {
        return offered;
    }
}
