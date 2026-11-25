// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { renderHook, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { availabilityOfCommand, combineAvailability, elementLookup, useElementGate } from './elementAvailability';

const stageData = vi.hoisted(() => ({
    value: { screen: 'VesselList', routes: { commands: {}, elements: { 'VesselList/action/RegisterVessel': 'StageElement_exact' } } } as Record<string, unknown>
}));
vi.mock('./stageData', () => ({ useStageData: () => stageData.value }));

const answer = (reason?: string) => ({ isSuccess: true, data: { unavailable: reason ? [{ name: 'StageElement_exact', module: '', feature: '', slice: '', reason }] : [] } });

function stubFetch(payload: () => Promise<unknown>) {
    const fetcher = vi.fn((_input: RequestInfo | URL) => payload().then(json => ({ ok: true, json: async () => json } as Response)));
    vi.stubGlobal('fetch', fetcher);
    return fetcher;
}

describe('element availability', () => {
    afterEach(() => vi.unstubAllGlobals());

    it('finds an element by its screen, its kind and the command or query it is bound to', () => {
        expect(elementLookup('VesselList', 'action', 'RegisterVessel')).toBe('VesselList/action/RegisterVessel');
    });

    it('offers an element without a gate and asks nothing', () => {
        const fetcher = stubFetch(async () => answer());
        const { result } = renderHook(() => useElementGate('action', 'DockVessel', {}));
        expect(result.current).toEqual({ kind: 'available' });
        expect(fetcher).not.toHaveBeenCalled();
    });

    it('shows nothing for a gated element until the server has answered', async () => {
        stubFetch(() => new Promise(() => undefined));
        const { result } = renderHook(() => useElementGate('action', 'RegisterVessel', {}));
        expect(result.current).toEqual({ kind: 'unknown' });
    });

    it('asks about the marker with the values the screen shows', async () => {
        const fetcher = stubFetch(async () => answer());
        const { result } = renderHook(() => useElementGate('action', 'RegisterVessel', { vesselId: 'v-17' }));
        await waitFor(() => expect(result.current).toEqual({ kind: 'available' }));
        const url = new URL(String(fetcher.mock.calls[0][0]), document.baseURI);
        expect(url.searchParams.get('part')).toBe('StageElement_exact');
        expect(JSON.parse(url.searchParams.get('arguments') ?? '{}')).toEqual({ vesselId: 'v-17' });
    });

    it('does not render an element whose part is not available', async () => {
        stubFetch(async () => answer('NotAvailable'));
        const { result } = renderHook(() => useElementGate('action', 'RegisterVessel', {}));
        await waitFor(() => expect(result.current).toEqual({ kind: 'notAvailable' }));
    });

    it('tells an element an operator suspended from one that is not available', async () => {
        stubFetch(async () => answer('Suspended'));
        const { result } = renderHook(() => useElementGate('action', 'RegisterVessel', {}));
        await waitFor(() => expect(result.current).toEqual({ kind: 'suspended' }));
    });

    it('keeps an element closed when the answer fails', async () => {
        const fetcher = stubFetch(() => Promise.reject(new Error('down')));
        const { result } = renderHook(() => useElementGate('action', 'RegisterVessel', {}));
        await waitFor(() => expect(fetcher).toHaveBeenCalled());
        expect(result.current).toEqual({ kind: 'unknown' });
    });

    it('carries what the server said about a command into the same kinds', () => {
        expect(availabilityOfCommand({ unavailable: false, kind: 'available' })).toEqual({ kind: 'available' });
        expect(availabilityOfCommand({ unavailable: true, kind: 'notAvailable', reason: 'NotAvailable' })).toEqual({ kind: 'notAvailable' });
        expect(availabilityOfCommand({ unavailable: true, kind: 'suspended', reason: 'Suspended' })).toEqual({ kind: 'suspended' });
        expect(availabilityOfCommand({ unavailable: true, kind: 'unauthorized', reason: 'Needs the Manager role' })).toEqual({ kind: 'unauthorized', reason: 'Needs the Manager role' });
    });

    it('lets a part that is not available outrank every other reason', () => {
        const kinds = combineAvailability({ kind: 'unauthorized' }, { kind: 'suspended' }, { kind: 'notAvailable' }, { kind: 'unknown' }).kind;
        expect(kinds).toBe('notAvailable');
    });

    it('lets a suspended part outrank one still being asked about and one the caller may not use', () => {
        expect(combineAvailability({ kind: 'unauthorized' }, { kind: 'unknown' }, { kind: 'suspended' }).kind).toBe('suspended');
    });

    it('offers an element only when nothing withholds it', () => {
        expect(combineAvailability({ kind: 'available' }, { kind: 'available' })).toEqual({ kind: 'available' });
        expect(combineAvailability({ kind: 'available' }, { kind: 'unauthorized', reason: 'No' })).toEqual({ kind: 'unauthorized', reason: 'No' });
    });
});
