// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, describe, expect, it, vi } from 'vitest';
import { Globals } from '@cratis/arc';
import { QueryFor, ObservableQueryFor, QueryHttpMethod } from '@cratis/arc/queries';
import { clearBindings } from '@cratis/scene.components';
import { queryResult } from '../../../Runtime/src/testing/queries';
import { performQuery, registeredQuery, registerQueryRouteResolver } from '../../../Runtime/src/queryExecutor';
import { registerStageQueries } from '../queryRegistration';

afterEach(() => { clearBindings(); registerQueryRouteResolver(undefined); vi.unstubAllGlobals(); });

describe('when registering live host queries', () => {
    it('registers snapshot and observable proxies by modeled name', () => {
        registerStageQueries({ commands: {}, queries: { Projects: '/projects', LiveProjects: '/projects/live?waitForFirstResult=true' } });
        expect(registeredQuery('Projects')?.prototype).toBeInstanceOf(QueryFor);
        expect(registeredQuery('LiveProjects')?.prototype).toBeInstanceOf(ObservableQueryFor);
        const live = registeredQuery('LiveProjects')!;
        expect(new live().queryName).toBeUndefined();
    });
    it('caches native proxies for attached routes missing from the named query map', () => {
        registerStageQueries({ commands: {}, queries: { AllInvoices: '/sales/invoices/all' } });
        const query = registeredQuery('Invoice', '/sales/invoices/all');
        expect(query?.prototype).toBeInstanceOf(QueryFor);
        expect(query).toBe(registeredQuery('Invoice', '/sales/invoices/all'));
        expect(query).toBe(registeredQuery('AllInvoices'));
        expect(registeredQuery('UnkeyedCollection', '/sales/invoices/live?waitForFirstResult=true')?.prototype).toBeInstanceOf(ObservableQueryFor);
        expect(registeredQuery('Missing')).toBeUndefined();
    });
    it('accepts routes without queries', () => {
        expect(() => registerStageQueries({ commands: {} })).not.toThrow();
    });
    it('sends non-route arguments and identity headers through Arc perform', async () => {
        const fetch = vi.fn().mockResolvedValue({ json: async () => queryResult() });
        vi.stubGlobal('fetch', fetch);
        registerStageQueries({ commands: {}, queries: { Project: '/projects/{id}' } });
        const query = registeredQuery('Project');
        expect(query).toBeDefined();
        const method = Globals.queryHttpMethod;
        Globals.queryHttpMethod = QueryHttpMethod.Get;
        try {
            await performQuery(query!, { id: 'one', filter: 'active' }, { microservice: 'catalog', origin: 'https://backend.example', apiBasePath: '/api', httpHeadersCallback: () => ({ Authorization: 'Bearer identity' }) });
            const [url, init] = fetch.mock.calls[0];
            expect(String(url)).toBe('https://backend.example/api/projects/one?filter=active');
            expect(init.headers.Authorization).toBe('Bearer identity');
            expect(init.headers[Globals.microserviceHttpHeader]).toBe('catalog');
        } finally { Globals.queryHttpMethod = method; }
    });
});
