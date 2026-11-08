// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { cleanup, fireEvent, render, screen, within } from './testing/render';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core/config';
import { stageTheme } from './stageTheme';
import { App } from './App';
import { registerTestCommand } from './testing/commands';

const element = (id: string, componentName: string, properties: Record<string, unknown> = {}, slots: Record<string, unknown[]> = {}) =>
    ({ id, name: id, componentName, properties, slots });

// The shape the Screenplay converter emits: the screen places an outlet of actions (`core:outletItems`) whose buttons carry where they open.
const toolbar = element('toolbar', 'core:outletItems', {
    name: 'main',
    kind: 'actions',
    items: [
        { kind: 'button', id: 'details', title: 'Details', icon: 'info', navigateTo: { screen: 'InvoiceDetails', slot: 'detail', kind: 'slot', routeParameterBindings: {} } },
        { kind: 'button', id: 'register', title: 'Register invoice', command: 'RegisterInvoice' },
        { kind: 'button', id: 'help', title: 'Help', navigateTo: { kind: 'dialog', dialog: 'HelpDialog', routeParameterBindings: {} } },
    ],
});

const navigationContribution = (label: string, targetScreen: string, properties: Record<string, unknown>, hierarchy: string[]) => ({
    contributionPointName: 'Navigation',
    content: element(`${targetScreen}-nav`, 'core:contribution', { label, targetScreen, hierarchy, ...properties }),
});

const scene = {
    layouts: [],
    screenTemplates: [],
    dialogTemplates: [{
        name: 'HelpDialog',
        displayName: 'Help dialog',
        slots: [],
        content: { content: [element('help-title', 'core:title', { text: 'Help content' })] },
    }],
    screens: [
        {
            name: 'InvoiceList',
            layout: 'AppShell',
            screenTemplate: null,
            forms: [],
            behaviors: [],
            path: ['Invoicing', 'Browsing'],
            contributions: [
                navigationContribution('Invoices', 'InvoiceList', {}, ['Invoicing', 'Browsing']),
                navigationContribution('Help', 'InvoiceList', { icon: 'help', presentation: 'icon', destination: { dialog: 'HelpDialog', kind: 'dialog', routeParameterBindings: {} } }, ['Invoicing']),
            ],
            slotContent: { content: [element('list-title', 'core:title', { text: 'Invoice list' }), toolbar, element('detail-outlet', 'core:slot', { name: 'detail' })] },
        },
        {
            name: 'InvoiceDetails',
            layout: 'AppShell',
            screenTemplate: null,
            forms: [],
            behaviors: [],
            path: ['Invoicing', 'Browsing'],
            contributions: [],
            slotContent: { content: [element('details-title', 'core:title', { text: 'Details screen' })] },
        },
    ],
};

function stubScene() {
    registerTestCommand('RegisterInvoice', '/api/invoices/register');
    vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
        const url = String(input);
        if (url === 'stage/scene') return Promise.resolve({ ok: true, json: () => Promise.resolve(scene) } as Response);
        if (url === 'stage/routes') return Promise.resolve({ ok: true, json: () => Promise.resolve({ commands: { RegisterInvoice: '/api/invoices/register' }, queries: {} }) } as Response);
        return Promise.resolve({ ok: false, json: () => Promise.resolve({}) } as Response);
    }));
}

