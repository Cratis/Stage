// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { registerStageCommands } from '../../src/host/commandRegistration';
import { schemaPropertiesOf, sessionIdentifiers } from './StageCommandForm';
import { stageComponents } from './stageComponents';
import { StageDataProvider } from './stageData';
import { element } from './testElements';
import { commandResult } from './testing/commands';

const StageToolbarItem = stageComponents['core:toolbarItem'];
const schema = JSON.stringify({
    type: 'object', required: ['projectId', 'archiveReason'], properties: {
        projectId: { type: 'string', format: 'uuid', 'x-concept': 'OtherProjectId' },
        archiveReason: { type: 'string' },
    },
});
const routes = { commands: { ArchiveProject: '/api/other-projects/archive' }, queries: {}, commandSchemas: { ArchiveProject: schema } };

function showFallback() {
    registerStageCommands(routes);
    const fetched = vi.fn<typeof fetch>().mockResolvedValue(new Response(JSON.stringify(commandResult()), { status: 200 }));
    vi.stubGlobal('fetch', fetched);
    render(
        <PrimeReactProvider>
            <StageDataProvider routes={routes} locale='en' locales={['en']} screen='Fallback'>
                <StageToolbarItem element={element('fallback', 'core:toolbarItem', { kind: 'Action', target: 'ArchiveProject', label: 'Fallback secondary' })} slots={{}} />
            </StageDataProvider>
        </PrimeReactProvider>,
    );
    return fetched;
}

async function openFallback() {
    fireEvent.click(screen.getByRole('button', { name: 'Fallback secondary' }));
    return screen.findByRole('dialog');
}

describe('a fallback action without compatible query rows', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); });

    it('reuses and hides the concept identity created by an earlier unmarked inline form, as on base', async () => {
        // Same fields/schema/initial query state as the retained fixture before its table publishes any rows.
        const initial = sessionIdentifiers([], schemaPropertiesOf(schema), {});
        expect(initial.projectId).toBe(localStorage.getItem('stage.session.OtherProjectId'));
        expect(initial.projectId).toMatch(/^[0-9a-f-]{36}$/);
        const fetched = showFallback();
        const dialog = await openFallback();
        expect(within(dialog).queryByLabelText('Project id')).toBeNull();
        fireEvent.change(within(dialog).getByLabelText('Archive reason'), { target: { value: 'Inherited identity' } });
        fireEvent.click(within(dialog).getByRole('button', { name: 'Execute Fallback secondary' }));
        await waitFor(() => expect(fetched).toHaveBeenCalledTimes(1));
        expect(JSON.parse(String(fetched.mock.calls[0][1]?.body))).toEqual({ projectId: initial.projectId, archiveReason: 'Inherited identity' });
    });

    it('asks for the identifier in a clean session and sends no POST while it is missing', async () => {
        const fetched = showFallback();
        const dialog = await openFallback();
        const identifier = within(dialog).getByLabelText('Project id');
        expect(identifier.isConnected).toBe(true);
        expect(identifier.hidden).toBe(false);
        fireEvent.change(within(dialog).getByLabelText('Archive reason'), { target: { value: 'Retained reason' } });
        await act(async () => { fireEvent.click(within(dialog).getByRole('button', { name: 'Execute Fallback secondary' })); });
        expect(fetched).not.toHaveBeenCalled();
        expect(screen.getByRole('dialog')).toBe(dialog);
        expect((within(dialog).getByLabelText('Archive reason') as HTMLInputElement).value).toBe('Retained reason');
        fireEvent.change(identifier, { target: { value: '7d1c8f3a-0000-4000-8000-000000000001' } });
        fireEvent.click(within(dialog).getByRole('button', { name: 'Execute Fallback secondary' }));
        await waitFor(() => expect(fetched).toHaveBeenCalledTimes(1));
        expect(JSON.parse(String(fetched.mock.calls[0][1]?.body))).toEqual({ projectId: '7d1c8f3a-0000-4000-8000-000000000001', archiveReason: 'Retained reason' });
    });
});
