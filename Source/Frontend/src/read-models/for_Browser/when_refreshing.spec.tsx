// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { detail, renderBrowser, response, serve } from './given/a_browser';

afterEach(() => vi.unstubAllGlobals());

describe('when refreshing a selected read model', () => {
    beforeEach(async () => {
        const fetch = serve();
        renderBrowser();
        fireEvent.click(await screen.findByRole('button', { name: 'ProjectSummary' }));
        await screen.findByText('Screenplay');
        fetch.mockImplementation(input => Promise.resolve(String(input) === 'stage/read-models'
            ? response({ readModels: [{ ...detail, module: 'Projects', feature: 'Registration', slice: 'ProjectLookup', instances: 2 }] })
            : response({ ...detail, instances: [{ key: 'new-key', values: { name: 'Updated' } }] })));
        fireEvent.click(screen.getByRole('button', { name: 'Refresh' }));
        await screen.findByText('Updated');
    });

    it('should replace instance values with the current world', () => {
        expect(screen.queryByText('Screenplay')).toBeNull();
        expect(screen.getByText('new-key')).toBeDefined();
    });
    it('should refresh the instance count too', () => {
        expect(screen.getByText('2')).toBeDefined();
    });
});
