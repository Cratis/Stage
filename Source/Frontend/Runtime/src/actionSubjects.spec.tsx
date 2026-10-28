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
const StageToolbarItem = stageComponents['core:toolbarItem'];
const identified = JSON.stringify({
    type: 'object',
    required: ['shipmentId', 'carrier'],
    properties: { shipmentId: { type: 'string', format: 'uuid', 'x-concept': 'ShipmentId', 'x-identity': true }, carrier: { type: 'string' } },
});
const plain = JSON.stringify({
    type: 'object',
    required: ['shipmentId', 'carrier'],
    properties: { shipmentId: { type: 'string', format: 'uuid', 'x-concept': 'ShipmentId' }, carrier: { type: 'string' } },
});
const rows = [{ shipmentId: '7d1c8f3a-0000-4000-8000-000000000001', consignee: 'Fjordline' }, { shipmentId: '7d1c8f3a-0000-4000-8000-000000000002', consignee: 'Norsk Fisk' }];
const keyed = rows.map(({ shipmentId, consignee }) => ({ rowId: shipmentId, consignee }));

type Placed = (schema: string) => ReturnType<typeof element>;

function renderScreen(placed: Placed, schema: string, screenRows: Record<string, unknown>[], toolbar: boolean) {
    const routes = { commands: { DispatchShipment: '/api/dispatch' }, queries: { AllShipments: '/api/shipments' }, commandSchemas: { DispatchShipment: schema } };
    registerStageCommands(routes);
    registerTestQueries(routes.queries);
    const fetched = vi.fn((_: RequestInfo | URL, init?: RequestInit) => init?.method === 'POST'
        ? Promise.resolve({ ok: true, json: async () => commandResult() })
        : Promise.resolve({ ok: true, json: async () => queryResult({ data: screenRows }) }));
    vi.stubGlobal('fetch', fetched);
    globalThis.location.hash = '#/Shipments';
    const table = element('shipments', 'core:table', { query: 'AllShipments', typeName: 'ShipmentRow' }, { columns: [element('consignee', 'core:column', { property: 'consignee', label: 'Consignee' })] });
    const Placed = toolbar ? StageToolbarItem : StageAction;
    render(
        <PrimeReactProvider>
            <StageDataProvider routes={routes} locale='en' locales={['en']} screen='Shipments'>
                <Placed element={placed(schema)} slots={{}} />
                <RegisteredTable element={table} slots={{}} />
            </StageDataProvider>
        </PrimeReactProvider>,
    );
    return fetched;
}

const toolbarItem = (subject: string): Placed => () => element('dispatch', 'core:toolbarItem', { name: 'dispatch', kind: 'Action', label: 'Dispatch', target: 'DispatchShipment', actionSubject: subject });
const plainAction = (properties: Record<string, unknown>): Placed => schema => element('dispatch', 'core:action', { command: 'DispatchShipment', label: 'Dispatch', schema, ...properties });
const dispatchButton = () => screen.getByRole('button', { name: 'Dispatch' }) as HTMLButtonElement;

async function submit(fetched: ReturnType<typeof renderScreen>, carrier = 'Posten') {
    fireEvent.click(dispatchButton());
    const dialog = await screen.findByRole('dialog');
    fireEvent.change(within(dialog).getByLabelText('Carrier', { exact: false }), { target: { value: carrier } });
    await act(async () => { fireEvent.click(within(dialog).getByRole('button', { name: /submit/i })); });
    await waitFor(() => expect(fetched.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(true));
    const [, init] = fetched.mock.calls.find(([, request]) => request?.method === 'POST')!;
    return JSON.parse(String(init?.body));
}

describe('a toolbar action that names the item as its subject', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); });

    it('is disabled with an empty table', async () => {
        renderScreen(toolbarItem('item'), identified, [], true);

        await waitFor(() => expect(dispatchButton().disabled).toBe(true));
        expect(dispatchButton().title).toBe('Select a row first');
    });

    it('is disabled while no row is selected', async () => {
        renderScreen(toolbarItem('item'), identified, rows, true);

        await screen.findByText('Fjordline');
        await waitFor(() => expect(dispatchButton().disabled).toBe(true));
    });

    it('submits the selected row\'s key once a row is selected', async () => {
        const fetched = renderScreen(toolbarItem('item'), identified, rows, true);

        fireEvent.click(await screen.findByText('Norsk Fisk'));
        await waitFor(() => expect(dispatchButton().disabled).toBe(false));

        expect(await submit(fetched)).toEqual({ shipmentId: rows[1].shipmentId, carrier: 'Posten' });
    });
});

describe('a toolbar action that names the screen as its subject', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); });

    it('is enabled with nothing selected and submits the session identity, never a row key', async () => {
        const fetched = renderScreen(toolbarItem('screen'), identified, rows, true);

        await screen.findByText('Fjordline');
        await waitFor(() => expect(dispatchButton().disabled).toBe(false));

        const body = await submit(fetched);
        expect(body.shipmentId).toBe(sessionIdentity('ShipmentId'));
        expect(rows.map(row => row.shipmentId)).not.toContain(body.shipmentId);
    });
});

describe('a screen action that names the item as its subject', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); });

    const forItem = plainAction({ actionSubject: 'item', subjectInputs: { shipmentId: { subject: 'item.rowId' } } });

    it('is disabled until a row is selected', async () => {
        renderScreen(forItem, plain, keyed, false);

        await screen.findByText('Fjordline');
        await waitFor(() => expect(dispatchButton().disabled).toBe(true));
        expect(dispatchButton().title).toBe('Select a row first');
    });

    it('submits the input bound from the selected row without asking for it', async () => {
        const fetched = renderScreen(forItem, plain, keyed, false);

        fireEvent.click(await screen.findByText('Norsk Fisk'));
        await waitFor(() => expect(dispatchButton().disabled).toBe(false));
        fireEvent.click(dispatchButton());
        const dialog = await screen.findByRole('dialog');
        expect(within(dialog).queryByLabelText('Shipment id', { exact: false })).toBeNull();
        fireEvent.change(within(dialog).getByLabelText('Carrier', { exact: false }), { target: { value: 'Posten' } });
        await act(async () => { fireEvent.click(within(dialog).getByRole('button', { name: /submit/i })); });

        await waitFor(() => expect(fetched.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(true));
        const [, init] = fetched.mock.calls.find(([, request]) => request?.method === 'POST')!;
        expect(JSON.parse(String(init?.body))).toEqual({ shipmentId: keyed[1].rowId, carrier: 'Posten' });
    });
});

describe('a screen action that names the screen as its subject', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); });

    it('is enabled although the screen\'s rows carry the key it acts on', async () => {
        renderScreen(plainAction({ actionSubject: 'screen', fields: [{ name: 'carrier', label: 'Carrier' }], generationMode: 'manual' }), plain, rows, false);

        await screen.findByText('Fjordline');
        await waitFor(() => expect(dispatchButton().disabled).toBe(false));
    });
});
