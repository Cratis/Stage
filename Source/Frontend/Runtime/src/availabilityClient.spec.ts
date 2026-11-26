// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, describe, expect, it, vi } from 'vitest';
import { availabilityOf, fetchUnavailableParts, reasonOf } from './availabilityClient';

const respond = (payload: unknown, ok = true) => Promise.resolve({ ok, json: async () => payload } as Response);
const snapshot = (unavailable: unknown[]) => ({ isSuccess: true, data: { unavailable } });

function stubFetch(payload: unknown, ok = true) {
    const fetcher = vi.fn((_input: RequestInfo | URL) => respond(payload, ok));
    vi.stubGlobal('fetch', fetcher);
    return () => new URL(String(fetcher.mock.calls[0][0]), document.baseURI);
}

describe('availability client', () => {
    afterEach(() => vi.unstubAllGlobals());

    it('reads a part that is not available as NotAvailable', async () => {
        stubFetch(snapshot([{ name: 'Part', module: '', feature: '', slice: '', reason: 'NotAvailable' }]));
        const parts = await fetchUnavailableParts(new AbortController().signal);
        expect(parts.get('Part')).toBe('NotAvailable');
    });

    it('reads a part an operator suspended as Suspended, not as NotAvailable', async () => {
        stubFetch(snapshot([{ name: 'Part', module: '', feature: '', slice: '', reason: 'Suspended' }]));
        const parts = await fetchUnavailableParts(new AbortController().signal);
        expect(parts.get('Part')).toBe('Suspended');
    });

    it('reads a refusal that names no reason as NotAvailable, as a server that predates suspension reports it', async () => {
        stubFetch(snapshot([{ name: 'Part', module: '', feature: '', slice: '' }]));
        const parts = await fetchUnavailableParts(new AbortController().signal);
        expect(parts.get('Part')).toBe('NotAvailable');
    });

    it('asks about every part when no part is given', async () => {
        const url = stubFetch(snapshot([]));
        await fetchUnavailableParts(new AbortController().signal);
        expect(url().searchParams.has('part')).toBe(false);
        expect(url().searchParams.has('arguments')).toBe(false);
    });

    it('asks about one part and the data it is about', async () => {
        const url = stubFetch(snapshot([]));
        await fetchUnavailableParts(new AbortController().signal, undefined, { part: 'Fleet.Vessels.Dock.DockVessel', arguments: { vesselId: 'v-17' } });
        expect(url().pathname).toBe('/.cratis/availability');
        expect(url().searchParams.get('part')).toBe('Fleet.Vessels.Dock.DockVessel');
        expect(JSON.parse(url().searchParams.get('arguments') ?? '{}')).toEqual({ vesselId: 'v-17' });
    });

    it('sends no arguments when the part is about no data', async () => {
        const url = stubFetch(snapshot([]));
        await fetchUnavailableParts(new AbortController().signal, undefined, { part: 'Part', arguments: {} });
        expect(url().searchParams.has('arguments')).toBe(false);
    });

    it('refuses an answer that is not a snapshot, so a failed decision never opens a part', async () => {
        stubFetch({ isSuccess: false });
        await expect(fetchUnavailableParts(new AbortController().signal)).rejects.toThrow('Invalid availability.');
    });

    it('refuses a failed request', async () => {
        stubFetch({}, false);
        await expect(fetchUnavailableParts(new AbortController().signal)).rejects.toThrow('Unable to read availability.');
    });

    it('tells NotAvailable from Suspended and from a reason it does not know', () => {
        expect(reasonOf('NotAvailable')).toBe('NotAvailable');
        expect(reasonOf('Suspended')).toBe('Suspended');
        expect(reasonOf('Forbidden')).toBeUndefined();
        expect(reasonOf(undefined)).toBeUndefined();
    });

    it('turns a refusal into what the renderer does', () => {
        expect(availabilityOf(undefined)).toEqual({ kind: 'available' });
        expect(availabilityOf('NotAvailable')).toEqual({ kind: 'notAvailable' });
        expect(availabilityOf('Suspended')).toEqual({ kind: 'suspended' });
    });
});