describe('when the application is hosted by the Scene navigation host', () => {
    afterEach(() => {
        cleanup();
        globalThis.history.replaceState(null, '', '#');
        vi.unstubAllGlobals();
    });

    it('should render an authored toolbar rather than a placeholder', async () => {
        stubScene();
        render(<PrimeReactProvider theme={stageTheme}><App /></PrimeReactProvider>);

        const bar = await screen.findByRole('toolbar', { name: 'main' });
        // Scene draws the buttons that open a destination; the Stage draws the one that runs a command.
        expect(Array.from(bar.querySelectorAll('[data-scene-outlet-item], [data-scene-toolbar-item]'))
            .map(item => item.getAttribute('data-scene-outlet-item') ?? item.getAttribute('data-scene-toolbar-item'))).toEqual(['details', 'register', 'help']);
    });

    // Scene 5 outlet buttons carry no presentation hints (they were cosmetic and are dropped); the icon a button names is drawn.
    it('should draw the icon a toolbar button names', async () => {
        stubScene();
        render(<PrimeReactProvider theme={stageTheme}><App /></PrimeReactProvider>);

        const details = await screen.findByRole('button', { name: 'Details' });
        expect(details.querySelector('[data-scene-icon-name="info"]')).not.toBeNull();
    });

    it('should render a command item as the Stage\'s command action rather than a dead control', async () => {
        stubScene();
        render(<PrimeReactProvider theme={stageTheme}><App /></PrimeReactProvider>);

        const item = await screen.findByText('Register invoice');
        expect(item.closest('[data-scene-toolbar-item="register"]')?.querySelector('.stage-action')).not.toBeNull();
    });

    it('should place a screen in the outlet a toolbar item targets, keeping the screen that declares it', async () => {
        stubScene();
        render(<PrimeReactProvider theme={stageTheme}><App /></PrimeReactProvider>);

        expect(screen.queryByRole('heading', { name: 'Details screen' })).toBeNull();
        fireEvent.click(await screen.findByRole('button', { name: 'Details' }));

        expect(await screen.findByRole('heading', { name: 'Details screen' })).toBeDefined();
        expect(screen.getByRole('heading', { name: 'Invoice list' })).toBeDefined();
        expect(document.querySelector('[data-scene-slot="detail"]')?.getAttribute('data-scene-slot-screen')).toBe('InvoiceDetails');
    });

    it('should open a toolbar dialog through the host and close it again', async () => {
        stubScene();
        render(<PrimeReactProvider theme={stageTheme}><App /></PrimeReactProvider>);

        fireEvent.click(await screen.findByRole('button', { name: 'Help' }));
        const dialog = await screen.findByRole('dialog', { name: 'Help dialog' });
        expect(within(dialog).getByRole('heading', { name: 'Help content' })).toBeDefined();

        fireEvent.click(within(dialog).getByRole('button', { name: 'Close' }));
        await vi.waitFor(() => expect(screen.queryByRole('dialog', { name: 'Help dialog' })).toBeNull());
    });

    it('should close a dialog with Escape and go back to where it was opened from', async () => {
        stubScene();
        render(<PrimeReactProvider theme={stageTheme}><App /></PrimeReactProvider>);

        fireEvent.click(await screen.findByRole('button', { name: 'Help' }));
        const dialog = await screen.findByRole('dialog', { name: 'Help dialog' });
        expect(dialog.getAttribute('aria-modal')).toBe('true');

        fireEvent.keyDown(globalThis as unknown as Element, { key: 'Escape' });
        await vi.waitFor(() => expect(screen.queryByRole('dialog', { name: 'Help dialog' })).toBeNull());
        expect(screen.getByRole('heading', { name: 'Invoice list' })).toBeDefined();
    });

    it('should open a dialog from a navigation entry authored with a dialog destination', async () => {
        stubScene();
        render(<PrimeReactProvider theme={stageTheme}><App /></PrimeReactProvider>);

        const nav = await screen.findByRole('navigation', { name: 'Navigation' });
        const entry = within(nav).getByRole('link', { name: 'Help' });
        expect(entry.getAttribute('data-presentation')).toBe('icon');
        fireEvent.click(entry);

        expect(await screen.findByRole('dialog', { name: 'Help dialog' })).toBeDefined();
    });

    it('should lay a screen no entry lists out under the module and feature it was declared in', async () => {
        stubScene();
        render(<PrimeReactProvider theme={stageTheme}><App /></PrimeReactProvider>);

        const nav = await screen.findByRole('navigation', { name: 'Navigation' });
        const browsing = within(nav).getByText('Browsing').closest('li')!;
        expect(within(browsing).getAllByRole('link').map(link => link.textContent)).toEqual(['Invoices', 'Details screen']);
    });

    it('should deep link to a screen no destination places in an outlet', async () => {
        globalThis.history.replaceState(null, '', '#/InvoiceList');
        stubScene();
        render(<PrimeReactProvider theme={stageTheme}><App /></PrimeReactProvider>);

        expect(await screen.findByRole('heading', { name: 'Invoice list' })).toBeDefined();
        expect(screen.queryByRole('heading', { name: 'Details screen' })).toBeNull();
    });
});
