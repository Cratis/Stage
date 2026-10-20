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
const schema = JSON.stringify({ type: 'object', required: ['quantity'], properties: { quantity: { type: 'integer' } } });

// Cratis/Stage#618 on Studio Play: a command form (the Add to basket dialog) posted to the root of Studio, answered 405,
// because the form's Arc command takes its base path from an Arc provider a live Stage never mounts.
describe('a command form in a page served under a path prefix', () => {
    afterEach(() => { vi.unstubAllGlobals(); vi.restoreAllMocks(); globalThis.history.replaceState(null, '', '/'); });

    it('posts its command under the prefix the page is served from and not to the root of the host', async () => {
        globalThis.history.replaceState(null, '', `${prefix}/#/Product`);
        const fetched = vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult() });
        vi.stubGlobal('fetch', fetched);
        render(
            <PrimeReactProvider>
                <StageDataProvider routes={routes} locale='en' locales={['en']} screen='Product'>
                    <StageCommandForm element={element('add', 'Stage:commandForm', {
                        command: 'AddToBasket', label: 'Add to basket', schema, generationMode: FormGenerationMode.Manual,
                        fields: [{ name: 'quantity', label: 'How many?' }],
                    })} />
                </StageDataProvider>
            </PrimeReactProvider>,
        );

        fireEvent.change(screen.getByLabelText('How many?'), { target: { value: '2' } });
        fireEvent.click(screen.getByRole('button', { name: 'Execute Add to basket' }));

        await waitFor(() => expect(fetched.mock.calls.some(([url]) => String(url).includes('add-to-basket'))).toBe(true));
        const url = String(fetched.mock.calls.find(([address]) => String(address).includes('add-to-basket'))![0]);
        expect(new URL(url, 'http://localhost').pathname).toBe(`${prefix}/api/shop/basket/add-to-basket`);
    });
});
