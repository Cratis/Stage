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
const addToBasket = JSON.stringify({
    type: 'object',
    required: ['basketId', 'productId', 'packaging'],
    properties: {
        basketId: { type: 'string', format: 'uuid', 'x-concept': 'BasketId' },
        productId: { type: 'string', format: 'uuid', 'x-concept': 'ProductId' },
        packaging: { type: 'string', enum: ['box', 'bag'] },
    },
});
const rename = JSON.stringify({ type: 'object', required: ['name'], properties: { name: { type: 'string' } } });
const routes = {
    commands: { CheckOut: '/api/checkout', AddToBasket: '/api/basket/add', Rename: '/api/rename' },
    queries: {},
    commandSchemas: { CheckOut: checkOut, AddToBasket: addToBasket, Rename: rename },
};
const field = (name: string) => document.querySelector(`.stage-command-form__field[data-field="${name}"]`);

function dialog(command: string, parameters: Record<string, string> = {}) {
    registerStageCommands(routes);
    return render(
        <PrimeReactProvider>
            <StageDataProvider routes={routes} parameters={parameters} locale='en' locales={['en']} screen='Basket'>
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
        globalThis.localStorage.setItem('stage.session.BasketId', 'b1');
        dialog('CheckOut');

        expect(screen.queryByLabelText('Basket Id')).toBeNull();
        expect(field('basketId')).toBeNull();
        const address = screen.getByRole('group', { name: 'Ship to' });
        fireEvent.change(within(address).getByLabelText('Street'), { target: { value: '12 Analytical Way' } });
        expect(screen.getByText('Method')).toBeDefined();
        fireEvent.click(screen.getByRole('button', { name: 'Execute CheckOut' }));

        // The method is required and has not been chosen, so nothing is submitted yet.
        expect((await screen.findAllByText(/method is required/i)).length).toBeGreaterThan(0);
        expect(fetched).not.toHaveBeenCalled();
    });

    it('asks for a required reference to another entity that nothing supplies', () => {
        dialog('AddToBasket');

        expect(field('productId')).not.toBeNull();
        expect(field('packaging')).not.toBeNull();
    });

    it('asks for the identity the session acts as while the session holds none', () => {
        dialog('AddToBasket');

        expect(field('basketId')).not.toBeNull();
    });

    it('does not ask for an identifier the session holds, and still asks for the reference it does not', () => {
        globalThis.localStorage.setItem('stage.session.BasketId', 'b1');
        dialog('AddToBasket');

        expect(field('basketId')).toBeNull();
        expect(field('productId')).not.toBeNull();
    });

    it('does not ask for an identifier the screen carries in its address', () => {
        dialog('AddToBasket', { basketId: 'b1', productId: 'p1' });

        expect(field('basketId')).toBeNull();
        expect(field('productId')).toBeNull();
        expect(field('packaging')).not.toBeNull();
    });

    it('leaves a command of plain values to the Scene task dialog', () => {
        const { container } = dialog('Rename');

        expect(container.querySelector('form.scene-slice-dialog')).not.toBeNull();
    });
});
