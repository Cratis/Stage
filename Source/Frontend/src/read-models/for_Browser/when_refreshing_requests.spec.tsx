// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { endpoints } from '../endpoints';
import { model, renderBrowser, serve } from './given/a_browser';

afterEach(() => vi.unstubAllGlobals());

describe('when refreshing after selecting a model', () => {
    it('should request both the summaries and the selected detail again', async () => {
        const requests = serve();
        renderBrowser();
        fireEvent.click(await screen.findByRole('button', { name: model.name }));
        await screen.findByRole('table', { name: `${model.name} instances` });
        const summaryReads = requests.mock.calls.filter(([input]) => String(input) === endpoints.readModels).length;
        const detailReads = requests.mock.calls.filter(([input]) => String(input) === endpoints.readModel(model.id)).length;

        fireEvent.click(screen.getByRole('button', { name: 'Refresh' }));
        await waitFor(() => {
            expect(requests.mock.calls.filter(([input]) => String(input) === endpoints.readModels)).toHaveLength(summaryReads + 1);
            expect(requests.mock.calls.filter(([input]) => String(input) === endpoints.readModel(model.id))).toHaveLength(detailReads + 1);
        });
        await screen.findByRole('table', { name: `${model.name} instances` });
    });
});
