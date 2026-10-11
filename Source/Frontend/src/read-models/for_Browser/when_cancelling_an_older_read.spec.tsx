// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { detail, model, renderBrowser, response, serve } from './given/a_browser';

afterEach(() => vi.unstubAllGlobals());

describe('when refreshing while an older detail request is pending', () => {
    it('should not let the older response replace the refreshed world', async () => {
        let completeOlderRead!: (value: Response) => void;
        const fetch = serve();
        fetch.mockImplementation(input => String(input) === 'stage/read-models'
            ? Promise.resolve(response({ readModels: [model] }))
            : new Promise<Response>(resolve => { completeOlderRead = resolve; }));
        renderBrowser();
        fireEvent.click(await screen.findByRole('button', { name: 'ProjectSummary' }));
        await waitFor(() => expect(completeOlderRead).toBeDefined());
        fetch.mockImplementation(input => Promise.resolve(String(input) === 'stage/read-models'
            ? response({ readModels: [model] })
            : response({ ...detail, instances: [{ key: 'new', values: { name: 'Current' } }] })));
        fireEvent.click(screen.getByRole('button', { name: 'Refresh' }));
        await screen.findByText('Current');
        await act(async () => { completeOlderRead(response(detail)); });
        expect(screen.queryByText('Screenplay')).toBeNull();
        expect(screen.getByText('Current')).toBeDefined();
    });
});
