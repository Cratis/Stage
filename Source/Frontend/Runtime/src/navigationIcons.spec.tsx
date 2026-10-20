// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { cleanup, fireEvent, render, screen, waitFor, within } from './testing/render';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core/config';
import { componentIcon } from '@cratis/scene.model';
import { App } from './App';
import { stageTheme } from './stageTheme';
import { stageLucideAdapter } from './stageIcons';

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
        { id: 'MissingIcon', name: 'MissingIcon', features: [], slices: [] },
        { id: 'NoIcon', name: 'NoIcon', features: [], slices: [] },
    ] }] },
    navigation: {
        application: { regions: [{ region: 'topbar', items: [{ id: 'support', title: 'Support', icon: 'headset', target: { module: 'Support' } }], buttons: [{ id: 'add', title: 'Add box', icon: 'plus' }] }] },
        modules: [{ module: 'Support', regions: [{ region: 'sidebar', mode: 'override', items: [
            { id: 'tickets', title: 'Tickets', icon: 'box', target: { module: 'Support', feature: 'Tickets' } },
            { id: 'kb', title: 'Knowledge base', icon: 'Cratis.Icons.Lucide#package', target: { module: 'Support', feature: 'KnowledgeBase' }, slot: 'side' },
            { id: 'missing-icon', title: 'Unknown icon', icon: 'not-a-lucide-icon', target: { module: 'Support', feature: 'MissingIcon' } },
            { id: 'no-icon', title: 'Without icon', target: { module: 'Support', feature: 'NoIcon' } },
        ] }] }],
    },
    screens: [
        screenOf('TicketList', ['Support', 'Tickets'], 'Tickets page'),
        screenOf('ArticleScreen', ['Support', 'KnowledgeBase'], 'Knowledge base page'),
        screenOf('MissingIconScreen', ['Support', 'MissingIcon'], 'Unknown icon page'),
        screenOf('NoIconScreen', ['Support', 'NoIcon'], 'Without icon page'),
    ],
};

const respond = (payload: unknown) => Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(payload) } as Response);

describe('when Play draws authored navigation icons', () => {
    beforeEach(() => {
        globalThis.history.replaceState(null, '', '#/TicketList');
        vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
            if (String(input) === 'stage/scene') return respond(scene);
            if (String(input) === 'stage/routes') return respond({ commands: {}, queries: {} });
            if (String(input) === 'stage/locales') return respond([]);
            return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) } as Response);
        }));
    });

    afterEach(() => {
        cleanup();
        globalThis.history.replaceState(null, '', '#');
        vi.unstubAllGlobals();
    });

    it('should draw Lucide glyphs for bare and qualified sidebar icons', async () => {
        render(<PrimeReactProvider theme={stageTheme}><App /></PrimeReactProvider>);

        const sidebar = await screen.findByRole('navigation', { name: 'Sidebar' });
        for (const title of ['Tickets', 'Knowledge base']) {
            const item = within(sidebar).getByRole('link', { name: title });
            expect(item.closest('[data-scene-nav-item]')?.hasAttribute('data-scene-iconless')).toBe(false);
            await waitFor(() => expect(item.querySelector('[data-scene-icon] svg')).not.toBeNull());
        }
    });

    it.each([
        ['Unknown icon', 'Unknown icon page'],
        ['Without icon', 'Without icon page'],
    ])('should preserve the label and navigation for %s', async (title, page) => {
        render(<PrimeReactProvider theme={stageTheme}><App /></PrimeReactProvider>);

        const sidebar = await screen.findByRole('navigation', { name: 'Sidebar' });
        const item = within(sidebar).getByRole('link', { name: title });
        expect(item.textContent).toContain(title);
        fireEvent.click(item);

        await waitFor(() => expect(document.querySelector('[data-scene-outlet="primary"]')?.textContent).toContain(page));
    });

    it('should draw a topbar button with both its icon and label', async () => {
        render(<PrimeReactProvider theme={stageTheme}><App /></PrimeReactProvider>);

        const button = await screen.findByRole('button', { name: 'Add box' });
        expect(button.textContent).toContain('Add box');
        await waitFor(() => expect(button.querySelector('[data-scene-icon="plus"] svg')).not.toBeNull());
    });
});

describe('the Stage Lucide adapter', () => {
    it('should load the box glyph', async () => {
        expect(await stageLucideAdapter.loadGlyph(componentIcon('box'))).toBeDefined();
    });

    it.each(['Box', 'ShoppingCart', 'shoppingCart', 'shopping cart', 'shopping_cart'])('should normalize %s before loading its glyph', async (key) => {
        const expected = key === 'Box' ? 'box' : 'shopping-cart';
        const glyph = await stageLucideAdapter.loadGlyph(componentIcon(key));
        expect(glyph).toBeDefined();
        expect(glyph).toBe(await stageLucideAdapter.loadGlyph(componentIcon(expected)));
    });

    it.each([
        ['chart', 'chart-column'],
        ['add', 'plus'],
        ['dollar', 'dollar-sign'],
        ['sitemap', 'network'],
        ['refresh', 'refresh-cw'],
        ['invoice', 'receipt-text'],
        ['batch', 'layers'],
    ])('should resolve the authored alias %s to %s', async (key, target) => {
        const glyph = await stageLucideAdapter.loadGlyph(componentIcon(key));
        expect(glyph).toBeDefined();
        expect(glyph).toBe(await stageLucideAdapter.loadGlyph(componentIcon(target)));
    });

    it.each(['not-a-lucide-icon', 'toString', 'constructor'])('should leave the unknown icon %s unavailable', async (key) => {
        expect(await stageLucideAdapter.loadGlyph(componentIcon(key))).toBeUndefined();
    });
});
