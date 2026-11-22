// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { endpoints } from '../endpoints';
import { detail, model, renderBrowser, response } from './given/a_browser';
import { deferResponse } from './given/a_deferred_response';
import { anotherDetail, anotherModel, serveTwoModels } from './given/two_models';

afterEach(() => vi.unstubAllGlobals());

describe('when changing selection before the older detail resolves', () => {
    it('should keep the newer selection and detail after the older response arrives', async () => {
        const requests = serveTwoModels();
        renderBrowser();
        await screen.findByRole('button', { name: model.name });
        const olderRead = deferResponse();
        const newerRead = deferResponse();
        requests.mockImplementation(input => String(input) === endpoints.readModel(model.id) ? olderRead.pending : newerRead.pending);

        fireEvent.click(screen.getByRole('button', { name: model.name }));
        await waitFor(() => expect(requests).toHaveBeenCalledWith(endpoints.readModel(model.id), expect.objectContaining({ signal: expect.any(AbortSignal) })));
        fireEvent.click(screen.getByRole('button', { name: anotherModel.name }));
        await waitFor(() => expect(requests).toHaveBeenCalledWith(endpoints.readModel(anotherModel.id), expect.objectContaining({ signal: expect.any(AbortSignal) })));

        await act(async () => { newerRead.complete(response(anotherDetail)); });
        await screen.findByRole('table', { name: `${anotherModel.name} instances` });
        await act(async () => { olderRead.complete(response(detail)); });

        expect(screen.getByRole('button', { name: anotherModel.name }).closest('tr')?.getAttribute('aria-selected')).toBe('true');
        expect(screen.getByRole('button', { name: model.name }).closest('tr')?.getAttribute('aria-selected')).toBe('false');
        expect(screen.getByRole('table', { name: `${anotherModel.name} instances` })).toBeDefined();
        expect(screen.getByText('Another project')).toBeDefined();
        expect(screen.queryByRole('table', { name: `${model.name} instances` })).toBeNull();
        expect(screen.queryByText('Screenplay')).toBeNull();
    });
});
