// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { InteractionActionKind, InteractionTriggerKind } from '@cratis/scene.model';
import { App } from './App';

const sceneWithAnInteraction = {
    layouts: [],
    screenTemplates: [],
    screens: [
        {
            name: 'InvoiceList',
            layout: 'AppShell',
            screenTemplate: null,
            forms: [],
            contributions: [],
            behaviors: [],
            slotContent: {
                content: [
                    {
                        id: 'cancel',
                        name: 'cancel',
                        componentName: 'core:button',
                        properties: { label: 'Cancel invoice' },
                        slots: {},
                        behaviors: [
                            {
                                name: 'CancelTheInvoice',
                                bindings: [
                                    {
                                        trigger: { kind: InteractionTriggerKind.Click },
                                        actions: [{ kind: InteractionActionKind.ExecuteCommand, command: 'CancelInvoice' }],
                                    },
                                ],
                            },
                        ],
                    },
                ],
            },
        },
    ],
};

const routes = { commands: { CancelInvoice: '/api/invoicing/invoices/cancel-invoice/cancel-invoice' }, queries: {} };

function respond(url: string) {
    if (url === 'stage/scene') return Promise.resolve({ ok: true, json: () => Promise.resolve(sceneWithAnInteraction) } as Response);
    if (url === 'stage/routes') return Promise.resolve({ ok: true, json: () => Promise.resolve(routes) } as Response);
    return Promise.resolve({ ok: true, json: () => Promise.resolve({ isSuccess: true }) } as Response);
}

describe('when a modeled interaction is clicked', () => {
    afterEach(() => vi.unstubAllGlobals());

    // The end of the whole chain: a `.play` document said `on click / execute CancelInvoice`, and clicking the
    // rendered button posts to the route this Stage registered for that command. Everything else is plumbing.
    it('should post the modeled command to the route the Stage registered', async () => {
        const fetched = vi.fn((input: RequestInfo | URL) => respond(String(input)));
        vi.stubGlobal('fetch', fetched);

        render(<App />);
        await screen.findByRole('button', { name: 'Cancel invoice' });

        fireEvent.click(screen.getByRole('button', { name: 'Cancel invoice' }));

        await waitFor(() => expect(fetched).toHaveBeenCalledWith(
            '/api/invoicing/invoices/cancel-invoice/cancel-invoice',
            expect.objectContaining({ method: 'POST' })));
    });

    // A command the Stage never registered must say so. Guessing a URL would turn a modelling mistake into a
    // 404 at the worst possible moment, long after the cause.
    it('should report a command this Stage does not serve', async () => {
        const fetched = vi.fn((input: RequestInfo | URL) => {
            const url = String(input);
            if (url === 'stage/routes') return Promise.resolve({ ok: true, json: () => Promise.resolve({ commands: {}, queries: {} }) } as Response);
            return respond(url);
        });
        vi.stubGlobal('fetch', fetched);

        render(<App />);
        await screen.findByRole('button', { name: 'Cancel invoice' });

        fireEvent.click(screen.getByRole('button', { name: 'Cancel invoice' }));

        await screen.findByText(/not registered by this Stage/);
    });
});
