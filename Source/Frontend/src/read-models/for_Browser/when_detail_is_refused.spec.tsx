// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, fireEvent, renderHook, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { model, renderBrowser, response } from './given/a_browser';
import { endpoints } from '../endpoints';
import { useBrowser } from '../useBrowser';

describe('when a read-model detail is refused', () => {
    afterEach(() => vi.unstubAllGlobals());

    it('should keep the list and selected identity in the hook after a refused detail', async () => {
        vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) =>
            String(input).endsWith(endpoints.readModels)
                ? response({ readModels: [model] })
                : response({ exceptionMessages: ['RefusedProjection is not admitted.'] }, 501)));
        const { result } = renderHook(() => useBrowser());
        await waitFor(() => expect(result.current.readModels).toEqual([model]));
        act(() => result.current.select(model.id));
        await waitFor(() => expect(result.current.message).toContain('RefusedProjection'));
        expect(result.current.readModels).toEqual([model]);
        expect(result.current.selectedId).toBe(model.id);
        expect(result.current.loading).toBe(false);
        expect(result.current.detail).toBeUndefined();
    });

    it('should retain the fetched list and show the detail refusal', async () => {
        vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) =>
            String(input).endsWith(endpoints.readModels)
                ? response({ readModels: [model] })
                : response({ supported: false, capability: 'Projection', exceptionMessages: ['RefusedProjection is not admitted.'] }, 501)));
        renderBrowser();
        fireEvent.click(await screen.findByRole('button', { name: model.name }));
        expect((await screen.findByRole('alert')).textContent).toContain('RefusedProjection');
        expect(screen.getByRole('button', { name: new RegExp(model.name) })).toBeTruthy();
    });
});
