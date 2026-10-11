// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, screen, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { detail, renderBrowser, serve } from './given/a_browser';

afterEach(() => vi.unstubAllGlobals());

describe('when selecting a read model', () => {
    beforeEach(async () => {
        serve();
        renderBrowser();
        fireEvent.click(await screen.findByRole('button', { name: 'ProjectSummary' }));
        await screen.findByRole('table', { name: 'ProjectSummary instances' });
    });

    it('should request the encoded semantic identity', () => {
        expect(fetch).toHaveBeenCalledWith('stage/read-models/sm%3Amodel', expect.objectContaining({ signal: expect.any(AbortSignal) }));
    });
    it('should show the key and one column per declared property', () => {
        const table = within(screen.getByRole('table', { name: 'ProjectSummary instances' }));
        expect(table.getAllByRole('columnheader').map(column => column.textContent)).toEqual(['Key', 'name', 'tags', 'address']);
        expect(table.getByText('project-one')).toBeDefined();
        expect(table.getByText('Screenplay')).toBeDefined();
    });
    it('should show arrays and composites as compact JSON', () => {
        expect(screen.getByText('["one","two"]')).toBeDefined();
        expect(screen.getByText('{"city":"Oslo"}')).toBeDefined();
    });
});

describe('when selecting a model without instances', () => {
    beforeEach(async () => {
        serve(undefined, { ...detail, instances: [] });
        renderBrowser();
        fireEvent.click(await screen.findByRole('button', { name: 'ProjectSummary' }));
    });

    it('should explain the empty instance table', async () => {
        expect(await screen.findByText('No instances yet.')).toBeDefined();
    });
});
