// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { endpoints } from '../endpoints';
import { model, renderBrowser } from './given/a_browser';
import { deferResponse } from './given/a_deferred_response';
import { anotherModel, serveTwoModels } from './given/two_models';

afterEach(() => vi.unstubAllGlobals());

describe('when a selected detail is pending', () => {
    let list: HTMLElement;

    beforeEach(async () => {
        const requests = serveTwoModels();
        renderBrowser();
        list = await screen.findByRole('table', { name: 'Read models' });
        const detailRead = deferResponse();
        requests.mockImplementation(() => detailRead.pending);
        fireEvent.click(screen.getByRole('button', { name: model.name }));
        await waitFor(() => expect(requests).toHaveBeenCalledWith(endpoints.readModel(model.id), expect.objectContaining({ signal: expect.any(AbortSignal) })));
    });

    it('should keep the same summary list mounted with navigation available', () => {
        expect(screen.getByRole('table', { name: 'Read models' })).toBe(list);
        expect(screen.getByRole('button', { name: anotherModel.name })).toBeDefined();
    });

    it('should reserve the summary loading status for summary requests', () => {
        expect(screen.queryByRole('status')).toBeNull();
    });
});
