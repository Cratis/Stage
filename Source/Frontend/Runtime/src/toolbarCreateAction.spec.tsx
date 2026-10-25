// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { registerStageCommands } from '../../src/host/commandRegistration';
import { stageComponents } from './stageComponents';
import { StageDataProvider } from './stageData';
import { element } from './testElements';
import { RegisteredTable, registerTestQueries, queryResult } from './testing/queries';
import { commandResult } from './testing/commands';
import { render } from './testing/render';

const StageToolbarItem = stageComponents['core:toolbarItem'];

// The host marks commands whose entity identity is allocated by the runtime. Rows on this screen
// happen to carry a related identifier that the person must still supply for a new shipment.
const planShipment = JSON.stringify({
    type: 'object', 'x-creates': true,
    required: ['orderId', 'carrier'],
    properties: {
        orderId: { type: 'string', format: 'uuid', 'x-concept': 'OrderId' },
        carrier: { type: 'string' },
    },
});
const routes = { commands: { PlanShipment: '/api/plan' }, queries: { AllShipments: '/api/shipments' }, commandSchemas: { PlanShipment: planShipment } };
const rows = [{ orderId: '7d1c8f3a-0000-4000-8000-000000000001', consignee: 'Fjordline' }, { orderId: '7d1c8f3a-0000-4000-8000-000000000002', consignee: 'Norsk Fisk' }];
const typedOrder = '9a9a9a9a-0000-4000-8000-000000000009';

async function renderShipments() {
    registerStageCommands(routes);
    registerTestQueries(routes.queries);
    const fetched = vi.fn((_: RequestInfo | URL, init?: RequestInit) => init?.method === 'POST'
        ? Promise.resolve({ ok: true, json: async () => commandResult() })
        : Promise.resolve({ ok: true, json: async () => queryResult({ data: rows }) }));
    vi.stubGlobal('fetch', fetched);
    globalThis.location.hash = '#/Shipments';
    const table = element('shipments', 'core:table', { query: 'AllShipments', typeName: 'ShipmentRow' }, { columns: [element('consignee', 'core:column', { property: 'consignee', label: 'Consignee' })] });
    const item = element('plan', 'core:toolbarItem', { name: 'plan', kind: 'Action', label: 'Add shipment', target: 'PlanShipment' });
    await act(async () => {
        render(<StageDataProvider routes={routes} locale='en' locales={['en']} screen='Shipments'>
            <StageToolbarItem element={item} slots={{}} />
            <RegisteredTable element={table} slots={{}} />
        </StageDataProvider>);
    });
    await screen.findByText('Fjordline');
    return fetched;
}

async function submit(fetched: Awaited<ReturnType<typeof renderShipments>>) {
    fireEvent.click(screen.getByRole('button', { name: 'Add shipment' }));
    const dialog = await screen.findByRole('dialog');
    fireEvent.change(within(dialog).getByLabelText('Order Id', { exact: false }), { target: { value: typedOrder } });
    fireEvent.change(within(dialog).getByLabelText('Carrier', { exact: false }), { target: { value: 'Posten' } });
    await act(async () => { fireEvent.click(within(dialog).getByRole('button', { name: 'Add shipment' })); });
    await waitFor(() => expect(fetched.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(true));
    const [, init] = fetched.mock.calls.find(([, request]) => request?.method === 'POST')!;
    return JSON.parse(String(init?.body)) as Record<string, unknown>;
}

describe('a toolbar action that creates an entity beside a list carrying a related identifier', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); });

    it('is enabled without selecting a row', async () => {
        await renderShipments();
        expect((screen.getByRole('button', { name: 'Add shipment' }) as HTMLButtonElement).disabled).toBe(false);
    });

    it('submits the entered identifier rather than the selected row key', async () => {
        const fetched = await renderShipments();
        fireEvent.click(screen.getByText('Norsk Fisk'));
        expect(await submit(fetched)).toEqual({ orderId: typedOrder, carrier: 'Posten' });
    });
});
