// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { AvailabilityProvider } from '@cratis/arc.react/availability';
import { FreshQueries } from './testing/freshQueries';
import { registerStageCommands } from '../../src/host/commandRegistration';
import { stageComponents } from './stageComponents';
import { StageDataProvider } from './stageData';
import { element } from './testElements';
import { commandResult } from './testing/commands';

const StageAction = stageComponents['core:action'];
const schema = JSON.stringify({ type: 'object', required: ['assetId'], properties: { assetId: { type: 'string' } } });

// The server answers a command's pre-flight check from the plan of the asset it is asked about.
// An element can also carry its own gate: the marker the Stage published for it is asked about at /.cratis/availability.
function renderAsset(answer: () => Record<string, unknown>, gate?: { reason?: string }) {
    const routes = { commands: { ExportAsset: '/api/export' }, queries: {}, commandSchemas: { ExportAsset: schema }, ...(gate ? { elements: { 'Asset/action/ExportAsset': 'StageElement_export' } } : {}) };
    registerStageCommands(routes);
    const unavailable = gate?.reason ? [{ name: 'StageElement_export', module: '', feature: '', slice: '', reason: gate.reason }] : [];
    const fetched = vi.fn((url: RequestInfo | URL) => Promise.resolve(new Response(JSON.stringify(String(url).includes('/.cratis/availability') ? { isSuccess: true, isAuthorized: true, data: { unavailable } } : String(url).endsWith('/validate') ? commandResult(answer()) : commandResult()), { status: 200 })));
    vi.stubGlobal('fetch', fetched);
    globalThis.location.hash = '#/Asset';
    const item = element('export', 'core:action', { command: 'ExportAsset', label: 'Export', schema, fields: [], generationMode: 'manual' });
    const view = (
        <PrimeReactProvider>
            <StageDataProvider routes={routes} parameters={{ assetId: 'a1' }} locale='en' locales={['en']} screen='Asset'>
                <StageAction element={item} slots={{}} />
            </StageDataProvider>
        </PrimeReactProvider>
    );
    // The application mounts Arc's availability provider when it has a gated element.
    render(gate ? <FreshQueries><AvailabilityProvider>{view}</AvailabilityProvider></FreshQueries> : view);
    return fetched;
}

describe('an action whose command the server refuses for the data on the screen', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); });

    it('is shown unavailable with the reason the server gave', async () => {
        renderAsset(() => ({ isSuccess: false, isAuthorized: false, authorizationFailureReason: 'The plan does not include exports' }));

        await waitFor(() => expect(screen.getByRole('button', { name: 'Export' }).getAttribute('data-stage-availability')).toBe('unavailable'));
        const button = screen.getByRole('button', { name: 'Export' }) as HTMLButtonElement;
        expect(button.disabled).toBe(true);
        expect(button.title).toBe('The plan does not include exports');
    });

    it('is not rendered when its command is not available to the caller', async () => {
        const fetched = renderAsset(() => ({ isSuccess: false, isAuthorized: false, authorizationFailureReason: 'NotAvailable' }));

        await waitFor(() => expect(fetched.mock.calls.some(([url]) => String(url).endsWith('/validate'))).toBe(true));
        await waitFor(() => expect(screen.queryByRole('button', { name: 'Export' })).toBeNull());
        expect(screen.queryByRole('status')).toBeNull();
    });

    it('says it is switched off for now when an operator suspended its command', async () => {
        renderAsset(() => ({ isSuccess: false, isAuthorized: false, authorizationFailureReason: 'Suspended' }));

        await waitFor(() => expect(screen.getByRole('status').textContent).toBe('Back soon'));
        expect(screen.queryByRole('button', { name: 'Export' })).toBeNull();
    });

    it('is not rendered when its own gate is not available', async () => {
        renderAsset(() => ({}), { reason: 'NotAvailable' });

        await waitFor(() => expect(screen.queryByRole('button', { name: 'Export' })).toBeNull());
        expect(screen.queryByRole('status')).toBeNull();
    });

    it('says it is switched off for now when an operator suspended its own gate', async () => {
        renderAsset(() => ({}), { reason: 'Suspended' });

        await waitFor(() => expect(screen.getByRole('status').textContent).toBe('Back soon'));
    });

    it('is offered when its own gate is open', async () => {
        renderAsset(() => ({}), {});

        await waitFor(() => expect(screen.getByRole('button', { name: 'Export' })).toBeTruthy());
    });

    it('is not offered before its own gate has answered', async () => {
        vi.stubGlobal('fetch', vi.fn(() => new Promise(() => undefined)));
        const routes = { commands: { ExportAsset: '/api/export' }, queries: {}, commandSchemas: { ExportAsset: schema }, elements: { 'Asset/action/ExportAsset': 'StageElement_export' } };
        registerStageCommands(routes);
        const item = element('export', 'core:action', { command: 'ExportAsset', label: 'Export', schema, fields: [], generationMode: 'manual' });
        render(<FreshQueries><AvailabilityProvider><PrimeReactProvider><StageDataProvider routes={routes} parameters={{ assetId: 'a1' }} locale='en' locales={['en']} screen='Asset'><StageAction element={item} slots={{}} /></StageDataProvider></PrimeReactProvider></AvailabilityProvider></FreshQueries>);

        expect(screen.queryByRole('button', { name: 'Export' })).toBeNull();
    });

    it('stays offered when the server authorizes it', async () => {
        const fetched = renderAsset(() => ({}));

        await waitFor(() => expect(fetched.mock.calls.some(([url]) => String(url).endsWith('/validate'))).toBe(true));
        expect(screen.getByRole('button', { name: 'Export' }).getAttribute('data-stage-availability')).toBeNull();
    });
});
