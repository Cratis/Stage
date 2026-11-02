// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import type { ExternalComponent, Screen } from '@cratis/scene.model';
import { StageConfirmDialog } from './StageConfirmDialog';
import { asDialogScreens, stageConfirmDialogComponent } from './stageDialogScreens';
import { registerStageCommands } from '../../src/host/commandRegistration';
import { stageComponents } from './stageComponents';
import { StageDataProvider } from './stageData';
import { StageQueryBindings } from './stageQueryBindings';
import { screenTablesOf, StageScreenTables } from './stageScreenTables';
import { element } from './testElements';
import { RegisteredTable, registerTestQueries, queryResult } from './testing/queries';
import { commandResult } from './testing/commands';

describe('the tables belonging to a screen', () => {
    it.each(['kanbanBoard', 'Cratis.Blueprint.Default:kanbanBoard'])('should register only the card query and selection of a %s board', component => {
        const board = element('picking', component, { cards: { kind: 'queryResult', query: 'PickingQueue' }, columns: { kind: 'queryResult', query: 'Columns' } });
        const summary = element('summary', 'core:text', { value: { kind: 'queryResult', query: 'OtherShipments' } });
        const outlet = element('outlet', 'core:outlet', {}, { content: [element('foreign', component, { cards: { kind: 'queryResult', query: 'ForeignQueue' } })] });

        expect(screenTablesOf(element('screen', 'core:stack', {}, { content: [board, summary, outlet] }))).toEqual(['picking', 'binding:PickingQueue']);
    });

    it('should find distinct tables nested in any slot without descending into outlets', () => {
        const ownTable = element('own-table', 'core:table');
        const nested = element('nested', 'core:stack', {}, { content: [ownTable] });
        const outlet = element('outlet', 'core:outlet', {}, { content: [element('other-table', 'core:table')] });
        const tree = element('screen', 'core:stack', {}, { body: [nested, outlet], footer: [ownTable, element('footer-table', 'core:table')] });

        expect(screenTablesOf(tree)).toEqual(['own-table', 'footer-table']);
    });
});

const StageAction = stageComponents['core:action'];
const schema = JSON.stringify({
    type: 'object',
    required: ['shipmentId', 'carrier'],
    properties: {
        shipmentId: { type: 'string', format: 'uuid', 'x-concept': 'ShipmentId' },
        carrier: { type: 'string' },
    },
});
const ownRow = { shipmentId: '7d1c8f3a-0000-4000-8000-000000000001', consignee: 'Own shipment' };
const otherRow = { shipmentId: '7d1c8f3a-0000-4000-8000-000000000002', consignee: 'Other shipment' };

function respond(url: RequestInfo | URL, init?: RequestInit) {
    if (init?.method === 'POST') return Promise.resolve({ ok: true, json: async () => commandResult() });
    const row = String(url).includes('own-shipments') ? ownRow : otherRow;
    return Promise.resolve({ ok: true, json: async () => queryResult({ data: [row] }) });
}

function renderScreens(withTable = true, withData = false, binding = false) {
    const routes = {
        commands: { DispatchShipment: '/api/scoped-dispatch' },
        queries: { OwnShipments: '/api/own-shipments', OtherShipments: '/api/other-shipments', ShipmentData: '/api/shipment-data' },
        commandSchemas: { DispatchShipment: schema },
    };
    registerStageCommands(routes);
    registerTestQueries(routes.queries);
    const fetched = vi.fn(respond);
    vi.stubGlobal('fetch', fetched);
    globalThis.location.hash = '#/Shipments';
    const table = (id: string, query: string) => element(id, 'core:table', { query }, {
        columns: [element(`${id}-consignee`, 'core:column', { property: 'consignee', label: 'Consignee' })],
    });
    const ownTable = table('own-table', 'OwnShipments');
    const otherTable = table('other-table', 'OtherShipments');
    const action = element('dispatch', 'core:action', {
        command: 'DispatchShipment', label: 'Dispatch', schema,
        fields: [{ name: 'carrier', label: 'Carrier' }], generationMode: 'manual',
        ...(withTable ? { rowAction: true } : {}),
    });
    const source = binding
        ? element('summary', 'core:text', { value: { kind: 'queryResult', query: 'ShipmentData', path: '0.shipmentId' } })
        : element('data', 'core:data', { typeName: 'ShipmentRow', query: 'ShipmentData' });
    const ownScreen = element('own-screen', 'core:stack', {}, { children: [action, ...(withTable ? [ownTable] : []), ...(withData ? [source] : [])] });
    const otherScreen = element('other-screen', 'core:stack', {}, { children: [otherTable] });
    render(
        <PrimeReactProvider>
            <StageDataProvider routes={routes} locale='en' locales={['en']} screen='Shipments'>
                <StageScreenTables element={ownScreen}>
                    {withData && <StageQueryBindings element={ownScreen} />}
                    <StageAction element={action} slots={{}} />
                    {withTable && <RegisteredTable element={ownTable} slots={{}} />}
                </StageScreenTables>
                {!withData && <StageScreenTables element={otherScreen}>
                    <RegisteredTable element={otherTable} slots={{}} />
                </StageScreenTables>}
            </StageDataProvider>
        </PrimeReactProvider>,
    );
    return fetched;
}

