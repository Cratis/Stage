// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { registerStageCommands } from '../../src/host/commandRegistration';
import { stageComponents } from './stageComponents';
import { StageDataProvider } from './stageData';
import { element } from './testElements';
import { commandResult } from './testing/commands';

const StageAction = stageComponents['core:action'];
const schema = JSON.stringify({ type: 'object', required: ['assetId'], properties: { assetId: { type: 'string' } } });

// The server answers a command's pre-flight check from the plan of the asset it is asked about.
function renderAsset(answer: () => Record<string, unknown>) {
    const routes = { commands: { ExportAsset: '/api/export' }, queries: {}, commandSchemas: { ExportAsset: schema } };
    registerStageCommands(routes);
    const fetched = vi.fn((url: RequestInfo | URL) => Promise.resolve({ ok: true, json: async () => String(url).endsWith('/validate') ? commandResult(answer()) : commandResult() }));
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
    render(view);
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

    it('stays offered when the server authorizes it', async () => {
        const fetched = renderAsset(() => ({}));

        await waitFor(() => expect(fetched.mock.calls.some(([url]) => String(url).endsWith('/validate'))).toBe(true));
        expect(screen.getByRole('button', { name: 'Export' }).getAttribute('data-stage-availability')).toBeNull();
    });
});
