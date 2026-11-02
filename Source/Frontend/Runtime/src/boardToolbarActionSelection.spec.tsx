// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { useEffect, useMemo } from 'react';
import { afterEach, chai, describe, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { registerStageCommands } from '../../src/host/commandRegistration';
import { stageComponents } from './stageComponents';
import { StageDataProvider, useStageData } from './stageData';
import { StageQueryBindings } from './stageQueryBindings';
import { StageScreenTables } from './stageScreenTables';
import { useComponentOutputsPublisher } from './stageComponentOutputs';
import { element } from './testElements';
import { registerTestQueries, queryResult } from './testing/queries';
import { commandResult } from './testing/commands';

chai.should();

const StageToolbarItem = stageComponents['core:toolbarItem'];
const rows = [
    { shipmentId: '7d1c8f3a-0000-4000-8000-000000000001', title: 'Fjordline', column: 'Ready' },
    { shipmentId: '7d1c8f3a-0000-4000-8000-000000000002', title: 'Norsk Fisk', column: 'Ready' },
];
const routes = {
    commands: { DispatchShipment: '/api/dispatch' },
    queries: { PickingQueue: '/api/picking' },
    commandSchemas: {
        DispatchShipment: JSON.stringify({
            type: 'object', required: ['shipmentId', 'carrier'], properties: {
                shipmentId: { type: 'string', format: 'uuid', 'x-concept': 'ShipmentId', 'x-identity': true },
                carrier: { type: 'string' },
            },
        }),
    },
};
const toolbarItem = element('dispatch', 'core:toolbarItem', { name: 'dispatch', kind: 'Action', label: 'Dispatch', target: 'DispatchShipment' });
const board = element('picking', 'Cratis.Blueprint.Default:kanbanBoard', { cards: { kind: 'queryResult', query: 'PickingQueue', path: '' } });
const composedScreen = element('picking-screen', 'core:stack', {}, { content: [toolbarItem, board] });

/** Scene's board output boundary: the original query row, or null after clearing selection. */
function BoardOutputs({ selectedItem, onOutputs, label = 'picking' }: { selectedItem: Record<string, unknown> | null; onOutputs?: ReturnType<typeof useComponentOutputsPublisher>; label?: string }) {
    const ownPublish = useComponentOutputsPublisher();
    const publish = onOutputs ?? ownPublish;
    const outputs = useMemo(() => ({ picking: { selectedItem } }), [selectedItem]);
    const loading = useStageData().queries['binding:PickingQueue']?.loading ?? true;
    useEffect(() => publish(outputs), [outputs, publish]);
    useEffect(() => () => publish({}), [publish]);
    return <output aria-label={`${label} status`}>{loading ? 'Loading' : 'Ready'}</output>;
}

function BoardScreen({ selectedItem, showBoard, subject }: { selectedItem: Record<string, unknown> | null; showBoard: boolean; subject?: string }) {
    const outputs = useComponentOutputsPublisher();
    return (
        <StageScreenTables element={composedScreen} outputScope={outputs.scope}>
            <StageQueryBindings element={composedScreen} />
            <StageToolbarItem element={{ ...toolbarItem, properties: { ...toolbarItem.properties, actionSubject: subject } }} slots={{}} />
            {showBoard && <BoardOutputs selectedItem={selectedItem} onOutputs={outputs} />}
        </StageScreenTables>
    );
}

function view(selectedItem: Record<string, unknown> | null, showBoard = true, subject?: string) {
    return (
        <PrimeReactProvider>
            <StageDataProvider routes={routes} locale='en' locales={['en']} screen='PickingQueue'>
                <BoardScreen selectedItem={selectedItem} showBoard={showBoard} subject={subject} />
                <BoardOutputs label='another-screen' selectedItem={rows[0]} />
            </StageDataProvider>
        </PrimeReactProvider>
    );
}

async function renderBoard(selectedItem: Record<string, unknown> | null = null, subject?: string) {
    registerStageCommands(routes);
    registerTestQueries(routes.queries);
    const fetched = vi.fn((_: RequestInfo | URL, init?: RequestInit) => init?.method === 'POST'
        ? Promise.resolve({ ok: true, json: async () => commandResult() })
        : Promise.resolve({ ok: true, json: async () => queryResult({ data: rows }) }));
    vi.stubGlobal('fetch', fetched);
    globalThis.location.hash = '#/PickingQueue';
    const rendered = render(view(selectedItem, true, subject));
    await waitFor(() => (screen.getByLabelText('picking status').textContent ?? '').should.equal('Ready'));
    return { rendered, fetched };
}

const dispatchButton = () => screen.getByRole('button', { name: 'Dispatch' }) as HTMLButtonElement;

async function selectCard(rendered: ReturnType<typeof render>, subject?: string) {
    rendered.rerender(view(rows[1], true, subject));
    await waitFor(() => dispatchButton().disabled.should.equal(false));
}

describe('a board toolbar action using Scene selectedItem outputs', () => {
    afterEach(() => {
        cleanup();
        vi.unstubAllGlobals();
        globalThis.localStorage?.clear();
        globalThis.location.hash = '#';
    });

    it('should stay disabled until a card is selected', async () => {
        await renderBoard();

        await waitFor(() => dispatchButton().disabled.should.equal(true));
        dispatchButton().title.should.equal('Select a row first');
    });

    it.each([undefined, 'item'])('should gate and submit its own selected card with subject %s', async subject => {
        const { rendered, fetched } = await renderBoard(null, subject);
        await waitFor(() => dispatchButton().disabled.should.equal(true));
        await selectCard(rendered, subject);

        fireEvent.click(dispatchButton());
        const dialog = await screen.findByRole('dialog');
        fireEvent.change(within(dialog).getByLabelText('Carrier', { exact: false }), { target: { value: 'Posten' } });
        await act(async () => { fireEvent.click(within(dialog).getByRole('button', { name: 'Submit' })); });

        await waitFor(() => fetched.mock.calls.some(([, request]) => request?.method === 'POST').should.equal(true));
        const [, request] = fetched.mock.calls.find(([, init]) => init?.method === 'POST')!;
        JSON.parse(String(request?.body)).should.deep.equal({ shipmentId: rows[1].shipmentId, carrier: 'Posten' });
    });

    it('should disable the action again after Scene clears the selected card', async () => {
        const { rendered } = await renderBoard();
        await selectCard(rendered);

        rendered.rerender(view(null));

        await waitFor(() => dispatchButton().disabled.should.equal(true));
    });

    it('should clear the selection when the board tree is withdrawn', async () => {
        const { rendered } = await renderBoard();
        await selectCard(rendered);

        rendered.rerender(view(null, false));

        await waitFor(() => dispatchButton().disabled.should.equal(true));
    });
});
