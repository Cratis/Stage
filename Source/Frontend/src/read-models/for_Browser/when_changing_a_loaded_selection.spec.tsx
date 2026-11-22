// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { model, renderBrowser } from './given/a_browser';
import { deferResponse } from './given/a_deferred_response';
import { anotherModel, serveTwoModels } from './given/two_models';

afterEach(() => vi.unstubAllGlobals());

describe('when changing selection after showing a detail', () => {
    it('should clear the older detail while the new selection loads', async () => {
        const requests = serveTwoModels();
        renderBrowser();
        fireEvent.click(await screen.findByRole('button', { name: model.name }));
        await screen.findByText('Screenplay');
        const detailRead = deferResponse();
        requests.mockImplementation(() => detailRead.pending);

        fireEvent.click(screen.getByRole('button', { name: anotherModel.name }));

        expect(screen.getByRole('button', { name: anotherModel.name }).closest('tr')?.getAttribute('aria-selected')).toBe('true');
        expect(screen.queryByRole('table', { name: `${model.name} instances` })).toBeNull();
        expect(screen.queryByText('Screenplay')).toBeNull();
        expect(screen.getByRole('table', { name: 'Read models' })).toBeDefined();
    });
});
