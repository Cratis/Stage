// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { Globals } from '@cratis/arc';
import { QueryTransportMethod, ObservableQueryFor, ObservableQuerySubscription, Paging, QueryFor, QueryResult } from '@cratis/arc/queries';
import { clearBindings, registerQuery, registerQueryIdentity } from '@cratis/scene.components';
import { registeredQuery, performQuery, subscribeQuery } from '../queryExecutor';

class Snapshot extends QueryFor<unknown> {
    readonly route: string = '/projects/{id}';
    constructor() { super(Object, false); }
    readonly defaultValue = {};
    readonly requiredRequestParameters = ['id'];
    readonly parameterDescriptors = [];
}
class OtherSnapshot extends QueryFor<unknown> {
    readonly route = '/other/{id}';
    constructor() { super(Object, false); }
    readonly defaultValue = {};
    readonly requiredRequestParameters = ['id'];
    readonly parameterDescriptors = [];
}
class Live extends ObservableQueryFor<unknown> {
    readonly route = '/projects';
    readonly queryName = 'Projects';
    constructor() { super(Object, true); }
    readonly defaultValue = [];
    readonly requiredRequestParameters = [];
    readonly parameterDescriptors = [];
}
const arc = { microservice: 'catalog', apiBasePath: '/api', origin: 'https://backend.example', httpHeadersCallback: () => ({ Authorization: 'Bearer identity' }) };
const result = QueryResult.empty({ name: 'One' });

beforeEach(() => clearBindings());
afterEach(() => { clearBindings(); vi.restoreAllMocks(); });

describe('when resolving a modeled query', () => {
    it('requires the modeled name even when a different name has the route', () => {
        registerQuery('Different', Snapshot);
        expect(registeredQuery('Projects', '/projects/{id}')).toBeUndefined();
    });
    it('accepts a native snapshot or observable proxy', () => {
        registerQuery('Project', Snapshot);
        registerQuery('Projects', Live);
        expect(registeredQuery('Project')).toBe(Snapshot);
        expect(registeredQuery('Projects')).toBe(Live);
    });
    it('does not let a stale route override a unique modeled name', () => {
        registerQuery('Project', Snapshot);
        expect(registeredQuery('Project', '/old-route')).toBe(Snapshot);
    });
    it('rejects an unrelated binding', () => {
        registerQuery('Project', class {});
        expect(registeredQuery('Project')).toBeUndefined();
    });
    it('uses a name-and-route alias only to disambiguate the same modeled name', () => {
        registerQueryIdentity('Project', 'a', Snapshot);
        registerQueryIdentity('Project', 'b', OtherSnapshot);
        registerQueryIdentity('Project:projects/{id}', 'a', Snapshot);
        registerQueryIdentity('Project:other/{id}', 'b', OtherSnapshot);
        expect(registeredQuery('Project')).toBeUndefined();
        expect(registeredQuery('Project', '/projects/{id}')).toBe(Snapshot);
        expect(registeredQuery('Project', '/other/{id}')).toBe(OtherSnapshot);
        expect(registeredQuery('Project', '/missing')).toBeUndefined();
    });
});

describe('when performing a snapshot', () => {
    it('configures Arc and passes route and non-route arguments through perform', async () => {
        const perform = vi.spyOn(Snapshot.prototype, 'perform').mockResolvedValue(result);
        const microservice = vi.spyOn(Snapshot.prototype, 'setMicroservice');
        const basePath = vi.spyOn(Snapshot.prototype, 'setApiBasePath');
        const origin = vi.spyOn(Snapshot.prototype, 'setOrigin');
        const headers = vi.spyOn(Snapshot.prototype, 'setHttpHeadersCallback');
        expect(await performQuery(Snapshot, { id: 'one', filter: 'active' }, arc)).toBe(result);
        expect(perform).toHaveBeenCalledWith({ id: 'one', filter: 'active' });
        expect(microservice).toHaveBeenCalledWith('catalog');
        expect(basePath).toHaveBeenCalledWith('/api');
        expect(origin).toHaveBeenCalledWith('https://backend.example');
        expect(headers.mock.calls[0][0]()).toEqual({ Authorization: 'Bearer identity' });
        expect(perform.mock.contexts[0]).toHaveProperty('paging', Paging.noPaging);
    });
});

describe('when subscribing to an observable query', () => {
    it('configures Arc, delivers typed results and returns Arc cleanup', () => {
        const unsubscribe = vi.fn();
        const callback = vi.fn();
        const subscribe = vi.spyOn(Live.prototype, 'subscribe').mockImplementation(deliver => {
            deliver(result);
            return new ObservableQuerySubscription({ connect: vi.fn(), disconnect: unsubscribe, lastPingLatency: 0, averageLatency: 0 });
        });
        const basePath = vi.spyOn(Live.prototype, 'setApiBasePath');
        const transport = vi.spyOn(Live.prototype, 'setMicroservice');
        const subscription = subscribeQuery(Live, { filter: 'active' }, arc, callback);
        expect(subscribe).toHaveBeenCalledWith(callback, { filter: 'active' });
        expect(basePath).toHaveBeenCalledWith('/api');
        expect(transport).toHaveBeenCalledWith('catalog');
        expect(callback).toHaveBeenCalledWith(result);
        subscription?.unsubscribe();
        expect(unsubscribe).toHaveBeenCalledOnce();
    });
    it('does not subscribe a snapshot proxy', () => {
        expect(subscribeQuery(Snapshot, {}, arc, vi.fn())).toBeUndefined();
    });
});

it("uses Arc's configured SSE hub transport beneath the host API base path", () => {
    const opened: string[] = [];
    const closed = vi.fn();
    class EventSourceFixture {
        static readonly CLOSED = 2;
        static readonly OPEN = 1;
        readonly readyState = 0;
        onopen = null;
        onmessage = null;
        onerror = null;
        constructor(url: string) { opened.push(url); }
        close = closed;
    }
    const transport = Globals.queryTransportMethod;
    const direct = Globals.queryDirectMode;
    const factory = Globals.eventSourceFactory;
    Globals.queryTransportMethod = QueryTransportMethod.ServerSentEvents;
    Globals.queryDirectMode = false;
    Globals.eventSourceFactory = undefined;
    vi.stubGlobal('EventSource', EventSourceFixture);
    try {
        const subscription = subscribeQuery(Live, {}, arc, vi.fn());
        expect(opened).toHaveLength(1);
        expect(opened[0]).toContain('https://backend.example/api/.cratis/queries/sse');
        expect(new URL(opened[0]).searchParams.get(Globals.microserviceWSQueryArgument)).toBe('catalog');
        subscription?.unsubscribe();
        expect(closed).toHaveBeenCalledOnce();
    } finally {
        Globals.queryTransportMethod = transport;
        Globals.queryDirectMode = direct;
        Globals.eventSourceFactory = factory;
        vi.unstubAllGlobals();
    }
});
