// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { detail, renderBrowser, response, serve } from './given/a_browser';

afterEach(() => vi.unstubAllGlobals());

describe('when the read-model request is unavailable', () => {
    it('should show a loading state while waiting', () => {
        vi.stubGlobal('fetch', vi.fn(() => new Promise<Response>(() => undefined)));
        renderBrowser();
        expect(screen.getByRole('status').textContent).toContain('Loading read models');
    });
    it('should explain a world that has not been rebuilt', async () => {
        vi.stubGlobal('fetch', vi.fn(async () => response({ state: 'loading' }, 503)));
        renderBrowser();
        expect((await screen.findByRole('alert')).textContent).toContain('still loading');
        expect(screen.queryByRole('table')).toBeNull();
    });
    it('should show the fault reason instead of authoritative rows', async () => {
        vi.stubGlobal('fetch', vi.fn(async () => response({ state: 'faulted', reason: 'Acknowledgment lost.' }, 503)));
        renderBrowser();
        expect((await screen.findByRole('alert')).textContent).toContain('Acknowledgment lost.');
        expect(screen.queryByRole('table')).toBeNull();
    });
    it('should direct eventmodel users to the Workbench using the response body', async () => {
        const reason = 'Read models are shown in the Chronicle Workbench under the eventmodel engine.';
        vi.stubGlobal('fetch', vi.fn(async () => response({ engine: 'eventmodel', reason }, 404)));
        renderBrowser();
        expect((await screen.findByRole('alert')).textContent).toBe(reason);
    });
    it('should show a refused model and its reason', async () => {
        vi.stubGlobal('fetch', vi.fn(async () => response({ exceptionMessages: ['Unsupported(Plan) model: Refused.'] }, 501)));
        renderBrowser();
        expect((await screen.findByRole('alert')).textContent).toContain('Stage refused this model. Unsupported(Plan)');
    });
    it('should explain an unknown selected model', async () => {
        const fetch = serve();
        renderBrowser();
        await screen.findByRole('button', { name: 'ProjectSummary' });
        fetch.mockImplementation(input => Promise.resolve(String(input) === 'stage/read-models' ? response({ readModels: [] }) : response({}, 404)));
        fireEvent.click(screen.getByRole('button', { name: 'ProjectSummary' }));
        expect((await screen.findByRole('alert')).textContent).toBe('The selected read model was not found.');
    });
    it('should discard previously shown rows if a refresh faults', async () => {
        const fetch = serve();
        renderBrowser();
        fireEvent.click(await screen.findByRole('button', { name: 'ProjectSummary' }));
        await screen.findByText('Screenplay');
        fetch.mockImplementation(async () => response({ state: 'faulted', reason: 'Indeterminate append.' }, 503));
        fireEvent.click(screen.getByRole('button', { name: 'Refresh' }));
        await screen.findByRole('alert');
        expect(screen.queryByText(detail.name)).toBeNull();
        expect(screen.queryByText('Screenplay')).toBeNull();
        expect(screen.queryByRole('table')).toBeNull();
    });
    it('should report failed HTTP requests', async () => {
        vi.stubGlobal('fetch', vi.fn(async () => new Response('Failed', { status: 500 })));
        renderBrowser();
        expect((await screen.findByRole('alert')).textContent).toBe('The Stage returned 500.');
    });
});
