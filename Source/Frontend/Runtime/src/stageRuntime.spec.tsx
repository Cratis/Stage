// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, cleanup, fireEvent, render, screen, waitFor, within } from './testing/render';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core/config';
import { stageTheme } from './stageTheme';
import type { EffectiveNavItem } from '@cratis/scene.engine';
import { App } from './App';
import { registerTestCommand, commandResult } from './testing/commands';

const element = (id: string, componentName: string, properties: Record<string, unknown> = {}, slots: Record<string, unknown[]> = {}) =>
    ({ id, name: id, componentName, properties, slots });

const page = (title: string, content: unknown[] = []) => element(`${title}-page`, 'core:title', { text: title }, { content });

const screenOf = (name: string, path: string[], content: unknown[]) => ({
    name, layout: 'AppShell', screenTemplate: null, forms: [], behaviors: [], contributions: [], path, slotContent: { content },
});

// The shape the Screenplay converter emits for a model with navigation regions: modules and features with a slice each, and
// the application's regions with a module overriding its sidebar.
const scene = {
    layouts: [],
    screenTemplates: [],
    dialogTemplates: [],
    uiProfiles: [],
    structure: {
        modules: [
            { id: 'Dashboard', name: 'Dashboard', features: [{ id: 'Overview', name: 'Overview', features: [], slices: [] }] },
            {
                id: 'Sales', name: 'Sales', features: [
                    { id: 'Orders', name: 'Orders', features: [], slices: [{ id: 'PlaceOrder', name: 'Place order', kind: 'StateChange', command: 'PlaceOrder' }] },
                    { id: 'Customers', name: 'Customers', features: [], slices: [] },
                    { id: 'Reports', name: 'Reports', features: [], slices: [] },
                ],
            },
        ],
    },
    navigation: {
        application: {
            logo: { image: '', title: 'Acme' },
            regions: [
                { region: 'topbar', items: [{ id: 'dashboard', title: 'Dashboard', target: { module: 'Dashboard' } }, { id: 'sales', title: 'Sales', target: { module: 'Sales' } }] },
                { region: 'sidebar', items: [{ id: 'overview', title: 'Overview', target: { module: 'Dashboard', feature: 'Overview' } }] },
            ],
        },
        modules: [{
            module: 'Sales',
            regions: [{ region: 'sidebar', mode: 'override', items: [
                { id: 'orders', title: 'Orders', target: { module: 'Sales', feature: 'Orders' } },
                { id: 'customers', title: 'Customers', target: { module: 'Sales', feature: 'Customers' } },
                { id: 'reports', title: 'Reports', target: { module: 'Sales', feature: 'Reports' } },
            ] }],
        }],
    },
    commandSchemas: {
        PlaceOrder: JSON.stringify({ type: 'object', properties: { customer: { type: 'string' }, quantity: { type: 'integer' }, tags: { type: 'array', items: { type: 'string' } } }, required: ['customer', 'quantity'] }),
    },
    screens: [
        screenOf('Overview', ['Dashboard', 'Overview'], [page('Overview page')]),
        screenOf('OrderList', ['Sales', 'Orders'], [
            page('Orders page'),
            element('orders-toolbar', 'core:toolbar', { name: 'orders' }, { items: [
                element('new-order', 'core:toolbarItem', { name: 'new', kind: 'Action', label: 'New order', slice: 'Sales.Orders.PlaceOrder', emphasis: 'primary' }),
            ] }),
        ]),
        screenOf('CustomerList', ['Sales', 'Customers'], [page('Customers page')]),
        screenOf('ReportList', ['Sales', 'Reports'], [page('Reports page')]),
    ],
};

function respond(payload: unknown, ok = true, status = 200): Promise<Response> {
    return Promise.resolve({ ok, status, json: () => Promise.resolve(payload), text: () => Promise.resolve(JSON.stringify(payload)) } as Response);
}

let posted: { url: string; body: unknown }[];

function stubStage(sceneDocument: unknown = scene, command: (body: unknown) => Promise<Response> = () => respond(commandResult())) {
    registerTestCommand('PlaceOrder', '/api/sales/place-order', { customer: { type: 'string' }, quantity: { type: 'integer' } });
    posted = [];
    vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        if (url === 'stage/scene') return respond(sceneDocument);
        if (url === 'stage/routes') return respond({ commands: { PlaceOrder: '/api/sales/place-order' }, queries: {} });
        if (url === 'stage/locales') return respond([]);
        if (url === 'http://localhost:3000/api/sales/place-order' && init?.method === 'POST') {
            const body = JSON.parse(String(init.body));
            posted.push({ url, body });
            return command(body);
        }
        return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) } as Response);
    }));
}

const labels = (nav: HTMLElement) => Array.from(nav.querySelectorAll('a, button')).map(item => item.textContent?.trim());
const renderStage = (isVisible?: (item: EffectiveNavItem) => boolean) => render(<PrimeReactProvider theme={stageTheme}><App isVisible={isVisible} /></PrimeReactProvider>);
const topbar = () => screen.findByRole('navigation', { name: 'Modules' });
const sidebar = () => screen.findByRole('navigation', { name: 'Sidebar' });

