// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useContext, useEffect, useState } from 'react';
import { ArcContext } from '@cratis/arc.react';
import { availabilityOf, fetchUnavailableParts, type ElementAvailability } from './availabilityClient';
import type { CommandAvailability } from './commandAvailability';
import { useStageData } from './stageData';

const available: ElementAvailability = { kind: 'available' };
const unknown: ElementAvailability = { kind: 'unknown' };

/** The kind of screen element that can carry an explicit availability gate. */
export type GatedElementKind = 'action' | 'data';

/** The lookup a client finds an element's marker by, the same the Stage publishes in the routes. */
export function elementLookup(screen: string, kind: GatedElementKind, name: string): string {
    return `${screen}/${kind}/${name}`;
}

/**
 * What the server says about the explicit `available` gate of a screen element, asked with the values the screen shows. An element without a gate needs
 * no request. An element is not offered until the server has said it may be, and a failed request keeps it closed.
 * @param kind Whether the element is an action or a data element.
 * @param name The name of the command or query it is bound to.
 * @param values The values the screen shows, by property name of the command or query. They decide availability and grant nothing.
 * @returns The availability of the element's own gate.
 */
export function useElementGate(kind: GatedElementKind, name: string, values: Record<string, unknown>): ElementAvailability {
    const { routes, screen } = useStageData();
    const arc = useContext(ArcContext);
    const part = screen && name ? routes?.elements?.[elementLookup(screen, kind, name)] : undefined;
    const key = JSON.stringify(values);
    const question = `${part ?? ''}:${key}`;
    const [answer, setAnswer] = useState<{ question: string; availability: ElementAvailability }>();

    useEffect(() => {
        if (!part) return undefined;
        let abort = new AbortController();
        const refresh = () => {
            abort.abort();
            const request = new AbortController();
            abort = request;
            void fetchUnavailableParts(request.signal, arc, { part, arguments: JSON.parse(key) as Record<string, unknown> })
                .then(unavailable => { if (!request.signal.aborted) setAnswer({ question, availability: availabilityOf(unavailable.get(part)) }); })
                .catch(() => { if (!request.signal.aborted) setAnswer(undefined); });
        };
        refresh();
        const interval = globalThis.setInterval(refresh, 30_000);
        globalThis.addEventListener('focus', refresh);
        return () => {
            abort.abort();
            globalThis.clearInterval(interval);
            globalThis.removeEventListener('focus', refresh);
        };
    }, [part, key, question, arc]);

    if (!part) return available;
    return answer?.question === question ? answer.availability : unknown;
}

/** What the renderer does with a command the server refused: the same kinds the element's own gate has, with the reason of an unauthorized one. */
export function availabilityOfCommand(command: CommandAvailability): ElementAvailability {
    if (command.kind === 'notAvailable') return { kind: 'notAvailable' };
    if (command.kind === 'suspended') return { kind: 'suspended' };
    if (command.kind === 'unauthorized') return { kind: 'unauthorized', reason: command.reason };
    return available;
}

/**
 * Combines everything that can withhold an element. A part that is not in the caller's offer outranks one an operator suspended, which outranks
 * one still being asked about, which outranks one the caller may not use; an element is offered only when nothing withholds it.
 * @param parts The availabilities that apply to the element.
 * @returns The availability the renderer acts on.
 */
export function combineAvailability(...parts: ElementAvailability[]): ElementAvailability {
    return parts.find(part => part.kind === 'notAvailable')
        ?? parts.find(part => part.kind === 'suspended')
        ?? parts.find(part => part.kind === 'unknown')
        ?? parts.find(part => part.kind === 'unauthorized')
        ?? available;
}
