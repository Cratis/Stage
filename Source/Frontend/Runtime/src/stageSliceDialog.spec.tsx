// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { registerStageCommands } from '../../src/host/commandRegistration';
import { stageComponents } from './stageComponents';
import { StageDataProvider } from './stageData';
import { element } from './testElements';
import { commandResult } from './testing/commands';

const StageSliceDialog = stageComponents['core:sliceDialog'];
const checkOut = JSON.stringify({
    type: 'object',
    required: ['basketId', 'shipTo', 'method'],
    properties: {
        basketId: { type: 'string', format: 'uuid', 'x-concept': 'BasketId' },
        shipTo: { type: 'object', required: ['street'], properties: { street: { type: 'string' }, city: { type: 'string' } } },
        method: { type: 'string', enum: ['standard', 'express'] },
    },
});
const rename = JSON.stringify({ type: 'object', required: ['name'], properties: { name: { type: 'string' } } });
const routes = { commands: { CheckOut: '/api/checkout', Rename: '/api/rename' }, queries: {}, commandSchemas: { CheckOut: checkOut, Rename: rename } };

function dialog(command: string) {
    registerStageCommands(routes);
    return render(
        <PrimeReactProvider>
            <StageDataProvider routes={routes} locale='en' locales={['en']} screen='Basket'>
                <StageSliceDialog element={element('dialog', 'core:sliceDialog', { slice: `Shop.${command}`, command, title: command })} slots={{}} />
            </StageDataProvider>
        </PrimeReactProvider>,
    );
}

describe('the task dialog of a toolbar slice button', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); });

    it('asks for an enumeration, a record and a collection through the command form, and not for the identity the session acts as', async () => {
        const fetched = vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult() });
        vi.stubGlobal('fetch', fetched);
        dialog('CheckOut');

        expect(screen.queryByLabelText('Basket Id')).toBeNull();
        const address = screen.getByRole('group', { name: 'Ship to' });
        fireEvent.change(within(address).getByLabelText('Street'), { target: { value: '12 Analytical Way' } });
        expect(screen.getByText('Method')).toBeDefined();
        fireEvent.click(screen.getByRole('button', { name: 'Execute CheckOut' }));

        // The method is required and has not been chosen, so nothing is submitted yet.
        expect((await screen.findAllByText(/method is required/i)).length).toBeGreaterThan(0);
        expect(fetched).not.toHaveBeenCalled();
    });

    it('leaves a command of plain values to the Scene task dialog', () => {
        const { container } = dialog('Rename');

        expect(container.querySelector('form.scene-slice-dialog')).not.toBeNull();
    });
});
