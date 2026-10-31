// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { actionVariant, humanize, humanizeScreenName } from './humanize';

describe('humanizing a name', () => {
    it.each([
        ['RegisterOpportunity', 'Register opportunity'],
        ['postalCode', 'Postal code'],
        ['RegisterSKU', 'Register SKU'],
        ['SKUList', 'SKU list'],
        ['Line2', 'Line2'],
        ['Add to basket', 'Add to basket'],
    ])('should write %s as %s', (name, expected) => expect(humanize(name)).toEqual(expected));

    it.each([
        ['PipelineScreen', 'Pipeline'],
        ['InvoiceDetailsPage', 'Invoice details'],
        ['ConfirmDialog', 'Confirm'],
        ['Screen', 'Screen'],
        ['OpportunityList', 'Opportunity list'],
        ['SKUScreen', 'SKU'],
    ])('should write the screen %s as %s', (name, expected) => expect(humanizeScreenName(name)).toEqual(expected));
});

describe('the emphasis of a toolbar action', () => {
    it('should make the first action primary and the ones after it secondary', () => {
        expect(actionVariant('', false)).toEqual('solid');
        expect(actionVariant('', true)).toEqual('outline');
    });

    it.each([['primary', 'solid'], ['secondary', 'outline'], ['quiet', 'ghost'], ['Primary', 'solid']])('should yield to the authored emphasis %s even after another action', (authored, expected) => {
        expect(actionVariant(authored, true)).toEqual(expected);
        expect(actionVariant(authored, false)).toEqual(expected);
    });
});
