// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { Globals } from '@cratis/arc';
import { ObservableQueryFor, QueryFor, QueryTransportMethod } from '@cratis/arc/queries';
import { performQuery, subscribeQuery } from '../queryExecutor';

class Products extends QueryFor<unknown> {
    readonly route: string = '/api/merchandising/products/all-products';
    constructor() { super(Object, true); }
    readonly defaultValue = [];
    readonly requiredRequestParameters = [];
    readonly parameterDescriptors = [];
}
class LiveProducts extends ObservableQueryFor<unknown> {
    readonly route = '/api/merchandising/products/all-products';
    readonly queryName = 'AllProducts';
    constructor() { super(Object, true); }
    readonly defaultValue = [];
    readonly requiredRequestParameters = [];
    readonly parameterDescriptors = [];
}

const prefix = '/api/play/17695861-60d0-4791-9e45-8b87c4c7aed4';

// The live host mounts no Arc provider, so queries see Arc's default configuration and its empty base path.
const defaultArc = { microservice: '', apiBasePath: '' };

describe('when querying with the default Arc configuration in a page served under a path prefix', () => {
    beforeEach(() => globalThis.history.replaceState(null, '', `${prefix}/#/PricingBoard`));
    afterEach(() => {
        vi.unstubAllGlobals();
        vi.restoreAllMocks();
        globalThis.history.replaceState(null, '', '/');
    });

    it('should perform a snapshot under the prefix the page is served from', async () => {
        const urls: string[] = [];
        vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => {
            urls.push(String(input));
            return new Response(JSON.stringify({ data: [], isSuccess: true, isAuthorized: true, isValid: true, hasExceptions: false, validationResults: [], exceptionMessages: [], exceptionStackTrace: '', paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 } }));
        }));

        await performQuery(Products, {}, defaultArc);

        expect(urls).toHaveLength(1);
        expect(new URL(urls[0], 'http://localhost').pathname).toBe(`${prefix}/api/merchandising/products/all-products`);
    });

    it('should still prefer a configured base path', async () => {
        const basePath = vi.spyOn(Products.prototype, 'setApiBasePath');
        vi.spyOn(Products.prototype, 'perform').mockResolvedValue(undefined as never);

        await performQuery(Products, {}, { microservice: '', apiBasePath: '/tenant-api' });

        expect(basePath).toHaveBeenCalledWith('/tenant-api');
    });

    it('should open the live query hub under the prefix the page is served from', () => {
        const opened: string[] = [];
        class EventSourceFixture {
            static readonly CLOSED = 2;
            static readonly OPEN = 1;
            readonly readyState = 0;
            onopen = null;
            onmessage = null;
            onerror = null;
            constructor(url: string) { opened.push(url); }
            close = vi.fn();
        }
        const transport = Globals.queryTransportMethod;
        const direct = Globals.queryDirectMode;
        const factory = Globals.eventSourceFactory;
        Globals.queryTransportMethod = QueryTransportMethod.ServerSentEvents;
        Globals.queryDirectMode = false;
        Globals.eventSourceFactory = undefined;
        vi.stubGlobal('EventSource', EventSourceFixture);
        try {
            const subscription = subscribeQuery(LiveProducts, {}, defaultArc, vi.fn());
            expect(opened).toHaveLength(1);
            expect(new URL(opened[0], 'http://localhost').pathname).toBe(`${prefix}/.cratis/queries/sse`);
            subscription?.unsubscribe();
        } finally {
            Globals.queryTransportMethod = transport;
            Globals.queryDirectMode = direct;
            Globals.eventSourceFactory = factory;
        }
    });
});
