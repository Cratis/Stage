// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import type { Screen } from '@cratis/scene.model';
import { registerStageCommands } from '../../src/host/commandRegistration';
import { stageComponents } from './stageComponents';
import { StageDataProvider } from './stageData';
import { StageStructureProvider } from './stageStructure';
import { element } from './testElements';
import { commandResult } from './testing/commands';

const StageSliceDialog = stageComponents['core:sliceDialog'];
const draftProduct = JSON.stringify({
    type: 'object',
    required: ['productId', 'sku', 'name', 'category'],
    properties: {
        productId: { type: 'string', format: 'uuid', 'x-concept': 'ProductId', 'x-identity': true },
        sku: { type: 'string' },
        name: { type: 'string' },
        category: { type: 'string' },
    },
});
const routes = { commands: { DraftProduct: '/api/products/draft' }, queries: {}, commandSchemas: { DraftProduct: draftProduct } };
const slice = 'Merchandising.Products.DraftProduct';
const newProduct = {
    name: 'NewProduct', layout: 'AppShell', screenTemplate: 'QuickCreateDialog', contributions: [], owner: slice,
    slotContent: { actions: [element('action-DraftProduct', 'core:action', { command: 'DraftProduct', navigateToScreen: 'ProductCatalog' })] },
    forms: [{ name: 'DraftProductForm', forCommand: 'DraftProduct', fields: [{ name: 'sku', label: 'SKU' }, { name: 'name', label: 'Product' }, { name: 'category', label: 'Category' }] }],
} as unknown as Screen;
const field = (name: string) => document.querySelector(`.stage-command-form__field[data-field="${name}"]`);

function dialog(screens: Screen[]) {
    registerStageCommands(routes);
    return render(
        <PrimeReactProvider>
            <StageDataProvider routes={routes} parameters={{}} locale='en' locales={['en']} screen='ProductCatalog'>
                <StageStructureProvider screens={screens}>
                    <StageSliceDialog element={element('dialog', 'core:sliceDialog', { slice, command: 'DraftProduct', title: 'New product' })} slots={{}} />
                </StageStructureProvider>
            </StageDataProvider>
        </PrimeReactProvider>,
    );
}

describe('the dialog of a toolbar slice button whose slice declares its own dialog screen', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); globalThis.history.replaceState(null, '', '/'); });

    it('draws the slice\'s screen', () => {
        const { container } = dialog([newProduct]);
        expect(container.querySelector('[data-stage-slice-screen="NewProduct"]')).not.toBeNull();
    });

    it('asks for the fields of the screen\'s form, with the form\'s labels', () => {
        dialog([newProduct]);
        expect(screen.getByLabelText('SKU')).toBeDefined();
        expect(screen.getByLabelText('Product')).toBeDefined();
        expect(screen.getByLabelText('Category')).toBeDefined();
    });

    it('does not ask for the identity the command creates', () => {
        dialog([newProduct]);
        expect(field('productId')).toBeNull();
    });

    it('goes where the screen\'s action goes once the command succeeded', async () => {
        vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult() }));
        dialog([newProduct]);

        fireEvent.change(screen.getByLabelText('SKU'), { target: { value: 'ACM-1' } });
        fireEvent.change(screen.getByLabelText('Product'), { target: { value: 'Mug' } });
        fireEvent.change(screen.getByLabelText('Category'), { target: { value: 'Kitchen' } });
        fireEvent.click(screen.getByRole('button', { name: 'New product' }));

        await waitFor(() => expect(globalThis.location.href).toContain('ProductCatalog'));
    });

    it('leaves a slice without a dialog screen to the Scene task dialog', () => {
        const { container } = dialog([]);
        expect(container.querySelector('[data-stage-slice-screen]')).toBeNull();
    });
});
