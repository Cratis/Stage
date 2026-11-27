// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useIsAvailable } from '@cratis/arc.react/availability';
import type { CommandAvailability } from './commandAvailability';
import { useStageData } from './stageData';

/** What a renderer shows for one element, from the three things that can withhold it. */
export type ElementAvailability =
    /** Offered. */
    | { kind: 'available' }
    /** Still asking. Nothing is shown for a part that has not been decided, so a part is never offered before the server said it may be. */
    | { kind: 'unknown' }
    /** The part is not in the caller's offer: the element is not rendered. */
    | { kind: 'notAvailable' }
    /** An operator suspended the part: the element can say so briefly. */
    | { kind: 'suspended' }
    /** The caller may not use the command: the renderer's rule for unauthorized applies, and the reason is the server's. */
    | { kind: 'unauthorized'; reason?: string };

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
    const part = screen && name ? routes?.elements?.[elementLookup(screen, kind, name)] : undefined;
    // Arc asks the server about the data the part is about only when there is a part with a gate; an element without one needs no request.
    const answer = useIsAvailable(part ?? '', part ? values : undefined);

    if (!part) return available;
    if (!answer.isKnown) return unknown;
    if (answer.isAvailable) return available;
    return answer.isSuspended ? { kind: 'suspended' } : { kind: 'notAvailable' };
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
