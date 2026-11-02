// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { FormGenerationMode } from '@cratis/scene.model';
import { RegisteredForm as StageCommandForm, commandResult } from './testing/commands';
import { StageDataProvider } from './stageData';
import { element } from './testElements';

const prefix = '/api/play/0d0f6d8e-1f5c-4a9f-9b57-6a1f6a5b7c11';
const routes = { commands: { AddToBasket: '/api/shop/basket/add-to-basket' }, queries: {} };
const schema = JSON.stringify({ type: 'object', required: ['sku'], properties: { sku: { type: 'string' } } });

describe('a command form in a Stage served under a path prefix', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.history.replaceState(null, '', '/'); globalThis.localStorage?.clear(); });

    it('posts the command under the prefix the page is served under, with Arc left at its default configuration', async () => {
        globalThis.history.replaceState(null, '', `${prefix}/#/Browse`);
        const fetched = vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult() });
        vi.stubGlobal('fetch', fetched);

        render(
            <PrimeReactProvider>
                <StageDataProvider routes={routes} locale='en' locales={['en']} screen='Browse'>
                    <StageCommandForm element={element('add', 'Stage:commandForm', { command: 'AddToBasket', label: 'Add to basket', schema, generationMode: FormGenerationMode.Auto, fields: [{ name: 'sku', label: 'Product' }] })} />
                </StageDataProvider>
            </PrimeReactProvider>,
        );
        fireEvent.change(screen.getByLabelText('Product'), { target: { value: 'ACM-1001' } });
        fireEvent.click(screen.getByRole('button', { name: 'Add to basket' }));

        await waitFor(() => expect(fetched.mock.calls.some(([url]) => String(url).includes('add-to-basket'))).toBe(true));
        const posted = fetched.mock.calls.map(([url]) => new URL(String(url), 'http://localhost').pathname).find(path => path.includes('add-to-basket'));
        expect(posted).toBe(`${prefix}/api/shop/basket/add-to-basket`);
    });
});
