// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { registerStageCommands } from '../../src/host/commandRegistration';
import { stageComponents } from './stageComponents';
import { StageDataProvider } from './stageData';
import { element } from './testElements';
import { RegisteredTable, registerTestQueries, queryResult } from './testing/queries';
import { commandResult } from './testing/commands';

const StageOutletItems = stageComponents['core:outletItems'];
const rows = [
    { productId: '5c1e8a10-0000-4000-8000-000000000001', name: 'Acme mug', status: 'draft' },
    { productId: '5c1e8a10-0000-4000-8000-000000000002', name: 'Acme lamp', status: 'approved' },
];
const approve = () => screen.getByRole('button', { name: 'Approve for sale' }) as HTMLButtonElement;

function renderScreen() {
    const routes = { commands: { ApproveProductForSale: '/api/approve' }, queries: { AllProducts: '/api/products' }, commandSchemas: {} };
    registerStageCommands(routes);
    registerTestQueries(routes.queries);
    vi.stubGlobal('fetch', vi.fn((_: RequestInfo | URL, init?: RequestInit) => init?.method === 'POST'
        ? Promise.resolve({ ok: true, json: async () => commandResult() })
        : Promise.resolve({ ok: true, json: async () => queryResult({ data: rows }) })));
    const table = element('products', 'core:table', { query: 'AllProducts', typeName: 'ProductRow' }, { columns: [element('name', 'core:column', { property: 'name', label: 'Name' })] });
    const toolbar = element('toolbar', 'core:outletItems', {
        name: 'actions',
        kind: 'actions',
        items: [{ kind: 'button', id: 'approve', title: 'Approve for sale', slice: 'Merchandising.Publication.ApproveProductForSale', subject: 'item', opens: 'none', enabledWhen: 'item.status == "draft"' }],
    });
    render(
        <PrimeReactProvider>
            <StageDataProvider routes={routes} locale='en' locales={['en']} screen='ProductCatalog'>
                <StageOutletItems element={toolbar} slots={{}} />
                <RegisteredTable element={table} slots={{}} />
            </StageDataProvider>
        </PrimeReactProvider>,
    );
}

describe('a toolbar button that runs its slice for the selected row', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); });

    it('waits for a row to be selected', async () => {
        renderScreen();
        await screen.findByText('Acme mug');
        expect(approve().disabled).toBe(true);
    });

    it('is enabled for a selected row its condition holds for', async () => {
        renderScreen();
        fireEvent.click(await screen.findByText('Acme mug'));
        await waitFor(() => expect(approve().disabled).toBe(false));
    });

    it('stays disabled for a selected row its condition does not hold for', async () => {
        renderScreen();
        fireEvent.click(await screen.findByText('Acme lamp'));
        await waitFor(() => expect(approve().getAttribute('data-scene-enablement')).not.toBeNull());
        expect(approve().disabled).toBe(true);
    });
});
