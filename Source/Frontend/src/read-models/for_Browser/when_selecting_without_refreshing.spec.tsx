// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { endpoints } from '../endpoints';
import { model, renderBrowser } from './given/a_browser';
import { anotherModel, serveTwoModels } from './given/two_models';

afterEach(() => vi.unstubAllGlobals());

describe('when selecting models without refreshing', () => {
    it('should load the summaries only once', async () => {
        const requests = serveTwoModels();
        renderBrowser();
        fireEvent.click(await screen.findByRole('button', { name: model.name }));
        await screen.findByRole('table', { name: `${model.name} instances` });
        fireEvent.click(screen.getByRole('button', { name: anotherModel.name }));
        await screen.findByRole('table', { name: `${anotherModel.name} instances` });

        expect(requests.mock.calls.filter(([input]) => String(input) === endpoints.readModels)).toHaveLength(1);
    });
});
