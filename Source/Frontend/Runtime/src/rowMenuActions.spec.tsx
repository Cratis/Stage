// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ExternalComponent } from '@cratis/scene.model';
import { PrimeReactProvider } from '@primereact/core/config';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
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
import emitted from './fixtures/row-menu-actions-scene.json';

// Captured only by and_the_action_is_opted_in with STAGE_UPDATE_VECTORS=1.
// The default C# assertion requires these bytes to equal the current renderer's output.
function componentsOf(node: unknown): ExternalComponent[] {
    if (node === null || typeof node !== 'object') return [];
    const component = 'componentName' in node ? [node as ExternalComponent] : [];
    return [...component, ...Object.values(node).flatMap(componentsOf)];
}

const captured = componentsOf(emitted);
const actions = captured.filter(component => component.componentName === 'core:action');
const capturedTable = captured.find(component => component.componentName === 'core:table')!;
const rows = [
    { resourceId: '11111111-1111-1111-1111-111111111111', name: 'Fjord', organisationId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' },
    { resourceId: '22222222-2222-2222-2222-222222222222', name: 'Mountain', organisationId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb' },
];
const otherRow = { resourceId: '33333333-3333-3333-3333-333333333333', name: 'River' };
const Table = stageComponents['core:table'];
const Action = stageComponents['core:action'];

interface ScreenOptions {
    labels?: string[];
    secondTable?: boolean;
    secondScreen?: boolean;
    noTable?: boolean;
    board?: boolean;
    refused?: boolean;
    pending?: boolean;
    navigating?: boolean;
    optionalReference?: boolean;
    probeable?: boolean;
}

// The acceptance hint/subject comes unchanged from the emitted fixture. This one regression
// extends only its command schema to isolate an optional reference supplied by the selected row.
function withOptionalReference(action: ExternalComponent): ExternalComponent {
    if (action.properties.command !== 'RenameResource') return action;
    const schema = JSON.parse(String(action.properties.schema)) as { properties: Record<string, unknown> };
    const reference = { type: 'string', format: 'uuid', 'x-concept': 'OrganisationId', 'x-reference': { query: 'AllOrganisations', value: 'organisationId', label: 'name' } };
    return { ...action, properties: { ...action.properties, schema: JSON.stringify({ ...schema, properties: { ...schema.properties, organisationId: reference } }) } };
}

function withProbeableSubject(action: ExternalComponent): ExternalComponent {
    if (action.properties.command !== 'RenameResource') return action;
    const schema = JSON.parse(String(action.properties.schema)) as object;
    // Native validation refuses missing required editable inputs before contacting the server.
    // This refusal test needs a command whose row identity is sufficient to run the server probe.
    return { ...action, properties: { ...action.properties, schema: JSON.stringify({ ...schema, required: ['resourceId'] }) } };
}

function fetchedResponse(options: ScreenOptions) {
    return vi.fn((url: RequestInfo | URL, init?: RequestInit) => {
        const path = String(url);
        if (path.endsWith('/validate')) {
            const refused = options.refused;
            return Promise.resolve({ ok: true, json: async () => commandResult(refused ? { isSuccess: false, isAuthorized: false, authorizationFailureReason: 'Archived resources cannot be renamed' } : {}) });
        }
        if (init?.method === 'POST') return options.pending ? new Promise<Response>(() => undefined) : Promise.resolve({ ok: true, json: async () => commandResult() });
        return Promise.resolve({ ok: true, json: async () => queryResult({ data: path.includes('other-resources') ? [otherRow] : rows }) });
    });
}

function screenParts(options: ScreenOptions, offered: ExternalComponent[]) {
    const navigation = options.navigating === false ? {} : { navigateOnRowClickToScreen: 'Details', navigateOnRowClickByParameter: 'resourceId' };
    const table = element('resources', 'core:table', { ...capturedTable.properties, query: 'AllResources', label: 'Resources', dataKey: 'resourceId', ...navigation }, capturedTable.slots);
    const other = element('other-resources', 'core:table', { ...capturedTable.properties, query: 'OtherResources', label: 'Other resources', dataKey: 'resourceId' }, capturedTable.slots);
    const tables = options.noTable || options.board ? [] : [table];
    if (options.secondTable) tables.push(other);
    const board = element('resource-board', 'kanbanBoard', { cards: { kind: 'queryResult', query: 'AllResources' } });
    const sources = options.board ? [board] : tables;
    const page = element('page', 'core:screen', {}, { content: [...sources, ...offered] });
    const unrelated = element('unrelated', 'core:screen', {}, { content: [other] });
    return { tables, other, page, unrelated };
}

function actionsFor(options: ScreenOptions) {
    const referenced = options.optionalReference ? actions.map(withOptionalReference) : actions;
    const available = options.probeable ? referenced.map(withProbeableSubject) : referenced;
    const offered = available.filter(action => !options.labels || options.labels.includes(String(action.properties.label)));
    return { available, offered };
}

async function openScreen(options: ScreenOptions = {}) {
    const { available, offered } = actionsFor(options);
    const commands = { RegisterResource: '/api/register', RenameResource: '/api/rename', ArchiveResource: '/api/archive' };
    const commandSchemas = Object.fromEntries(available.map(action => [String(action.properties.command), String(action.properties.schema)]));
    const routes = { commands, commandSchemas, queries: { AllResources: '/api/resources', OtherResources: '/api/other-resources', AllOrganisations: '/api/organisations' } };
    registerStageCommands(routes);
    registerTestQueries(routes.queries);
    const fetched = fetchedResponse(options);
    vi.stubGlobal('fetch', fetched);
    const { tables, other, page, unrelated } = screenParts(options, offered);
    render(<PrimeReactProvider theme={stageTheme}><StageDataProvider routes={routes} routesReady locale='en' locales={['en']} screen='Resources' parameters={{ resourceId: '44444444-4444-4444-4444-444444444444' }}>
        <StageScreenTables element={page}>
            {tables.map(item => <Table key={item.id} element={item} slots={{}} />)}
            {offered.map(action => <Action key={action.id} element={action} slots={{}} />)}
        </StageScreenTables>
        {options.secondScreen && <StageScreenTables element={unrelated}><Table element={other} slots={{}} /></StageScreenTables>}
    </StageDataProvider></PrimeReactProvider>);
    if (tables.length > 0) await screen.findByText('Mountain');
    if (options.secondTable || options.secondScreen) await screen.findByText('River');
    return fetched;
}

function submitted(fetched: Awaited<ReturnType<typeof openScreen>>) {
    return fetched.mock.calls.filter(([url, init]) => init?.method === 'POST' && !String(url).endsWith('/validate'));
}

async function openRename(trigger: HTMLElement) {
    fireEvent.click(trigger);
    fireEvent.mouseDown(await screen.findByRole('menuitem', { name: 'Rename' }), { button: 0, detail: 1 });
    return screen.findByRole('dialog', { name: 'Rename' });
}

async function renameFromMenu(trigger: HTMLElement, name: string) {
    const dialog = await openRename(trigger);
    fireEvent.change(within(dialog).getByLabelText(/^Name/), { target: { value: name } });
    fireEvent.click(within(dialog).getByRole('button', { name: 'Rename' }));
}

beforeEach(() => {
    // jsdom has no layout observer; menu positioning still runs through the real popup primitive.
    vi.stubGlobal('ResizeObserver', class {
        observe = vi.fn();
        unobserve = vi.fn();
        disconnect = vi.fn();
    });
});

afterEach(() => {
    vi.unstubAllGlobals();
    globalThis.location.hash = '';
    globalThis.localStorage?.clear();
});

describe('the emitted item-action row menu', () => {
    it('opens by mouse, grouping only the opted-in siblings with per-row names and expanded state', async () => {
        await openScreen();
        expect(screen.queryByRole('button', { name: 'Rename' })).toBeNull();
        expect(screen.queryByRole('button', { name: 'Archive' })).toBeNull();
        expect(screen.getByRole('button', { name: 'Create' })).toBeDefined();
        expect((screen.getByRole('button', { name: 'Inline rename' }) as HTMLButtonElement).disabled).toBe(true);
        expect(screen.getByRole('button', { name: 'Advisory rename' })).toBeDefined();
        expect(screen.getByRole('button', { name: 'Unknown placement' })).toBeDefined();
        const trigger = screen.getByRole('button', { name: 'Actions for Fjord' });
        expect(trigger.getAttribute('aria-expanded')).toBe('false');
        expect(screen.getByRole('button', { name: 'Actions for Mountain' }).getAttribute('aria-expanded')).toBe('false');
        fireEvent.click(trigger);
        expect(await screen.findAllByRole('menuitem')).toHaveLength(2);
        expect(trigger.getAttribute('aria-expanded')).toBe('true');
        expect(screen.getAllByRole('menuitem', { name: 'Rename' })).toHaveLength(1);
        expect(screen.getAllByRole('menuitem', { name: 'Archive' })).toHaveLength(1);
    });

    it('adds no menu or actions column without an opted-in item action', async () => {
        await openScreen({ labels: ['Create', 'Inline rename', 'Advisory rename', 'Unknown placement'] });
        expect(screen.queryByRole('button', { name: /^Actions for / })).toBeNull();
        expect(screen.queryByRole('columnheader', { name: 'Actions' })).toBeNull();
    });

    it.each([
        ['two tables', { secondTable: true }],
        ['no table', { noTable: true }],
        ['a board-only screen', { board: true }],
    ] as const)('retains inline actions for %s instead of hiding or copying them', async (_, options) => {
        await openScreen(options);
        expect(screen.queryByRole('button', { name: /^Actions for / })).toBeNull();
        expect(screen.queryByRole('columnheader', { name: 'Actions' })).toBeNull();
        expect(screen.getAllByRole('button', { name: 'Rename' })).toHaveLength(1);
        expect(screen.getAllByRole('button', { name: 'Archive' })).toHaveLength(1);
    });

    it('opens without selecting or navigating the row and submits the clicked row rather than route values', async () => {
        const fetched = await openScreen();
        await renameFromMenu(screen.getByRole('button', { name: 'Actions for Mountain' }), 'New name');
        await waitFor(() => expect(submitted(fetched)).toHaveLength(1));
        expect(JSON.parse(submitted(fetched)[0][1]!.body as string)).toMatchObject({ resourceId: rows[1].resourceId, name: 'New name' });
        expect(screen.getByText('Mountain').closest('tr')?.getAttribute('aria-selected')).toBe('false');
        expect(globalThis.location.hash).not.toContain('Details');
    });

    it('supplies the clicked row B optional reference even while row A remains selected', async () => {
        const fetched = await openScreen({ optionalReference: true, navigating: false });
        fireEvent.click(screen.getByText('Fjord'));
        await waitFor(() => expect(screen.getByText('Fjord').closest('tr')?.getAttribute('aria-selected')).toBe('true'));
        await renameFromMenu(screen.getByRole('button', { name: 'Actions for Mountain' }), 'Mountain renamed');
        await waitFor(() => expect(submitted(fetched)).toHaveLength(1));
        expect(JSON.parse(submitted(fetched)[0][1]!.body as string)).toMatchObject({ resourceId: rows[1].resourceId, organisationId: rows[1].organisationId });
        expect(screen.getByText('Fjord').closest('tr')?.getAttribute('aria-selected')).toBe('true');
    });

    it('does not leak menus into another screen', async () => {
        await openScreen({ secondScreen: true });
        const other = screen.getByRole('table', { name: 'Other resources' });
        expect(within(other).queryByRole('button', { name: /^Actions for / })).toBeNull();
    });

    it('retains the server refusal and reason on the menu item', async () => {
        const fetched = await openScreen({ refused: true, probeable: true });
        fireEvent.click(screen.getByRole('button', { name: 'Actions for Mountain' }));
        await waitFor(() => expect(screen.getByRole('menuitem', { name: 'Rename' }).getAttribute('aria-disabled')).toBe('true'));
        const item = screen.getByRole('menuitem', { name: 'Rename' });
        expect(item.title).toBe('Archived resources cannot be renamed');
        fireEvent.mouseDown(item, { button: 0, detail: 1 });
        expect(screen.queryByRole('dialog')).toBeNull();
        expect(submitted(fetched)).toHaveLength(0);
    });

    it('keeps the dialog mounted after selection and prevents duplicate pending submissions', async () => {
        const fetched = await openScreen({ pending: true });
        await renameFromMenu(screen.getByRole('button', { name: 'Actions for Fjord' }), 'Pending name');
        const dialog = await screen.findByRole('dialog', { name: 'Rename' });
        expect(screen.queryByRole('menu')).toBeNull();
        fireEvent.click(within(dialog).getByRole('button', { name: 'Rename' }));
        await waitFor(() => expect(submitted(fetched)).toHaveLength(1));
    });

    it('opens by keyboard and dismisses with Escape, restoring trigger focus', async () => {
        await openScreen();
        const trigger = screen.getByRole('button', { name: 'Actions for Fjord' });
        trigger.focus();
        fireEvent.keyDown(trigger, { key: 'ArrowDown' });
        const menu = await screen.findByRole('menu');
        expect(await screen.findByRole('menuitem', { name: 'Rename' })).toBeDefined();
        expect(trigger.getAttribute('aria-expanded')).toBe('true');
        fireEvent.keyDown(menu, { key: 'Escape' });
        await waitFor(() => expect(screen.queryByRole('menu')).toBeNull());
        expect(trigger.getAttribute('aria-expanded')).toBe('false');
        await waitFor(() => expect(document.activeElement).toBe(trigger));
        expect(screen.getByText('Fjord').closest('tr')?.getAttribute('aria-selected')).toBe('false');
    });

    it('activates a menu item with the keyboard without selecting the table row', async () => {
        await openScreen();
        const trigger = screen.getByRole('button', { name: 'Actions for Mountain' });
        trigger.focus();
        fireEvent.keyDown(trigger, { key: 'ArrowDown' });
        const menu = await screen.findByRole('menu');
        await screen.findByRole('menuitem', { name: 'Archive' });
        fireEvent.keyDown(menu, { key: 'End' });
        fireEvent.keyDown(menu, { key: 'Enter' });
        expect(await screen.findByRole('dialog', { name: 'Archive' })).toBeDefined();
        expect(screen.getByText('Mountain').closest('tr')?.getAttribute('aria-selected')).toBe('false');
    });

    it('returns focus to the named row trigger after the command dialog closes', async () => {
        await openScreen();
        const trigger = screen.getByRole('button', { name: 'Actions for Mountain' });
        const dialog = await openRename(trigger);
        await waitFor(() => expect(trigger.getAttribute('aria-expanded')).toBe('false'));
        fireEvent.click(within(dialog).getByRole('button', { name: /^Close/ }));
        await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull());
        await waitFor(() => expect(document.activeElement).toBe(trigger));
    });

    it('collects only exact item opt-ins from the single table screen, not from outlets', () => {
        const page = element('page', 'core:screen', {}, { content: [capturedTable, ...actions] });
        expect(screenRowMenuActionsOf(page).map(action => action.properties.label)).toEqual(['Rename', 'Archive']);
        const outlet = element('outlet', 'core:slot', {}, { content: [capturedTable, ...actions] });
        expect(screenRowMenuActionsOf(outlet)).toEqual([]);
        expect(actions.filter(isRowMenuAction).map(action => action.properties.actionSubject)).toEqual(['item', 'item']);
    });
});
