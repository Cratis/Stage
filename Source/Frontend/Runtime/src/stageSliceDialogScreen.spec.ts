// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { ScreenFit, type Screen } from '@cratis/scene.model';
import { sliceDialogScreenOf } from './stageSliceDialogScreen';
import { element } from './testElements';

const slice = 'Merchandising.Products.DraftProduct';
const form = { name: 'DraftProductForm', forCommand: 'DraftProduct', fields: [{ name: 'sku', label: 'SKU' }, { name: 'name', label: 'Product' }] };
const screen = (overrides: Partial<Screen>): Screen => ({
    name: 'NewProduct', layout: 'AppShell', contributions: [], forms: [form], owner: slice,
    slotContent: { actions: [element('action-DraftProduct', 'core:action', { command: 'DraftProduct', navigateToScreen: 'ProductCatalog' })] },
    ...overrides,
} as unknown as Screen);

describe('finding the dialog screen a slice declares', () => {
    describe('when the slice declares a screen filling a blueprint dialog template', () => {
        const found = sliceDialogScreenOf([screen({ screenTemplate: 'QuickCreateDialog' })], slice, 'DraftProduct');

        it('finds the screen', () => expect(found?.screen.name).toBe('NewProduct'));
        it('takes the form the screen declares for the command', () => expect(found?.form?.fields.map(field => field.label)).toEqual(['SKU', 'Product']));
        it('takes where the action goes once the command succeeded', () => expect(found?.navigateToScreen).toBe('ProductCatalog'));
    });

    describe('when the slice declares a screen that fits a dialog', () => {
        it('finds the screen', () => expect(sliceDialogScreenOf([screen({ fits: [ScreenFit.Dialog] })], slice, 'DraftProduct')?.screen.name).toBe('NewProduct'));
    });

    describe('when the slice declares a screen filling a dialog template of the application', () => {
        it('finds the screen', () => expect(sliceDialogScreenOf([screen({ screenTemplate: 'ProductDialog' })], slice, 'DraftProduct', [{ name: 'ProductDialog' }])?.screen.name).toBe('NewProduct'));
    });

    describe('when the slice declares only a page', () => {
        it('finds nothing', () => expect(sliceDialogScreenOf([screen({ screenTemplate: 'ListScreen' })], slice, 'DraftProduct')).toBeUndefined());
    });

    describe('when the dialog screen belongs to another slice', () => {
        it('finds nothing', () => expect(sliceDialogScreenOf([screen({ screenTemplate: 'QuickCreateDialog', owner: 'Merchandising.Products.SetPrice' })], slice, 'DraftProduct')).toBeUndefined());
    });

    describe('when no slice is named', () => {
        it('finds nothing', () => expect(sliceDialogScreenOf([screen({ screenTemplate: 'QuickCreateDialog' })], undefined, 'DraftProduct')).toBeUndefined());
    });
});
