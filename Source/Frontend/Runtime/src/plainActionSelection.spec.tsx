// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, chai, describe, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { registerStageCommands } from '../../src/host/commandRegistration';
import { StageAction } from './stageComponents';
import { StageDataProvider } from './stageData';
import { element } from './testElements';
import { RegisteredTable, registerTestQueries, queryResult } from './testing/queries';
import { commandResult } from './testing/commands';
import { sessionIdentity } from './stageSession';
import { humanize, schemaPropertiesOf } from './stageFormSchema';

chai.should();
const rows = [{ shipmentId: '7d1c8f3a-0000-4000-8000-000000000001', consignee: 'Fjordline' }];
const schema = JSON.stringify({ type: 'object', required: ['shipmentId', 'carrier'], properties: {
    shipmentId: { type: 'string', format: 'uuid', 'x-concept': 'ShipmentId', 'x-identity': true },
    carrier: { type: 'string' },
} });

const assignmentSchema = JSON.stringify({ type: 'object', required: ['shipmentId', 'driverId'], properties: {
    shipmentId: { type: 'string', format: 'uuid', 'x-concept': 'ShipmentId', 'x-identity': true },
    driverId: { type: 'string', format: 'uuid', 'x-concept': 'DriverId' },
} });

const creationSchema = JSON.stringify({ type: 'object', 'x-creates': true, required: ['shipmentId', 'carrier'], properties: {
    shipmentId: { type: 'string', format: 'uuid', 'x-concept': 'ShipmentId' },
    carrier: { type: 'string' },
} });

function renderShipments(generated = false, otherScreen = false, empty = false, referenceSchema?: string) {
    const commandSchema = referenceSchema ?? (generated ? JSON.stringify({ type: 'object', required: ['carrier'], properties: { carrier: { type: 'string' } } }) : schema);
    const routes = { commands: { DispatchShipment: '/api/dispatch' }, queries: { AllShipments: '/api/shipments' }, commandSchemas: { DispatchShipment: commandSchema } };
    registerStageCommands(routes);
    registerTestQueries(routes.queries);
    const fetched = vi.fn((_: RequestInfo | URL, init?: RequestInit) => init?.method === 'POST'
        ? Promise.resolve({ ok: true, json: async () => commandResult() })
        : Promise.resolve({ ok: true, json: async () => queryResult({ data: empty ? [] : rows }) }));
    vi.stubGlobal('fetch', fetched);
    const table = <RegisteredTable element={element('shipments', 'core:table', { query: 'AllShipments', typeName: 'ShipmentRow' }, { columns: [element('consignee', 'core:column', { property: 'consignee', label: 'Consignee' })] })} slots={{}} />;
    render(<PrimeReactProvider>
        {otherScreen && <StageDataProvider routes={routes} locale='en' locales={['en']} screen='Other'>{table}</StageDataProvider>}
        <StageDataProvider routes={routes} locale='en' locales={['en']} screen='Shipments'>
            <StageAction element={element('dispatch', 'core:action', { command: 'DispatchShipment', label: 'Dispatch', schema: commandSchema, rowAction: !generated && !otherScreen, fields: schemaPropertiesOf(commandSchema).map(property => ({ name: property.name, label: humanize(property.name) })) })} slots={{}} />
            {!otherScreen && table}
        </StageDataProvider>
    </PrimeReactProvider>);
    return fetched;
}

async function selectShipmentAndOpen() {
    fireEvent.click(await screen.findByText('Fjordline'));
    await waitFor(() => (screen.getByRole('button', { name: 'Dispatch' }) as HTMLButtonElement).disabled.should.be.false);
    fireEvent.click(screen.getByRole('button', { name: 'Dispatch' }));
    return screen.findByRole('dialog');
}

async function execute(dialog: HTMLElement, fetched: ReturnType<typeof renderShipments>) {
    await act(async () => { fireEvent.click(within(dialog).getByRole('button', { name: /execute/i })); });
    await waitFor(() => fetched.mock.calls.some(([, init]) => init?.method === 'POST').should.be.true);
    const [, init] = fetched.mock.calls.find(([, request]) => request?.method === 'POST')!;
    return JSON.parse(String(init?.body)) as Record<string, unknown>;
}

describe('a plain action beside a collection', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); });

    it('should wait for a selected row', async () => {
        renderShipments();
        await screen.findByText('Fjordline');
        await waitFor(() => (screen.getByRole('button', { name: 'Dispatch' }) as HTMLButtonElement).disabled.should.be.true);
        (screen.getByRole('button', { name: 'Dispatch' }) as HTMLButtonElement).title.should.equal('Select a row first');
    });

    it('should hide and submit the selected identifier instead of the held session identity', async () => {
        const held = sessionIdentity('ShipmentId');
        const fetched = renderShipments();
        const dialog = await selectShipmentAndOpen();
        within(dialog).queryAllByLabelText(/shipment id/i).length.should.equal(0);
        fireEvent.change(within(dialog).getByLabelText('Carrier', { exact: false }), { target: { value: 'Posten' } });
        const submitted = await execute(dialog, fetched);
        submitted.should.deep.equal({ shipmentId: rows[0].shipmentId, carrier: 'Posten' });
        held.should.not.equal(rows[0].shipmentId);
    });

    it('should ask for an unrelated uuid reference and submit only the carried identifier from the row', async () => {
        const fetched = renderShipments(false, false, false, assignmentSchema);
        const dialog = await selectShipmentAndOpen();
        within(dialog).queryAllByLabelText(/shipment id/i).length.should.equal(0);
        const driverId = '7d1c8f3a-0000-4000-8000-000000000003';
        fireEvent.change(within(dialog).getByLabelText('Driver Id', { exact: false }), { target: { value: driverId } });
        const submitted = await execute(dialog, fetched);
        submitted.should.deep.equal({ shipmentId: rows[0].shipmentId, driverId });
    });

    it('should keep a modeled row action disabled when its table is empty', async () => {
        renderShipments(false, false, true);
        await waitFor(() => (screen.getByRole('button', { name: 'Dispatch' }) as HTMLButtonElement).disabled.should.be.true);
        (screen.getByRole('button', { name: 'Dispatch' }) as HTMLButtonElement).title.should.equal('Select a row first');
    });

    it('should leave a command with a generated identifier enabled without selection', async () => {
        renderShipments(true);
        await screen.findByText('Fjordline');
        (screen.getByRole('button', { name: 'Dispatch' }) as HTMLButtonElement).disabled.should.be.false;
    });

    it('should neither wait for nor take a selected row when the command creates its entity', async () => {
        const fetched = renderShipments(false, false, false, creationSchema);
        await screen.findByText('Fjordline');
        (screen.getByRole('button', { name: 'Dispatch' }) as HTMLButtonElement).disabled.should.be.false;
        const dialog = await selectShipmentAndOpen();
        const entered = '7d1c8f3a-0000-4000-8000-000000000009';
        fireEvent.change(within(dialog).getByLabelText('Shipment Id', { exact: false }), { target: { value: entered } });
        fireEvent.change(within(dialog).getByLabelText('Carrier', { exact: false }), { target: { value: 'Posten' } });
        const submitted = await execute(dialog, fetched);
        submitted.should.deep.equal({ shipmentId: entered, carrier: 'Posten' });
    });

    it('should not bind to a table on another screen', async () => {
        renderShipments(false, true);
        fireEvent.click(await screen.findByText('Fjordline'));
        fireEvent.click(screen.getByRole('button', { name: 'Dispatch' }));
        const dialog = await screen.findByRole('dialog');
        within(dialog).queryAllByLabelText(/shipment id/i).length.should.equal(1);
    });
});
