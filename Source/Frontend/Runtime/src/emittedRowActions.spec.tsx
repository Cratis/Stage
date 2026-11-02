// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { queryResult, registerTestQueries } from './testing/queries';
import { fireEvent, render, screen, waitFor } from './testing/render';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core/config';
import { stageTheme } from './stageTheme';
import { StageDataProvider } from './stageData';
import { registerStageCommands } from '../../src/host/commandRegistration';
import { commandResult } from './testing/commands';
import { stageComponents } from './stageComponents';
import { StageScreenTables } from './stageScreenTables';
import { element } from './testElements';
import emitted from './fixtures/row-actions-scene.json';

// Captured only by when_creation_and_row_actions_share_a_screen with STAGE_UPDATE_VECTORS=1.
// Its default assertion requires this fixture to equal the current renderer's output.
const rows = [
    { resourceId: '11111111-1111-1111-1111-111111111111', name: 'Fjord' },
    { resourceId: '22222222-2222-2222-2222-222222222222', name: 'Mountain' },
];
const typedId = '33333333-3333-3333-3333-333333333333';

async function openScreen(index: number | 'toolbar', data = rows, subject?: 'item' | 'screen', overrides: Record<string, unknown> = {}) {
    const commandSchemas = typeof overrides.schema === 'string' ? { ...emitted.commandSchemas, RenameResource: overrides.schema } : emitted.commandSchemas;
    const routes = { commands: { RegisterResource: '/api/register', RenameResource: '/api/rename' }, commandSchemas, queries: { AllResources: '/api/resources' } };
    registerStageCommands(routes);
    registerTestQueries({ AllResources: '/api/resources' });
    const fetched = vi.fn((_: RequestInfo | URL, init?: RequestInit) => Promise.resolve(init?.method === 'POST'
        ? { ok: true, json: async () => commandResult() }
        : { ok: true, json: async () => queryResult({ data }) }));
    vi.stubGlobal('fetch', fetched);
    const authored = emitted.screens[0].slotContent.content[index === 'toolbar' ? 0 : index];
    const captured = element(authored.id ?? 'action', authored.componentName, authored.properties);
    const action = index === 'toolbar'
        ? element('register-toolbar', 'core:toolbarItem', { name: 'register', label: 'Register resource', kind: 'Action', target: 'RegisterResource' })
        : { ...captured, properties: { ...captured.properties, ...(subject ? { actionSubject: subject } : {}), ...overrides } };
    const table = element('resources', 'core:table', { query: 'AllResources' }, { columns: [element('resource-name', 'core:column', { property: 'name', label: 'Name' })] });
    const page = element('page', 'core:screen', {}, { content: [table, action] });
    const Table = stageComponents['core:table'];
    const Action = stageComponents[action.componentName];
    render(<PrimeReactProvider theme={stageTheme}><StageDataProvider routes={routes} routesReady locale='en' locales={['en']} screen='Resources'><StageScreenTables element={page}>
        <Table element={table} slots={{}} /><Action element={action} slots={{}} />
    </StageScreenTables></StageDataProvider></PrimeReactProvider>);
    if (data.length > 0) await screen.findByText('Fjord');
    return fetched;
}

afterEach(() => vi.unstubAllGlobals());

describe('renderer-emitted creation and row actions', () => {
    it.each(['toolbar', 0] as const)('creates with no row on an empty list (%s)', async index => {
        const fetched = await openScreen(index, []);
        fireEvent.click(screen.getByRole('button', { name: 'Register resource' }));
        fireEvent.change(await screen.findByLabelText(/^Resource [Ii]d$/), { target: { value: typedId } });
        fireEvent.change(screen.getByLabelText('Name'), { target: { value: 'New resource' } });
        fireEvent.click(screen.getByRole('button', { name: 'Register resource' }));
        await waitFor(() => expect(fetched.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(true));
        const [, init] = fetched.mock.calls.find(([, request]) => request?.method === 'POST')!;
        expect(JSON.parse(String(init?.body))).toEqual({ resourceId: typedId, name: 'New resource' });
    });

    it('binds an unhinted plain action to the selected row', async () => {
        const fetched = await openScreen(1);
        // Table rows render before the screen publishes them to action bindings.
        await waitFor(() => expect((screen.getByRole('button', { name: 'Rename resource' }) as HTMLButtonElement).disabled).toBe(true));
        fireEvent.click(screen.getByText('Mountain'));
        fireEvent.click(screen.getByRole('button', { name: 'Rename resource' }));
        expect(screen.queryByLabelText(/^Resource [Ii]d$/)).toBeNull();
        fireEvent.change(await screen.findByLabelText('Name'), { target: { value: 'Renamed' } });
        fireEvent.click(screen.getByRole('button', { name: 'Rename resource' }));
        await waitFor(() => expect(fetched.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(true));
        const [, init] = fetched.mock.calls.find(([, request]) => request?.method === 'POST')!;
        expect(JSON.parse(String(init?.body))).toEqual({ resourceId: rows[1].resourceId, name: 'Renamed' });
    });

    it('submits an identifier-only plain action with the selected row identifiers and asks for nothing', async () => {
        // A CloseWorkItem-shaped command: its only input is the identifier the selected row carries.
        const fetched = await openScreen(1, rows, undefined, {
            fields: [{ name: 'resourceId', label: 'Resource Id' }],
            schema: JSON.stringify({ type: 'object', properties: { resourceId: { type: 'string', format: 'uuid', 'x-concept': 'ResourceId' } }, required: ['resourceId'] }),
        });
        await waitFor(() => expect((screen.getByRole('button', { name: 'Rename resource' }) as HTMLButtonElement).disabled).toBe(true));
        fireEvent.click(screen.getByText('Mountain'));
        fireEvent.click(screen.getByRole('button', { name: 'Rename resource' }));
        expect(await screen.findByRole('dialog')).toBeTruthy();
        expect(screen.queryByLabelText(/^Resource [Ii]d$/)).toBeNull();
        expect(screen.queryByLabelText('Name')).toBeNull();
        fireEvent.click(screen.getAllByRole('button', { name: 'Rename resource' }).at(-1)!);
        await waitFor(() => expect(fetched.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(true));
        const [, init] = fetched.mock.calls.find(([, request]) => request?.method === 'POST')!;
        expect(JSON.parse(String(init?.body))).toEqual({ resourceId: rows[1].resourceId });
    });

    it('honors an explicit item subject on a creation candidate', async () => {
        await openScreen(0, rows, 'item');
        expect((screen.getByRole('button', { name: 'Register resource' }) as HTMLButtonElement).disabled).toBe(true);
        fireEvent.click(screen.getByText('Mountain'));
        fireEvent.click(screen.getByRole('button', { name: 'Register resource' }));
        expect(screen.queryByLabelText(/^Resource [Ii]d$/)).toBeNull();
        expect(await screen.findByLabelText('Name')).toBeTruthy();
    });

    it('honors an explicit screen subject without reading a row', async () => {
        await openScreen(1, rows, 'screen');
        fireEvent.click(screen.getByRole('button', { name: 'Rename resource' }));
        expect(await screen.findByLabelText(/^Resource [Ii]d$/)).toBeTruthy();
    });
});
