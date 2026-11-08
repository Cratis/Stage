// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { filterArgumentsFor, parseStageAddress, stageAddress, stageRouteShapeOf } from '../stageSceneRouting';
import { catalog } from './given/a_catalog';

const shape = stageRouteShapeOf(catalog);

describe('when reading what is routed in an application', () => {
    it('should route a pane by the parameters it names', () => expect(shape.routedSlots.detail).toEqual(['sku']));
    it('should route the page slot', () => expect(shape.pageRouted).toBe(true));
    it('should never route a panel', () => expect(shape.routedSlots.activity).toBeUndefined());
    it('should know the routed slot an outlet of tabs owns', () => expect(shape.tabSlots).toEqual({ detailTabs: 'detail' }));
    it('should know the parameter each filter writes and the query it sets, including those a feature adds', () =>
        expect(shape.filters).toEqual({ q: 'AllProducts', status: 'AllProducts' }));
    it('should know the screen a slot shows for a selection', () => expect(shape.slotDefaults).toEqual({ detail: 'ProductDetails' }));
    it('should know the screen a slot route segment opens', () => expect(shape.segmentScreens['set-product-price']).toBe('SetProductPrice'));
});

describe('when writing an address', () => {
    const route = {
        slots: { detail: { screen: 'ProductDetails', parameters: { sku: 'NK-101' } }, activity: { screen: 'SetProductPrice', parameters: {} } },
        tabs: { detailTabs: 'pricing' },
        query: { q: 'kettle', status: 'onSale' },
    };

    it('should put the selected item of a routed pane, the chosen tab of the slot it owns and the filters in the address', () =>
        expect(stageAddress('ProductCatalog', route, shape)).toBe('ProductCatalog/@detail/NK-101/@detail.detailTabs/pricing?q=kettle&status=onSale'));
    it('should put the segment of a slice opened in a routed slot after its route values', () =>
        expect(stageAddress('ProductCatalog', { slots: { detail: { screen: 'SetProductPrice', parameters: { sku: 'NK-101' }, segment: 'set-product-price' } }, tabs: {}, query: {} }, shape))
            .toBe('ProductCatalog/@detail/NK-101/set-product-price'));
    it('should keep the screen parameters of the navigation host beside the filters', () =>
        expect(stageAddress('ProductCatalog?region=north', { slots: {}, tabs: {}, query: { q: 'kettle' } }, shape)).toBe('ProductCatalog?region=north&q=kettle'));
    it('should write a route value that looks like a group with its mark encoded', () =>
        expect(stageAddress('ProductCatalog', { slots: { detail: { parameters: { sku: '@home' } } }, tabs: {}, query: {} }, shape)).toBe('ProductCatalog/@detail/%40home'));
    it('should leave the filters out when the page slot is not routed', () =>
        expect(stageAddress('ProductCatalog', { slots: {}, tabs: {}, query: { q: 'kettle' } }, { ...shape, pageRouted: false })).toBe('ProductCatalog'));
});

describe('when reading an address back', () => {
    const address = parseStageAddress('ProductCatalog/@detail/NK-101/@detail.detailTabs/pricing?region=north&q=kettle&status=onSale', shape);

    it('should give the navigation host its own part, with its own parameters', () => expect(address.scene).toBe('ProductCatalog?region=north'));
    it('should show the selected item in the routed pane, with the screen the pane shows for a selection', () =>
        expect(address.route.slots.detail).toEqual({ screen: 'ProductDetails', parameters: { sku: 'NK-101' } }));
    it('should choose the tab the address names', () => expect(address.route.tabs).toEqual({ detailTabs: 'pricing' }));
    it('should give the filters back their values', () => expect(address.route.query).toEqual({ q: 'kettle', status: 'onSale' }));
    it('should open the screen a slot route segment names', () =>
        expect(parseStageAddress('ProductCatalog/@detail/NK-101/set-product-price', shape).route.slots.detail)
            .toEqual({ screen: 'SetProductPrice', parameters: { sku: 'NK-101' }, segment: 'set-product-price' }));
    it('should read an encoded mark as part of a route value', () =>
        expect(parseStageAddress('ProductCatalog/@detail/%40home', shape).route.slots.detail.parameters).toEqual({ sku: '@home' }));
    it('should round-trip what it writes', () => {
        const route = { slots: { detail: { screen: 'ProductDetails', parameters: { sku: 'a b/c' } } }, tabs: { detailTabs: 'overview' }, query: { q: 'kettle & mug' } };
        expect(parseStageAddress(stageAddress('ProductCatalog', route, shape), shape)).toEqual({ scene: 'ProductCatalog', route });
    });
});

describe('when giving a query the values the filters wrote', () => {
    it('should give a query the values of the filters that set its parameters', () =>
        expect(filterArgumentsFor(shape, { q: 'kettle', status: 'onSale', other: 'x' }, 'AllProducts')).toEqual({ q: 'kettle', status: 'onSale' }));
    it('should give another query none of them', () => expect(filterArgumentsFor(shape, { q: 'kettle' }, 'ProductStats')).toEqual({}));
});
