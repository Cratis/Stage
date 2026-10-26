// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { SceneElementView } from '@cratis/scene.react';
import { PrimeReactProvider } from '@primereact/core';
import { stageApplicationRegistry } from './App';
import { StageDataProvider, useStageData } from './stageData';
import { bindDataSources } from './stageDataSources';
import { element } from './testElements';
import { commandResult, registerTestCommand } from './testing/commands';
import { queryResult, registerTestQueries } from './testing/queries';

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

    it.each([['draft', '/api/send'], ['sent', '/api/reopen']])('executes exactly the selected command for %s', async (status, commandRoute) => {
        registerTestQueries(routes.queries);
        for (const [name, route] of Object.entries(routes.commands)) registerTestCommand(name, route, { invoiceId: { type: 'string' } });
        const fetched = vi.fn((_input: RequestInfo | URL, init?: RequestInit) => Promise.resolve({ ok: true,
            json: async () => init?.method === 'POST' ? commandResult() : queryResult({ data: { id: 'invoice-1', status } }),
        } as Response));
        vi.stubGlobal('fetch', fetched);
        render(<PrimeReactProvider><StageDataProvider routes={routes}><ThroughScene /></StageDataProvider></PrimeReactProvider>);

        fireEvent.click(await screen.findByRole('button', { name: 'Process' }));

        await waitFor(() => expect(fetched.mock.calls.filter(([, init]) => init?.method === 'POST')).toHaveLength(1));
        const [url, request] = fetched.mock.calls.find(([, init]) => init?.method === 'POST')!;
        expect(String(url)).toBe(`http://localhost:3000${commandRoute}`);
        expect(request?.body).toBe(JSON.stringify({ invoiceId: 'invoice-1' }));
    });
});
