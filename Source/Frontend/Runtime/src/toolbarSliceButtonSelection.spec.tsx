// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { registerStageCommands } from '../../src/host/commandRegistration';
import { stageComponents } from './stageComponents';
import { StageDataProvider } from './stageData';
import { element } from './testElements';
import { RegisteredTable, registerTestQueries, queryResult } from './testing/queries';
import { commandResult } from './testing/commands';

const StageToolbarItem = stageComponents['core:toolbarItem'];
const StageSliceDialog = stageComponents['core:sliceDialog'];
const approve = JSON.stringify({
    type: 'object',
    required: ['productId'],
    properties: {
        productId: { type: 'string', format: 'uuid', 'x-concept': 'ProductId', 'x-identity': true },
    },
});
const routes = { commands: { ApproveProductForSale: '/api/approve' }, queries: { ReadyForApproval: '/api/ready' }, commandSchemas: { ApproveProductForSale: approve } };
const rows = [{ productId: '5c1e8a10-0000-4000-8000-000000000001', name: 'Acme mug' }, { productId: '5c1e8a10-0000-4000-8000-000000000002', name: 'Acme lamp' }];

// The toolbar button is first on the screen; the dialog beside it carries the same title.
const toolbarButton = () => screen.getAllByRole('button', { name: 'Approve product' })[0] as HTMLButtonElement;

function renderScreen() {
    registerStageCommands(routes);
    registerTestQueries(routes.queries);
    const fetched = vi.fn((_: RequestInfo | URL, init?: RequestInit) => init?.method === 'POST'
        ? Promise.resolve({ ok: true, json: async () => commandResult() })
        : Promise.resolve({ ok: true, json: async () => queryResult({ data: rows }) }));
    vi.stubGlobal('fetch', fetched);
    globalThis.location.hash = '#/Publication';
    const table = element('ready', 'core:table', { query: 'ReadyForApproval', typeName: 'ProductRow' }, { columns: [element('name', 'core:column', { property: 'name', label: 'Name' })] });
    const button = element('approve', 'core:toolbarItem', { name: 'approve', kind: 'Button', label: 'Approve product', slice: 'Merchandising.Publication.ApproveProductForSale' });
    render(
        <PrimeReactProvider>
            <StageDataProvider routes={routes} locale='en' locales={['en']} screen='Publication'>
                <StageToolbarItem element={button} slots={{}} />
                <RegisteredTable element={table} slots={{}} />
                <StageSliceDialog element={element('dialog', 'core:sliceDialog', { slice: 'Merchandising.Publication.ApproveProductForSale', command: 'ApproveProductForSale', title: 'Approve product' })} slots={{}} />
            </StageDataProvider>
        </PrimeReactProvider>,
    );
    return fetched;
}

describe('a feature toolbar button on a list whose rows carry the key its command acts on', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); });

    it('is visible but disabled while no row is selected', async () => {
        renderScreen();

        await screen.findByText('Acme mug');
        await waitFor(() => expect(toolbarButton().disabled).toBe(true));
        expect(toolbarButton().title).toBe('Select a row first');
    });

    it('is enabled once a row is selected, and its dialog does not ask for the key and submits the selected row\'s key', async () => {
        const fetched = renderScreen();

        fireEvent.click(await screen.findByText('Acme lamp'));
        await waitFor(() => expect(toolbarButton().disabled).toBe(false));

        const dialog = document.querySelector('.scene-slice-dialog') as HTMLElement;
        expect(within(dialog).queryByLabelText('Product Id', { exact: false })).toBeNull();
        await act(async () => { fireEvent.click(within(dialog).getByRole('button', { name: /execute/i })); });

        await waitFor(() => expect(fetched.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(true));
        const [, init] = fetched.mock.calls.find(([, request]) => request?.method === 'POST')!;
        expect(JSON.parse(String(init?.body))).toEqual({ productId: rows[1].productId });
    });
});
