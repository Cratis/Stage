// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { createBrowserDispatcher, SceneElementView } from '@cratis/scene.react';
import { PrimeReactProvider } from '@primereact/core';
import { stageApplicationRegistry } from './App';
import { StageDataProvider, useStageData } from './stageData';
import { bindDataSources } from './stageDataSources';
import { element } from './testElements';
import { commandResult, registerTestCommand } from './testing/commands';
import { queryResult, registerTestQueries } from './testing/queries';

vi.mock('@cratis/scene.react', async importOriginal => {
    const scene = await importOriginal<typeof import('@cratis/scene.react')>();
    return { ...scene, createBrowserDispatcher: vi.fn(scene.createBrowserDispatcher) };
});

const tree = bindDataSources(element('screen', 'core:section', {}, { content: [
    element('subject', 'core:data', { typeName: 'Invoice', query: 'GetInvoice', isCollection: false }),
    element('process', 'core:guarded-action', {
        label: 'Process',
        alternatives: [{ command: 'SendInvoice', condition: { kind: 'comparison', left: { subject: 'item.status' }, operator: 'Equal', right: 'draft' }, arguments: { invoiceId: { subject: 'item.id' } } }],
        otherwise: { outcome: 'Execute', command: 'ReopenInvoice', arguments: { invoiceId: { subject: 'item.id' } } },
    }),
] }));
const routes = { queries: { GetInvoice: '/api/invoice' }, commands: { SendInvoice: '/api/send', ReopenInvoice: '/api/reopen' } };

function ThroughScene() {
    const data = useStageData();
    return <SceneElementView element={tree} registry={stageApplicationRegistry()} resolveBinding={data.resolveBinding} />;
}

describe('a guarded action rendered through Scene and Stage', () => {
    afterEach(() => { vi.unstubAllGlobals(); vi.restoreAllMocks(); });

    it('should preserve executor failure messages in the fallback dispatcher outcome', async () => {
        const createDispatcher = vi.mocked(createBrowserDispatcher);
        registerTestQueries(routes.queries);
        vi.stubGlobal('fetch', vi.fn(() => Promise.resolve({ ok: true,
            json: async () => queryResult({ data: { id: 'invoice-1', status: 'draft' } }),
        } as Response)));
        render(<PrimeReactProvider><StageDataProvider routes={routes} locale='en' locales={['en']} screen='Invoice'><ThroughScene /></StageDataProvider></PrimeReactProvider>);
        const executeCommand = createDispatcher.mock.calls.at(-1)![0]!.executeCommand!;

        const outcome = await executeCommand('UnregisteredGuardedCommand', { invoiceId: 'invoice-1' });

        expect(outcome).toEqual({
            isSuccess: false,
            response: undefined,
            validationErrors: [],
            exceptionMessages: ['The modeled command “UnregisteredGuardedCommand” is not registered by this Stage.'],
        });
    });

    it.each([['draft', '/api/send'], ['sent', '/api/reopen']])('executes exactly the selected command for %s', async (status, commandRoute) => {
        registerTestQueries(routes.queries);
        for (const [name, route] of Object.entries(routes.commands)) registerTestCommand(name, route, { invoiceId: { type: 'string' } });
        const fetched = vi.fn((_input: RequestInfo | URL, init?: RequestInit) => Promise.resolve({ ok: true,
            json: async () => init?.method === 'POST' ? commandResult() : queryResult({ data: { id: 'invoice-1', status } }),
        } as Response));
        vi.stubGlobal('fetch', fetched);
        render(<PrimeReactProvider><StageDataProvider routes={routes} locale='en' locales={['en']} screen='Invoice'><ThroughScene /></StageDataProvider></PrimeReactProvider>);

        fireEvent.click(await screen.findByRole('button', { name: 'Process' }));

        await waitFor(() => expect(fetched.mock.calls.filter(([, init]) => init?.method === 'POST')).toHaveLength(1));
        const [url, request] = fetched.mock.calls.find(([, init]) => init?.method === 'POST')!;
        expect(String(url)).toBe(`http://localhost:3000${commandRoute}`);
        expect(request?.body).toBe(JSON.stringify({ invoiceId: 'invoice-1' }));
    });
});
