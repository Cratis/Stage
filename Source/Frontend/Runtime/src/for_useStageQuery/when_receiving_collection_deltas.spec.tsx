// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, describe, expect, it, vi } from 'vitest';
import { act, renderHook, waitFor } from '@testing-library/react';
import type React from 'react';
import { ArcContext } from '@cratis/arc.react';
import { Globals, ObservableQueryTransferMode } from '@cratis/arc';
import { ObservableQueryFor, QueryTransportMethod } from '@cratis/arc/queries';
import { registerQuery } from '@cratis/scene.components';
import { useStageQuery } from '../stageData';
import { queryResult } from '../testing/queries';

class Collection extends ObservableQueryFor<object[]> {
    readonly route = '/items';
    readonly queryName = 'Tests.Items';
    readonly defaultValue = [];
    readonly requiredRequestParameters = [];
    readonly parameterDescriptors = [];
    constructor() { super(Object, true); }
}

const originalFactory = Globals.eventSourceFactory;
const originalTransport = Globals.queryTransportMethod;
afterEach(() => {
    Globals.eventSourceFactory = originalFactory;
    Globals.queryTransportMethod = originalTransport;
    vi.unstubAllGlobals();
});

describe('when receiving collection deltas through the default Arc hub', () => {
    it('retains unchanged rows, applies add/replace/remove and accepts a subsequent full snapshot', async () => {
        expect(Globals.queryDirectMode).toBe(false);
        expect(Globals.observableQueryTransferMode).toBe(ObservableQueryTransferMode.Delta);
        const source = { onopen: null, onmessage: null, onerror: null, close: vi.fn(), readyState: 1 } as unknown as EventSource;
        Globals.eventSourceFactory = vi.fn(() => source);
        Globals.queryTransportMethod = QueryTransportMethod.ServerSentEvents;
        vi.stubGlobal('EventSource', { OPEN: 1, CLOSED: 2 });
        const fetch = vi.fn().mockResolvedValue({ ok: true });
        vi.stubGlobal('fetch', fetch);
        registerQuery('Items', Collection);
        const wrapper = ({ children }: { children: React.ReactNode }) => <ArcContext.Provider value={{
            microservice: '', apiBasePath: '/api', origin: 'http://localhost:8080',
        }}>{children}</ArcContext.Provider>;
        const { result, unmount } = renderHook(() => useStageQuery({ scope: 'items', name: 'Items', route: '/items' }), { wrapper });
        await waitFor(() => expect(source.onmessage).toBeTypeOf('function'));
        act(() => source.onmessage?.({ data: JSON.stringify({ type: 'Connected', payload: 'collection-deltas', keepAliveIntervalMs: 0 }) } as MessageEvent));
        await waitFor(() => expect(fetch).toHaveBeenCalled());
        const subscription = JSON.parse(fetch.mock.calls[0][1].body);
        expect(subscription.request.transferMode).toBe('delta');
        const emit = (data: unknown[], changeSet?: object) => act(() => source.onmessage?.({ data: JSON.stringify({
            type: 'QueryResult', queryId: subscription.queryId, payload: queryResult({ data, ...(changeSet ? { changeSet } : {}) }),
        }) } as MessageEvent));
        emit([{ id: 'keep', name: 'Retained' }, { id: 'replace', name: 'Before' }, { id: 'remove', name: 'Removed' }]);
        await waitFor(() => expect(result.current.rows).toHaveLength(3));
        emit([], { added: [{ id: 'add', name: 'Added' }], replaced: [{ id: 'replace', name: 'After' }], removed: [{ id: 'remove' }] });
        await waitFor(() => expect(result.current.rows.map(row => row.name)).toEqual(['Retained', 'After', 'Added']));
        emit([{ id: 'snapshot', name: 'Full snapshot' }], { added: [], replaced: [], removed: [] });
        await waitFor(() => expect(result.current.rows).toEqual([{ id: 'snapshot', name: 'Full snapshot' }]));
        unmount();
        await waitFor(() => expect(source.close).toHaveBeenCalledTimes(1));
    });
});