async function submitDispatch() {
    fireEvent.click(screen.getByRole('button', { name: 'Dispatch' }));
    const dialog = await screen.findByRole('dialog');
    fireEvent.change(within(dialog).getByLabelText('Carrier', { exact: false }), { target: { value: 'Posten' } });
    await act(async () => { fireEvent.click(within(dialog).getByRole('button', { name: /submit/i })); });
}

async function submittedBody(fetched: ReturnType<typeof renderScreens>) {
    await waitFor(() => expect(fetched.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(true));
    const [, init] = fetched.mock.calls.find(([, request]) => request?.method === 'POST')!;
    return JSON.parse(String(init?.body));
}

describe('an action scoped to its own screen tables', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); });

    it('should keep the selected list row when a confirm-dialog screen opens its decision form', async () => {
        const routes = { commands: { DispatchShipment: '/api/scoped-dispatch' }, queries: { OwnShipments: '/api/own-shipments' }, commandSchemas: { DispatchShipment: schema } };
        registerStageCommands(routes);
        registerTestQueries(routes.queries);
        const fetched = vi.fn(respond);
        vi.stubGlobal('fetch', fetched);
        globalThis.location.hash = '#/Shipments';
        const table = element('own-table', 'core:table', { query: 'OwnShipments' }, {
            columns: [element('consignee', 'core:column', { property: 'consignee', label: 'Consignee' })],
        });
        const [dialog] = asDialogScreens([{
            name: 'Dispatch', screenTemplate: 'ConfirmDialog', forms: [],
            slotContent: { actions: [element('dispatch', 'core:action', { command: 'DispatchShipment' })] },
        } as unknown as Screen], []);
        const confirmation = dialog.slotContent.content[0] as ExternalComponent;
        const show = (content: ExternalComponent) => (
            <PrimeReactProvider>
                <StageDataProvider routes={routes} locale='en' locales={['en']} screen={content.id}>
                    <StageScreenTables element={content}>
                        {content.componentName === stageConfirmDialogComponent ? <StageConfirmDialog element={content} /> : <RegisteredTable element={content} slots={{}} />}
                    </StageScreenTables>
                </StageDataProvider>
            </PrimeReactProvider>
        );
        const view = render(show(table));
        fireEvent.click(await screen.findByText('Own shipment'));
        globalThis.location.hash = '#/Dispatch';
        view.rerender(show(confirmation));
        expect(screenTablesOf(confirmation)).toEqual([]);
        fireEvent.click(screen.getByRole('button', { name: 'Dispatch shipment' }));
        expect(screen.queryByLabelText(/shipmentId/i)).toBeNull();
        fireEvent.change(screen.getByLabelText('carrier', { exact: false }), { target: { value: 'Posten' } });
        fireEvent.click(screen.getByRole('button', { name: 'Confirm' }));

        expect(await submittedBody(fetched)).toEqual({ shipmentId: ownRow.shipmentId, carrier: 'Posten' });
    });

    it('should stay disabled when only another screen has a selected row', async () => {
        renderScreens();
        await screen.findByText('Own shipment');
        fireEvent.click(await screen.findByText('Other shipment'));

        await waitFor(() => expect((screen.getByRole('button', { name: 'Dispatch' }) as HTMLButtonElement).disabled).toBe(true));
        expect(screen.getByRole('button', { name: 'Dispatch' }).title).toBe('Select a row first');
    });

    it('should enable and submit its own selected row even when another screen has a selection', async () => {
        const fetched = renderScreens();
        fireEvent.click(await screen.findByText('Other shipment'));
        fireEvent.click(await screen.findByText('Own shipment'));

        await waitFor(() => expect((screen.getByRole('button', { name: 'Dispatch' }) as HTMLButtonElement).disabled).toBe(false));
        await submitDispatch();

        expect(await submittedBody(fetched)).toEqual({ shipmentId: ownRow.shipmentId, carrier: 'Posten' });
    });

    it('should not fill a plain action identifier from another screen selection', async () => {
        const fetched = renderScreens(false);
        fireEvent.click(await screen.findByText('Other shipment'));
        await submitDispatch();

        expect((await submittedBody(fetched)).shipmentId).not.toBe(otherRow.shipmentId);
    });

    it.each([{ binding: false, source: 'data declaration' }, { binding: true, source: 'query binding' }])('should not gate an action on rows from a $source', async ({ binding }) => {
        const fetched = renderScreens(false, true, binding);
        await waitFor(() => expect(fetched.mock.calls.some(([url]) => String(url).includes('shipment-data'))).toBe(true));
        await act(async () => {});

        expect((screen.getByRole('button', { name: 'Dispatch' }) as HTMLButtonElement).disabled).toBe(false);
    });

    it.each([{ binding: false, source: 'data declaration' }, { binding: true, source: 'query binding' }])('should not fill an action identifier from a $source', async ({ binding }) => {
        const fetched = renderScreens(false, true, binding);
        await waitFor(() => expect(fetched.mock.calls.some(([url]) => String(url).includes('shipment-data'))).toBe(true));
        await submitDispatch();

        expect((await submittedBody(fetched)).shipmentId).not.toBe(otherRow.shipmentId);
    });
});