describe('the running Stage with navigation regions', () => {
    beforeEach(() => {
        globalThis.history.replaceState(null, '', '#/Overview');
        stubStage();
    });

    afterEach(() => {
        cleanup();
        globalThis.history.replaceState(null, '', '#');
        vi.unstubAllGlobals();
    });

    it('draws the application topbar and the sidebar of the application as the shell regions', async () => {
        renderStage();
        expect(labels(await topbar())).toEqual(['Dashboard', 'Sales']);
        expect(labels(await sidebar())).toEqual(['Overview']);
        expect(document.querySelector('.layout-logo')?.textContent).toContain('Acme');
    });

    it('swaps the sidebar for the effective navigation of the module it goes to, and renders its first feature', async () => {
        renderStage();
        fireEvent.click(within(await topbar()).getByText('Sales'));

        await waitFor(() => expect(screen.getByText('Orders page')).toBeDefined());
        expect(labels(await sidebar())).toEqual(['Orders', 'Customers', 'Reports']);
    });

    it('marks the module and the feature of the page the user is on', async () => {
        renderStage();
        fireEvent.click(within(await topbar()).getByText('Sales'));
        await screen.findByText('Orders page');

        expect(within(await topbar()).getByText('Sales').closest('[aria-current]')?.getAttribute('aria-current')).toBe('page');
        expect(within(await sidebar()).getByText('Orders').closest('[aria-current]')?.getAttribute('aria-current')).toBe('page');
    });

    it('records where the user is in the address and restores it from a deep link', async () => {
        renderStage();
        fireEvent.click(within(await topbar()).getByText('Sales'));
        fireEvent.click(within(await sidebar()).getByText('Customers'));
        await screen.findByText('Customers page');
        expect(globalThis.location.hash).toContain('CustomerList');
        cleanup();

        renderStage();
        expect(await screen.findByText('Customers page')).toBeDefined();
        expect(labels(await sidebar())).toEqual(['Orders', 'Customers', 'Reports']);
    });

    it('hides the items the user may not see, and does not disable them', async () => {
        renderStage(item => item.title !== 'Reports');
        fireEvent.click(within(await topbar()).getByText('Sales'));

        expect(labels(await sidebar())).toEqual(['Orders', 'Customers']);
        expect(screen.queryByText('Reports')).toBeNull();
    });

    describe('and a toolbar button names a State Change slice', () => {
        beforeEach(async () => {
            renderStage();
            fireEvent.click(within(await topbar()).getByText('Sales'));
            fireEvent.click(await screen.findByRole('button', { name: 'New order' }));
        });

        it('opens the slice as a task dialog whose fields are the command\'s schema, without what the form cannot present', async () => {
            const dialog = await screen.findByRole('dialog');
            expect(within(dialog).getByRole('heading', { name: 'New order' })).toBeDefined();
            expect(within(dialog).getByLabelText('Customer', { exact: false })).toBeDefined();
            expect(within(dialog).getByLabelText('Quantity', { exact: false })).toBeDefined();
            expect(within(dialog).queryByLabelText('Tags', { exact: false })).toBeNull();
        });

        it('posts the command to the route the Stage registered, through the real invoker, and closes', async () => {
            const dialog = await screen.findByRole('dialog');
            fireEvent.change(within(dialog).getByLabelText('Customer', { exact: false }), { target: { value: 'Fjordline AS' } });
            fireEvent.change(within(dialog).getByLabelText('Quantity', { exact: false }), { target: { value: '4' } });
            await act(async () => { fireEvent.click(dialog.querySelector('.scene-slice-dialog-submit')!); });

            await waitFor(() => expect(posted).toEqual([{ url: 'http://localhost:3000/api/sales/place-order', body: { customer: 'Fjordline AS', quantity: 4 } }]));
            await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull());
        });
    });

    describe('and the command is rejected by the application', () => {
        it('shows the validation message beside the field and keeps the dialog open', async () => {
            stubStage(scene, () => respond(commandResult({ isSuccess: false, isValid: false, validationResults: [{ severity: 3, message: 'Quantity must be positive', members: ['quantity'], state: null }] })));
            renderStage();
            fireEvent.click(within(await topbar()).getByText('Sales'));
            fireEvent.click(await screen.findByRole('button', { name: 'New order' }));
            const dialog = await screen.findByRole('dialog');
            fireEvent.change(within(dialog).getByLabelText('Customer', { exact: false }), { target: { value: 'Fjordline AS' } });
            fireEvent.change(within(dialog).getByLabelText('Quantity', { exact: false }), { target: { value: '-1' } });
            await act(async () => { fireEvent.click(dialog.querySelector('.scene-slice-dialog-submit')!); });

            expect(await within(dialog).findByText('Quantity must be positive')).toBeDefined();
            expect(screen.getByRole('dialog')).toBeDefined();
        });
    });
});

describe('the running Stage with a model that declares no navigation regions', () => {
    afterEach(() => {
        cleanup();
        globalThis.history.replaceState(null, '', '#');
        vi.unstubAllGlobals();
    });

    it('keeps what it always had: the menu of its screens, and no region of its own in the topbar or sidebar', async () => {
        globalThis.history.replaceState(null, '', '#/Overview');
        const { navigation: _navigation, structure: _structure, ...plain } = scene;
        stubStage(plain);
        renderStage();

        await screen.findByText('Overview page');
        expect(screen.queryByRole('navigation', { name: 'Modules' })).toBeNull();
        expect(screen.queryByRole('navigation', { name: 'Sidebar' })).toBeNull();
    });
});
