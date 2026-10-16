// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { ApplicationHost, type ApplicationDocument } from '@cratis/scene.blueprint.default';
import { stageNavigationModel } from './stageNavigationModel';
import { stageProfile, stageRegistry } from './blueprint';
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

// What the user sees after a click: which page is in the primary outlet, what the side pane shows, which items are marked, and whether
// the pane offers to close. The Stage and the Scene preview host compose through the same `ApplicationHost`, so these are the same.
async function observed() {
    const outlet = (name: string) => document.querySelector(`[data-scene-outlet="${name}"]`);
    return {
        page: outlet('primary')?.getAttribute('data-scene-screen'),
        side: outlet('side')?.getAttribute('data-scene-outlet-screen') ?? null,
        marked: Array.from((await sidebar()).querySelectorAll('[aria-current]')).map(item => item.textContent?.trim()),
        closable: screen.queryByRole('button', { name: 'Close' }) !== null,
    };
}

const clicks = ['Knowledge base', 'Tickets'];

describe('when the same navigation is clicked in the Stage and in the Scene preview host', () => {
    afterEach(() => {
        cleanup();
        globalThis.history.replaceState(null, '', '#');
        vi.unstubAllGlobals();
    });

    async function inStage() {
        globalThis.history.replaceState(null, '', '#/TicketList');
        vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
            const url = String(input);
            if (url === 'stage/scene') return respond(scene);
            if (url === 'stage/routes') return respond({ commands: {}, queries: {} });
            if (url === 'stage/locales') return respond([]);
            return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) } as Response);
        }));
        const shown = render(<PrimeReactProvider theme={stageTheme}><App /></PrimeReactProvider>);
        await screen.findByText('Tickets page');
        return shown;
    }

    function inPreviewHost() {
        const document_ = scene as unknown as ApplicationDocument;
        return render(
            <PrimeReactProvider theme={stageTheme}>
                <ApplicationHost
                    application={document_}
                    composition={{ profile: stageProfile(undefined) }}
                    navigationModel={stageNavigationModel(scene as never)}
                    registry={stageRegistry({})}
                    initialScreen='TicketList'
                    layoutConfigStorage={null}
                />
            </PrimeReactProvider>);
    }

    async function afterClicking(host: () => Promise<unknown> | unknown) {
        await host();
        await sidebar();
        const seen = [await observed()];
        for (const label of clicks) {
            fireEvent.click(within(await sidebar()).getByText(label));
            await waitFor(async () => expect((await observed()).marked).toContain(label));
            seen.push(await observed());
        }

        return seen;
    }

    it('should show the same pages, panes and marked items after every click', async () => {
        const stage = await afterClicking(inStage);
        cleanup();
        const preview = await afterClicking(inPreviewHost);

        expect(preview).toEqual(stage);
        // And what they agree on is the behaviour: the page stays, the pane opens beside it, and a click on the page's own item replaces the page.
        expect(stage[1]).toEqual({ page: 'TicketList', side: 'ArticleScreen', marked: ['Tickets', 'Knowledge base'], closable: true });
    });
});
