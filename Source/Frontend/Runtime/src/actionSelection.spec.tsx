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
import { sessionIdentity } from './stageSession';

const StageAction = stageComponents['core:action'];
const dispatch = JSON.stringify({
    type: 'object',
    required: ['shipmentId', 'carrier'],
    properties: {
        // The rendered scene carries the command's schema on the action itself, without the host's identity mark.
        shipmentId: { type: 'string', format: 'uuid', 'x-concept': 'ShipmentId' },
        carrier: { type: 'string' },
    },
});
// The host registers the command from the schema it serves; the action reads the one the rendered scene put on it.
const routes = { commands: { DispatchShipment: '/api/dispatch' }, queries: { AllShipments: '/api/shipments' }, commandSchemas: { DispatchShipment: dispatch } };
const rows = [{ shipmentId: '7d1c8f3a-0000-4000-8000-000000000001', consignee: 'Fjordline' }, { shipmentId: '7d1c8f3a-0000-4000-8000-000000000002', consignee: 'Norsk Fisk' }];

function renderShipments(screenRows: Record<string, unknown>[] = rows, fields: { name: string; label: string }[] = [{ name: 'carrier', label: 'Carrier' }]) {
    registerStageCommands(routes);
    registerTestQueries(routes.queries);
    const fetched = vi.fn((_: RequestInfo | URL, init?: RequestInit) => init?.method === 'POST'
        ? Promise.resolve({ ok: true, json: async () => commandResult() })
        : Promise.resolve({ ok: true, json: async () => queryResult({ data: screenRows }) }));
    vi.stubGlobal('fetch', fetched);
    globalThis.location.hash = '#/Shipments';
    const table = element('shipments', 'core:table', { query: 'AllShipments', typeName: 'ShipmentRow' }, { columns: [element('consignee', 'core:column', { property: 'consignee', label: 'Consignee' })] });
    // What a screen's toolbar renders an action as: the command, its schema and the form's own fields, which leave the key out.
    const item = element('dispatch', 'core:action', { command: 'DispatchShipment', label: 'Dispatch', schema: dispatch, fields, generationMode: 'manual' });
    render(
        <PrimeReactProvider>
            <StageDataProvider routes={routes} locale='en' locales={['en']} screen='Shipments'>
                <StageAction element={item} slots={{}} />
                <RegisteredTable element={table} slots={{}} />
            </StageDataProvider>
        </PrimeReactProvider>,
    );
    return fetched;
}

describe('an action on a list screen whose rows carry the key it acts on', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); });

    it('is visible but disabled while no row is selected', async () => {
        renderShipments();

        await screen.findByText('Fjordline');
        // The table shows its rows before the screen's data has published them, so wait for the action to see them.
        await waitFor(() => expect((screen.getByRole('button', { name: 'Dispatch' }) as HTMLButtonElement).disabled).toBe(true));
        expect((screen.getByRole('button', { name: 'Dispatch' }) as HTMLButtonElement).title).toBe('Select a row first');
    });

    it('is enabled without a selection when its form asks for the key', async () => {
        renderShipments(rows, [{ name: 'shipmentId', label: 'Shipment' }, { name: 'carrier', label: 'Carrier' }]);

        await screen.findByText('Fjordline');
        await waitFor(() => expect(screen.getByRole('button', { name: 'Dispatch' }).hasAttribute('disabled')).toBe(false));
    });

    it('is enabled once a row is selected and submits that row\'s key', async () => {
        const fetched = await selectAndSubmit();
        const [, init] = fetched.mock.calls.find(([, request]) => request?.method === 'POST')!;
        expect(JSON.parse(String(init?.body))).toEqual({ shipmentId: rows[1].shipmentId, carrier: 'Posten' });
    });

    it('submits the selected row\'s key, not the identity the session already holds', async () => {
        const held = sessionIdentity('ShipmentId');
        const fetched = await selectAndSubmit();
        const [, init] = fetched.mock.calls.find(([, request]) => request?.method === 'POST')!;
        expect(held).not.toBe(rows[1].shipmentId);
        expect(JSON.parse(String(init?.body)).shipmentId).toBe(rows[1].shipmentId);
    });
});

async function selectAndSubmit() {
    const fetched = renderShipments();

    fireEvent.click(await screen.findByText('Norsk Fisk'));
    await waitFor(() => expect((screen.getByRole('button', { name: 'Dispatch' }) as HTMLButtonElement).disabled).toBe(false));
    fireEvent.click(screen.getByRole('button', { name: 'Dispatch' }));
    const dialog = await screen.findByRole('dialog');
    fireEvent.change(within(dialog).getByLabelText('Carrier', { exact: false }), { target: { value: 'Posten' } });
    await act(async () => { fireEvent.click(within(dialog).getByRole('button', { name: /execute/i })); });

    await waitFor(() => expect(fetched.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(true));
    return fetched;
}
