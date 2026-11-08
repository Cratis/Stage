// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import type { SceneNavigationState } from '@cratis/scene.react';
import { createStageSceneRouting, stageRouteShapeOf, type StageRoutingMode, type StageSceneRouting } from '../stageSceneRouting';
import { catalog } from './given/a_catalog';

const shape = stageRouteShapeOf(catalog);
const routings: StageSceneRouting[] = [];

function routing(mode: StageRoutingMode = 'path'): StageSceneRouting {
    const created = createStageSceneRouting(window, mode, shape);
    routings.push(created);
    // The navigation host listens from the start, which is what keeps the routing listening to the browser.
    created.history.listen(() => undefined);
    return created;
}

// The browser announces back and forward after the fact.
function popped(): Promise<void> {
    return new Promise(resolve => window.addEventListener('popstate', () => setTimeout(resolve, 0), { once: true }));
}

async function back() {
    const done = popped();
    window.history.back();
    await done;
}

async function forward() {
    const done = popped();
    window.history.forward();
    await done;
}

const address = () => `${window.location.pathname}${window.location.search}`;
const page: SceneNavigationState = { screen: 'ProductCatalog', url: 'ProductCatalog', parameters: {} };

beforeEach(() => window.history.replaceState(null, '', '/ProductCatalog'));
afterEach(() => {
    routings.splice(0).forEach(created => created.dispose());
    window.history.replaceState(null, '', '/');
});

describe('when an item is selected into a routed pane', () => {
    it('should put the item in the address', () => {
        routing().openInSlot('detail', { screen: 'ProductDetails', parameters: { sku: 'NK-101' } });
        expect(address()).toBe('/ProductCatalog/@detail/NK-101');
    });

    it('should make it a history entry', () => {
        const length = window.history.length;
        routing().openInSlot('detail', { screen: 'ProductDetails', parameters: { sku: 'NK-101' } });
        expect(window.history.length).toBe(length + 1);
    });

    it('should show it again after a reload, or from a shared link', () => {
        routing().openInSlot('detail', { screen: 'ProductDetails', parameters: { sku: 'NK-101' } });
        expect(routing().getState().slots.detail).toEqual({ screen: 'ProductDetails', parameters: { sku: 'NK-101' } });
    });

    it('should give the navigation host only its own part of the address', () => {
        routing().openInSlot('detail', { screen: 'ProductDetails', parameters: { sku: 'NK-101' } });
        expect(routing().history.location()).toBe('ProductCatalog');
    });
});

describe('when a tab is chosen in the routed slot it owns', () => {
    it('should put the tab in the address, after the slot', () => {
        const router = routing();
        router.openInSlot('detail', { screen: 'ProductDetails', parameters: { sku: 'NK-101' } });
        router.selectTab('detailTabs', 'pricing');
        expect(address()).toBe('/ProductCatalog/@detail/NK-101/@detail.detailTabs/pricing');
    });

    it('should choose it again after a reload', () => {
        routing().selectTab('detailTabs', 'pricing');
        expect(routing().getState().tabs).toEqual({ detailTabs: 'pricing' });
    });
});

describe('when a filter is set on a routed page', () => {
    it('should put its value in the address as a query parameter', () => {
        const router = routing();
        router.setQueryParameter('q', 'kettle');
        router.setQueryParameter('status', 'onSale');
        expect(address()).toBe('/ProductCatalog?q=kettle&status=onSale');
    });

    it('should not add a history entry', () => {
        const length = window.history.length;
        routing().setQueryParameter('q', 'kettle');
        expect(window.history.length).toBe(length);
    });

    it('should give the filters their values back after a reload', () => {
        routing().setQueryParameter('q', 'kettle');
        expect(routing().getState().query).toEqual({ q: 'kettle' });
    });

    it('should take a cleared filter out of the address', () => {
        const router = routing();
        router.setQueryParameter('q', 'kettle');
        router.setQueryParameter('q', undefined);
        expect(address()).toBe('/ProductCatalog');
    });
});

describe('when going back and forward', () => {
    it('should move through the routed slot changes', async () => {
        const router = routing();
        router.openInSlot('detail', { screen: 'ProductDetails', parameters: { sku: 'NK-101' } });
        router.openInSlot('detail', { screen: 'ProductDetails', parameters: { sku: 'FM-220' } });

        await back();
        expect(router.getState().slots.detail.parameters).toEqual({ sku: 'NK-101' });
        await back();
        expect(router.getState().slots.detail).toBeUndefined();
        await forward();
        expect(router.getState().slots.detail.parameters).toEqual({ sku: 'NK-101' });
    });

    it('should move through the chosen tabs', async () => {
        const router = routing();
        router.selectTab('detailTabs', 'overview');
        router.selectTab('detailTabs', 'pricing');

        await back();
        expect(router.getState().tabs.detailTabs).toBe('overview');
    });

    it('should tell what reads the router that the route changed', async () => {
        const router = routing();
        router.openInSlot('detail', { screen: 'ProductDetails', parameters: { sku: 'NK-101' } });
        let changes = 0;
        router.subscribe(() => changes++);

        await back();
        expect(changes).toBe(1);
    });
});

describe('when a dialog or a drawer opens over the page', () => {
    it('should not change the address', () => {
        const router = routing();
        router.history.replace(page);
        router.openInSlot('detail', { screen: 'ProductDetails', parameters: { sku: 'NK-101' } });
        const before = address();

        router.history.push({ ...page, dialog: 'SetProductPrice' });
        expect(address()).toBe(before);
    });

    it('should not be a history entry', () => {
        const router = routing();
        router.history.replace(page);
        const length = window.history.length;

        router.history.push({ ...page, dialog: 'SetProductPrice' });
        expect(window.history.length).toBe(length);
    });

    it('should put the page back when it closes, without leaving it', () => {
        const router = routing();
        let restored: SceneNavigationState | undefined;
        router.history.listen((_, state) => { restored = state; });
        router.history.replace(page);
        router.history.push({ ...page, dialog: 'SetProductPrice' });

        router.history.back();
        expect(restored).toEqual(page);
        expect(router.history.state()).toEqual(page);
        expect(address()).toBe('/ProductCatalog');
    });

    it('should leave it out of back and forward', async () => {
        const router = routing();
        router.history.replace(page);
        router.openInSlot('detail', { screen: 'ProductDetails', parameters: { sku: 'NK-101' } });
        router.history.push({ ...page, dialog: 'SetProductPrice' });

        await back();
        expect(address()).toBe('/ProductCatalog');
    });
});

describe('when the navigation host goes to another screen', () => {
    it('should leave the slots, tabs and filters of the screen it left behind', () => {
        const router = routing();
        router.openInSlot('detail', { screen: 'ProductDetails', parameters: { sku: 'NK-101' } });
        router.setQueryParameter('q', 'kettle');

        router.history.push({ screen: 'Orders', url: 'Orders', parameters: {} });
        expect(address()).toBe('/Orders');
        expect(router.getState()).toEqual({ slots: {}, tabs: {}, query: {} });
    });
});

describe('when screens are addressed by the fragment', () => {
    it('should route the slot in the fragment', () => {
        window.history.replaceState(null, '', '/#/ProductCatalog');
        routing('hash').openInSlot('detail', { screen: 'ProductDetails', parameters: { sku: 'NK-101' } });
        expect(window.location.hash).toBe('#/ProductCatalog/@detail/NK-101');
    });
});

describe('when a slot is not routed', () => {
    it('should keep what it shows out of the address', () => {
        routing().openInSlot('activity', { screen: 'SetProductPrice', parameters: {} });
        expect(address()).toBe('/ProductCatalog');
    });
});
