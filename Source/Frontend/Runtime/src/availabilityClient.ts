// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Globals } from '@cratis/arc';
import { buildQueryHttpRequest, Paging, QueryHttpMethod, Sorting } from '@cratis/arc/queries';
import type { ArcConfiguration } from '@cratis/arc.react';
import { apiBasePathFor } from './hostedBasePath';

/**
 * Why Arc says a part is not offered. `NotAvailable` is a part that is not in the caller's offer; `Suspended` is a part an operator switched off
 * and will switch on again. A server that predates suspension reports no reason, which is `NotAvailable`.
 */
export type AvailabilityReason = 'NotAvailable' | 'Suspended';

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

/** What is asked of `/.cratis/availability`: every part, or one part and the data it is about. */
export interface AvailabilityQuestion {
    /** The full name of the command, or of the type that declares the query, or the exact marker key of a gated screen or element. Every part is decided when it is not given. */
    part?: string;

    /** The values the screen shows, by property name of the command or query. They only decide availability; they never grant anything. */
    arguments?: Record<string, unknown>;
}

/** The parts the server reports as not available, by name, with the reason each one is refused. */
export type UnavailableParts = ReadonlyMap<string, AvailabilityReason>;

/** Reads an authorization failure reason, or the reason of an unavailable part, as the availability the renderer reasons about. */
export function reasonOf(reason: string | undefined): AvailabilityReason | undefined {
    if (reason === 'Suspended') return 'Suspended';
    if (reason === 'NotAvailable') return 'NotAvailable';
    return undefined;
}

/**
 * The availability of an element that the server refused, or `available` when it did not.
 * @param refused The reason of the refusal, when there was one.
 * @returns What the renderer does with the element.
 */
export function availabilityOf(refused: AvailabilityReason | undefined): ElementAvailability {
    if (refused === 'Suspended') return { kind: 'suspended' };
    if (refused === 'NotAvailable') return { kind: 'notAvailable' };
    return { kind: 'available' };
}

/**
 * Reads Arc's request-specific unavailable-part snapshot, for every part or for one part and the data it is about. A failed request never opens a part.
 * @param signal Cancels an abandoned request.
 * @param arc The host's Arc transport configuration.
 * @param question What to ask about; everything when omitted.
 * @returns The unavailable parts and why each one is refused.
 */
export async function fetchUnavailableParts(signal: AbortSignal, arc: ArcConfiguration = { microservice: '' }, question: AvailabilityQuestion = {}): Promise<UnavailableParts> {
    const headers = new Headers(arc.httpHeadersCallback?.());
    headers.set('Accept', 'application/json');
    headers.set('Content-Type', 'application/json');
    if (arc.microservice) headers.set(Globals.microserviceHttpHeader, arc.microservice);
    const args: Record<string, unknown> = { waitForFirstResult: true };
    if (question.part) args.part = question.part;
    if (question.part && question.arguments && Object.keys(question.arguments).length > 0) args.arguments = JSON.stringify(question.arguments);
    const { url, init } = buildQueryHttpRequest(QueryHttpMethod.Get, {
        route: '/.cratis/availability',
        origin: arc.origin ?? '',
        apiBasePath: apiBasePathFor(arc.apiBasePath),
        args,
        parameterValues: {},
        paging: Paging.noPaging,
        sorting: Sorting.none,
        headers,
        signal,
    });
    const response = await fetch(url, { ...init, credentials: 'same-origin' });
    if (!response.ok) throw new Error('Unable to read availability.');
    const result: unknown = await response.json();
    return unavailablePartsOf(result);
}

interface UnavailablePartWire {
    name: string;
    reason?: string;
}

interface AvailabilitySnapshot {
    isSuccess: boolean;
    data: { unavailable: UnavailablePartWire[] };
}

function unavailablePartsOf(result: unknown): UnavailableParts {
    if (!validSnapshot(result)) throw new Error('Invalid availability.');
    return new Map(result.data.unavailable.map(part => [part.name, reasonOf(part.reason) ?? 'NotAvailable']));
}

function validSnapshot(result: unknown): result is AvailabilitySnapshot {
    const snapshot = result as Partial<AvailabilitySnapshot> | null;
    if (snapshot?.isSuccess !== true) return false;
    const unavailable: unknown = snapshot.data?.unavailable;
    return Array.isArray(unavailable) && unavailable.every(part => typeof part?.name === 'string');
}
