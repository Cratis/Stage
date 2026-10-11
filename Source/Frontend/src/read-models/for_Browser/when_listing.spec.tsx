// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { model, renderBrowser, serve } from './given/a_browser';

afterEach(() => vi.unstubAllGlobals());

describe('when listing the live read models', () => {
    beforeEach(() => {
        serve([model, { ...model, id: 'sm:empty', name: 'EmptySummary', instances: 0 }]);
        renderBrowser();
    });

    it('should show every read model including empty ones', async () => {
        expect(await screen.findByRole('button', { name: 'EmptySummary' })).toBeDefined();
        expect(screen.getByRole('button', { name: 'ProjectSummary' })).toBeDefined();
        expect(screen.getByText('0')).toBeDefined();
        expect(screen.getByText('1')).toBeDefined();
    });
    it('should show each model location', async () => {
        await screen.findByRole('button', { name: 'ProjectSummary' });
        expect(screen.getAllByText('Projects / Registration / ProjectLookup')).toHaveLength(2);
    });
});

describe('when the plan contains no read models', () => {
    beforeEach(() => {
        serve([]);
        renderBrowser();
    });

    it('should explain the empty list', async () => {
        expect(await screen.findByText('No read models in this session.')).toBeDefined();
    });
});
