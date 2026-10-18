// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { cleanup, fireEvent, render, screen, waitFor, within } from './testing/render';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core/config';
import { stageTheme } from './stageTheme';
import { App } from './App';

const element = (id: string, componentName: string, properties: Record<string, unknown> = {}) => ({ id, name: id, componentName, properties, slots: {} });
const screenOf = (name: string, path: string[], title: string) => ({
    name, layout: 'AppShell', screenTemplate: null, forms: [], behaviors: [], path, contributions: [],
    slotContent: { content: [element(`${name}-title`, 'core:title', { text: title })] },
});

// The shape the converter emits for `Integration/Cli/Shell/model`: a layout that declares a `side` slot and a sidebar item that
// renders into it (`item kb ... slot side`).
const scene = {
    layouts: [{ name: 'AppShell', slots: [{ name: 'topbar' }, { name: 'navigation' }, { name: 'content' }, { name: 'side' }] }],
    screenTemplates: [],
    dialogTemplates: [],
    uiProfiles: [],
    structure: { modules: [{ id: 'Support', name: 'Support', features: [
        { id: 'Tickets', name: 'Tickets', features: [], slices: [] },
        { id: 'KnowledgeBase', name: 'KnowledgeBase', features: [], slices: [] },
    ] }] },
    navigation: {
        application: { regions: [{ region: 'topbar', items: [{ id: 'support', title: 'Support', target: { module: 'Support' } }] }] },
        modules: [{ module: 'Support', regions: [{ region: 'sidebar', mode: 'override', items: [
            { id: 'tickets', title: 'Tickets', target: { module: 'Support', feature: 'Tickets' } },
            { id: 'kb', title: 'Knowledge base', target: { module: 'Support', feature: 'KnowledgeBase' }, slot: 'side' },
        ] }] }],
    },
    screens: [screenOf('TicketList', ['Support', 'Tickets'], 'Tickets page'), screenOf('ArticleScreen', ['Support', 'KnowledgeBase'], 'Knowledge base page')],
};

const respond = (payload: unknown) => Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(payload), text: () => Promise.resolve(JSON.stringify(payload)) } as Response);
const sidebar = () => screen.findByRole('navigation', { name: 'Sidebar' });

const active = async () => Array.from((await sidebar()).querySelectorAll('[aria-current]')).map(item => item.textContent?.trim());
const primary = () => document.querySelector('[data-scene-outlet="primary"]')!;
const side = () => document.querySelector('[data-scene-outlet="side"]')!;
const renderStage = () => render(<PrimeReactProvider theme={stageTheme}><App /></PrimeReactProvider>);

describe('when a navigation item names the slot it renders into', () => {
    beforeEach(() => {
        globalThis.history.replaceState(null, '', '#/TicketList');
        vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
            const url = String(input);
            if (url === 'stage/scene') return respond(scene);
            if (url === 'stage/routes') return respond({ commands: {}, queries: {} });
            if (url === 'stage/locales') return respond([]);
            return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) } as Response);
        }));
    });

    afterEach(() => {
        cleanup();
        globalThis.history.replaceState(null, '', '#');
        vi.unstubAllGlobals();
    });

    it('should keep the page in the primary outlet and render the target in a second pane', async () => {
        renderStage();
        await screen.findByText('Tickets page');
        fireEvent.click(within(await sidebar()).getByText('Knowledge base'));

        await waitFor(() => expect(side().textContent).toContain('Knowledge base page'));
        expect(primary().getAttribute('data-scene-screen')).toBe('TicketList');
        expect(primary().textContent).toContain('Tickets page');
        expect(document.querySelector('.layout-wrapper > .layout-side [data-scene-outlet="side"]')).not.toBeNull();
    });

    it('should mark both items active, and only the page once the pane is closed', async () => {
        renderStage();
        await screen.findByText('Tickets page');
        expect(await active()).toEqual(['Tickets']);

        fireEvent.click(within(await sidebar()).getByText('Knowledge base'));
        await waitFor(async () => expect(await active()).toEqual(['Tickets', 'Knowledge base']));

        fireEvent.click(await within(side() as HTMLElement).findByRole('button', { name: 'Close' }));
        await waitFor(async () => expect(await active()).toEqual(['Tickets']));
        expect(side().textContent).not.toContain('Knowledge base page');
        expect(primary().textContent).toContain('Tickets page');
    });

    it('should keep the pane when the page changes', async () => {
        renderStage();
        await screen.findByText('Tickets page');
        fireEvent.click(within(await sidebar()).getByText('Knowledge base'));
        await waitFor(() => expect(side().textContent).toContain('Knowledge base page'));

        fireEvent.click(within(await screen.findByRole('navigation', { name: 'Modules' })).getByText('Support'));
        expect(side().textContent).toContain('Knowledge base page');
    });

    it('should restore both panes after a reload', async () => {
        const first = renderStage();
        await screen.findByText('Tickets page');
        fireEvent.click(within(await sidebar()).getByText('Knowledge base'));
        await waitFor(() => expect(side().textContent).toContain('Knowledge base page'));
        first.unmount();

        renderStage();
        await waitFor(() => expect(side().textContent).toContain('Knowledge base page'));
        expect(primary().textContent).toContain('Tickets page');
    });

    it('should open the pane from a deep link', async () => {
        globalThis.history.replaceState(null, '', '#/ArticleScreen');
        renderStage();

        await waitFor(() => expect(side().textContent).toContain('Knowledge base page'));
        expect(await active()).toContain('Knowledge base');
    });
});
