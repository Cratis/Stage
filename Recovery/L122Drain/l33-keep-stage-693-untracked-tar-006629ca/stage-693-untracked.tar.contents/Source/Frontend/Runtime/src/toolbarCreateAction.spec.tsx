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

// The host marks a command whose identity the runtime allocates - a generated identifier, or none - as one that creates (StageRoutes, SchemaSynthesizer.Creating).
// Its identifier is generated, so the schema carries only what the person gives: the order the new shipment is for, which every row of the list also carries.
const planShipment = JSON.stringify({
    type: 'object',
    'x-creates': true,
    required: ['orderId', 'carrier'],
    properties: {
        orderId: { type: 'string', format: 'uuid', 'x-concept': 'OrderId' },
        carrier: { type: 'string' },
    },
});
const routes = { commands: { PlanShipment: '/api/plan' }, queries: { AllShipments: '/api/shipments' }, commandSchemas: { PlanShipment: planShipment } };
const rows = [{ orderId: '7d1c8f3a-0000-4000-8000-000000000001', consignee: 'Fjordline' }, { orderId: '7d1c8f3a-0000-4000-8000-000000000002', consignee: 'Norsk Fisk' }];
const typedOrder = '9a9a9a9a-0000-4000-8000-000000000009';

function renderShipments() {
    registerStageCommands(routes);
    registerTestQueries(routes.queries);
    const fetched = vi.fn((_: RequestInfo | URL, init?: RequestInit) => init?.method === 'POST'
        ? Promise.resolve({ ok: true, json: async () => commandResult() })
        : Promise.resolve({ ok: true, json: async () => queryResult({ data: rows }) }));
    vi.stubGlobal('fetch', fetched);
    globalThis.location.hash = '#/Shipments';
    const table = element('shipments', 'core:table', { query: 'AllShipments', typeName: 'ShipmentRow' }, { columns: [element('consignee', 'core:column', { property: 'consignee', label: 'Consignee' })] });
    const item = element('plan', 'core:toolbarItem', { name: 'plan', kind: 'Action', label: 'Add shipment', target: 'PlanShipment' });
    render(
        <PrimeReactProvider>
            <StageDataProvider routes={routes} locale='en' locales={['en']} screen='Shipments'>
                <StageToolbarItem element={item} slots={{}} />
                <RegisteredTable element={table} slots={{}} />
            </StageDataProvider>
        </PrimeReactProvider>,
    );
    return fetched;
}

async function submit(fetched: ReturnType<typeof renderShipments>) {
    fireEvent.click(screen.getByRole('button', { name: 'Add shipment' }));
    const dialog = await screen.findByRole('dialog');
    fireEvent.change(within(dialog).getByLabelText('Order Id', { exact: false }), { target: { value: typedOrder } });
    fireEvent.change(within(dialog).getByLabelText('Carrier', { exact: false }), { target: { value: 'Posten' } });
    await act(async () => { fireEvent.click(within(dialog).getByRole('button', { name: /execute/i })); });
    await waitFor(() => expect(fetched.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(true));
    const [, init] = fetched.mock.calls.find(([, request]) => request?.method === 'POST')!;
    return JSON.parse(String(init?.body)) as Record<string, unknown>;
}

describe('a toolbar action on a list that creates an entity whose key the rows carry', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); });

    it('is enabled while no row is selected', async () => {
        renderShipments();

        await screen.findByText('Fjordline');
        // Give the screen's data time to publish the rows the action could otherwise wait on.
        await act(async () => { await new Promise(resolve => setTimeout(resolve, 50)); });
        expect((screen.getByRole('button', { name: 'Add shipment' }) as HTMLButtonElement).disabled).toBe(false);
    });

    it('submits what the person gave, not the selected row\'s key', async () => {
        const fetched = renderShipments();
        fireEvent.click(await screen.findByText('Norsk Fisk'));

        const body = await submit(fetched);

        expect(body).toEqual({ orderId: typedOrder, carrier: 'Posten' });
    });
});
