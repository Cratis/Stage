// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import type { ExternalComponent } from '@cratis/scene.model';
import { PrimeReactProvider } from '@primereact/core/config';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor, within } from './testing/render';
import { queryResult, registerTestQueries } from './testing/queries';
import { commandResult } from './testing/commands';
import { registerStageCommands } from '../../src/host/commandRegistration';
import { StageDataProvider } from './stageData';
import { StageScreenTables, screenRowMenuActionsOf } from './stageScreenTables';
import { stageComponents } from './stageComponents';
import { stageTheme } from './stageTheme';
import { element } from './testElements';
import { isRowMenuAction } from './stageRowMenuActions';

// Only the real source-to-Scene C# specification writes this file (STAGE_UPDATE_VECTORS=1).
// Read at runtime so type-checking does not depend on having captured the fixture yet.
const emitted: unknown = JSON.parse(readFileSync(new URL('./fixtures/row-menu-actions-scene.json', import.meta.url), 'utf8'));

function componentsOf(node: unknown): ExternalComponent[] {
    if (node === null || typeof node !== 'object') return [];
    const component = 'componentName' in node ? [node as ExternalComponent] : [];
    return [...component, ...Object.values(node).flatMap(componentsOf)];
}

const captured = componentsOf(emitted);
const actions = captured.filter(component => component.componentName === 'core:action');
const capturedTable = captured.find(component => component.componentName === 'core:table')!;
const rows = [
    { resourceId: '11111111-1111-1111-1111-111111111111', name: 'Fjord' },
    { resourceId: '22222222-2222-2222-2222-222222222222', name: 'Mountain' },
];
const otherRow = { resourceId: '33333333-3333-3333-3333-333333333333', name: 'River' };
const Table = stageComponents['core:table'];
const Action = stageComponents['core:action'];

interface ScreenOptions {
    labels?: string[];
    secondTable?: boolean;
    secondScreen?: boolean;
    refused?: boolean;
    pending?: boolean;
    navigating?: boolean;
}

async function openScreen({ labels, secondTable, secondScreen, refused, pending, navigating = true }: ScreenOptions = {}) {
    const commands = { RegisterResource: '/api/register', RenameResource: '/api/rename', ArchiveResource: '/api/archive' };
    const commandSchemas = Object.fromEntries(actions.map(action => [String(action.properties.command), String(action.properties.schema)]));
    const routes = { commands, commandSchemas, queries: { AllResources: '/api/resources', OtherResources: '/api/other-resources' } };
    registerStageCommands(routes);
    registerTestQueries(routes.queries);
    const fetched = vi.fn((url: RequestInfo | URL, init?: RequestInit) => {
        const path = String(url);
        if (path.endsWith('/validate')) return Promise.resolve({ ok: true, json: async () => commandResult(refused && path.includes('/rename/') ? { isSuccess: false, isAuthorized: false, authorizationFailureReason: 'Archived resources cannot be renamed' } : {}) });
        if (init?.method === 'POST') return pending ? new Promise<Response>(() => undefined) : Promise.resolve({ ok: true, json: async () => commandResult() });
        return Promise.resolve({ ok: true, json: async () => queryResult({ data: path.includes('other-resources') ? [otherRow] : rows }) });
    });
    vi.stubGlobal('fetch', fetched);
    const table = element('resources', 'core:table', { ...capturedTable.properties, query: 'AllResources', label: 'Resources', dataKey: 'resourceId', ...(navigating ? { navigateOnRowClickToScreen: 'Details', navigateOnRowClickByParameter: 'resourceId' } : {}) }, capturedTable.slots);
    const other = element('other-resources', 'core:table', { ...capturedTable.properties, query: 'OtherResources', label: 'Other resources', dataKey: 'resourceId' }, capturedTable.slots);
    const offered = actions.filter(action => !labels || labels.includes(String(action.properties.label)));
    const tables = secondTable ? [table, other] : [table];
    const page = element('page', 'core:screen', {}, { content: [...tables, ...offered] });
    const unrelated = element('unrelated', 'core:screen', {}, { content: [other] });
    render(<PrimeReactProvider theme={stageTheme}><StageDataProvider routes={routes} routesReady locale='en' locales={['en']} screen='Resources' parameters={{ resourceId: '44444444-4444-4444-4444-444444444444' }}>
        <StageScreenTables element={page}>
            {tables.map(item => <Table key={item.id} element={item} slots={{}} />)}
            {offered.map(action => <Action key={action.id} element={action} slots={{}} />)}
        </StageScreenTables>
        {secondScreen && <StageScreenTables element={unrelated}><Table element={other} slots={{}} /></StageScreenTables>}
    </StageDataProvider></PrimeReactProvider>);
    await screen.findByText('Mountain');
    if (secondTable || secondScreen) await screen.findByText('River');
    return fetched;
}

function submitted(fetched: Awaited<ReturnType<typeof openScreen>>) {
    return fetched.mock.calls.filter(([url, init]) => init?.method === 'POST' && !String(url).endsWith('/validate'));
}

async function renameFromMenu(trigger: HTMLElement, name: string) {
    fireEvent.click(trigger);
    fireEvent.click(await screen.findByRole('menuitem', { name: 'Rename', exact: true }));
    const dialog = await screen.findByRole('dialog', { name: 'Rename', exact: true });
    fireEvent.change(within(dialog).getByLabelText(/^Name/), { target: { value: name } });
    fireEvent.click(within(dialog).getByRole('button', { name: 'Rename', exact: true }));
}

afterEach(() => {
    vi.unstubAllGlobals();
    globalThis.location.hash = '';
    globalThis.localStorage?.clear();
});

describe('the emitted item-action row menu', () => {
    it('groups exactly the opted-in siblings once and leaves other actions inline', async () => {
        await openScreen();
        expect(screen.queryByRole('button', { name: 'Rename', exact: true })).toBeNull();
        expect(screen.queryByRole('button', { name: 'Archive', exact: true })).toBeNull();
        expect(screen.getByRole('button', { name: 'Create', exact: true })).toBeDefined();
        expect(screen.getByRole('button', { name: 'Create from screen' })).toBeDefined();
        expect((screen.getByRole('button', { name: 'Inline rename' }) as HTMLButtonElement).disabled).toBe(true);
        expect(screen.getByRole('button', { name: 'Advisory rename' })).toBeDefined();
        expect(screen.getByRole('button', { name: 'Unknown placement' })).toBeDefined();
        fireEvent.click(screen.getAllByRole('button', { name: 'Row actions' })[0]);
        expect(await screen.findAllByRole('menuitem')).toHaveLength(2);
        expect(screen.getAllByRole('menuitem', { name: 'Rename', exact: true })).toHaveLength(1);
        expect(screen.getAllByRole('menuitem', { name: 'Archive', exact: true })).toHaveLength(1);
    });

    it('adds no menu or actions column without an opted-in item action', async () => {
        await openScreen({ labels: ['Create', 'Create from screen', 'Inline rename', 'Advisory rename', 'Unknown placement'] });
        expect(screen.queryByRole('button', { name: 'Row actions' })).toBeNull();
        expect(screen.queryByRole('columnheader', { name: 'Actions' })).toBeNull();
    });

    it('opens without selecting or navigating the row and submits the clicked row rather than route values', async () => {
        const fetched = await openScreen();
        const before = globalThis.location.hash;
        await renameFromMenu(screen.getAllByRole('button', { name: 'Row actions' })[1], 'New name');
        await waitFor(() => expect(submitted(fetched)).toHaveLength(1));
        expect(JSON.parse(submitted(fetched)[0][1]!.body as string)).toMatchObject({ resourceId: rows[1].resourceId, name: 'New name' });
        expect(screen.getByText('Mountain').closest('tr')?.getAttribute('aria-selected')).toBe('false');
        // The authored action may navigate after success; opening the menu never invokes the table's Details route.
        expect(before).not.toContain('Details');
        expect(globalThis.location.hash).not.toContain('Details');
    });

    it('does not inherit another table selection or a sibling screen action scope', async () => {
        const fetched = await openScreen({ secondTable: true, navigating: false });
        fireEvent.click(screen.getByText('Fjord'));
        const other = screen.getByRole('table', { name: 'Other resources' });
        await renameFromMenu(within(other).getByRole('button', { name: 'Row actions' }), 'River renamed');
        await waitFor(() => expect(submitted(fetched)).toHaveLength(1));
        expect(JSON.parse(submitted(fetched)[0][1]!.body as string).resourceId).toBe(otherRow.resourceId);
    });

    it('does not leak menus into another screen', async () => {
        await openScreen({ secondScreen: true });
        const other = screen.getByRole('table', { name: 'Other resources' });
        expect(within(other).queryByRole('button', { name: 'Row actions' })).toBeNull();
    });

    it('retains the server refusal and reason on the menu item', async () => {
        const fetched = await openScreen({ refused: true });
        fireEvent.click(screen.getAllByRole('button', { name: 'Row actions' })[1]);
        const item = await screen.findByRole('menuitem', { name: 'Rename', exact: true });
        await waitFor(() => expect(item.getAttribute('aria-disabled')).toBe('true'));
        expect(item.title).toBe('Archived resources cannot be renamed');
        fireEvent.click(item);
        expect(screen.queryByRole('dialog')).toBeNull();
        expect(submitted(fetched)).toHaveLength(0);
    });

    it('keeps the dialog mounted after selection and prevents duplicate pending submissions', async () => {
        const fetched = await openScreen({ pending: true });
        await renameFromMenu(screen.getAllByRole('button', { name: 'Row actions' })[0], 'Pending name');
        const dialog = await screen.findByRole('dialog', { name: 'Rename', exact: true });
        expect(screen.queryByRole('menu')).toBeNull();
        fireEvent.click(within(dialog).getByRole('button', { name: 'Rename', exact: true }));
        await waitFor(() => expect(submitted(fetched)).toHaveLength(1));
    });

    it('opens by keyboard and dismisses with Escape, restoring trigger focus', async () => {
        await openScreen();
        const trigger = screen.getAllByRole('button', { name: 'Row actions' })[0];
        trigger.focus();
        fireEvent.keyDown(trigger, { key: 'ArrowDown' });
        const menu = await screen.findByRole('menu');
        fireEvent.keyDown(menu, { key: 'Escape' });
        await waitFor(() => expect(screen.queryByRole('menu')).toBeNull());
        await waitFor(() => expect(document.activeElement).toBe(trigger));
        expect(screen.getByText('Fjord').closest('tr')?.getAttribute('aria-selected')).toBe('false');
    });

    it('activates a menu item with the keyboard without selecting the table row', async () => {
        await openScreen();
        const trigger = screen.getAllByRole('button', { name: 'Row actions' })[1];
        trigger.focus();
        fireEvent.keyDown(trigger, { key: 'ArrowDown' });
        const menu = await screen.findByRole('menu');
        fireEvent.keyDown(menu, { key: 'End' });
        fireEvent.keyDown(menu, { key: 'Enter' });
        expect(await screen.findByRole('dialog', { name: 'Archive', exact: true })).toBeDefined();
        expect(screen.getByText('Mountain').closest('tr')?.getAttribute('aria-selected')).toBe('false');
    });

    it('collects only exact item opt-ins and never visits an outlet screen', () => {
        const page = element('page', 'core:screen', {}, { content: actions });
        expect(screenRowMenuActionsOf(page).map(action => action.properties.label)).toEqual(['Rename', 'Archive']);
        const outlet = element('outlet', 'core:slot', {}, { content: actions });
        expect(screenRowMenuActionsOf(outlet)).toEqual([]);
        expect(actions.filter(isRowMenuAction).map(action => action.properties.actionSubject)).toEqual(['item', 'item']);
    });
});
