// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, cleanup, fireEvent, render, screen } from '../testing/render';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { OutletItemKind, OutletKind } from '@cratis/scene.model';
import { OutletView, SceneRouterProvider, outletOfElement } from '@cratis/scene.react';
import { StageRouteShapeContext, createStageSceneRouting, stageRouteShapeOf, useStageFilterArguments, useStageSceneRouter, type StageSceneRouting } from '../stageSceneRouting';
import { catalog } from './given/a_catalog';

const shape = stageRouteShapeOf(catalog);
const routings: StageSceneRouting[] = [];

const filters = outletOfElement('filters', {
    name: 'filters',
    kind: OutletKind.Filters,
    items: [{ kind: OutletItemKind.Filter, id: 'search', title: 'Search', sets: { query: 'AllProducts', parameter: 'q' } }],
}, undefined)!;

const tabs = outletOfElement('detailTabs', {
    name: 'detailTabs',
    kind: OutletKind.Tabs,
    slot: 'detail',
    items: [
        { kind: OutletItemKind.Tab, id: 'overview', title: 'Overview', shows: 'ProductDetails' },
        { kind: OutletItemKind.Tab, id: 'pricing', title: 'Pricing', shows: 'ProductPricing' },
    ],
}, undefined)!;

// What the query of the product list is narrowed by.
function Products() {
    const values = useStageFilterArguments('AllProducts');
    return <output aria-label='AllProducts arguments'>{JSON.stringify(values)}</output>;
}

function Page({ routing }: { routing: StageSceneRouting }) {
    const router = useStageSceneRouter(routing);
    return (
        <StageRouteShapeContext.Provider value={shape}>
            <SceneRouterProvider router={router}>
                <OutletView outlet={filters} elementId='filters' />
                <Products />
                <OutletView outlet={tabs} elementId='detailTabs' />
            </SceneRouterProvider>
        </StageRouteShapeContext.Provider>
    );
}

// What a reload does: a new router reads the address, and the page is drawn again from it.
function load() {
    cleanup();
    const routing = createStageSceneRouting(window, 'path', shape);
    routings.push(routing);
    routing.history.listen(() => undefined);
    return render(<Page routing={routing} />);
}

const search = () => screen.getByPlaceholderText<HTMLInputElement>('Search');
const tab = (name: string) => screen.getByRole('tab', { name });

beforeEach(() => window.history.replaceState(null, '', '/ProductCatalog'));
afterEach(() => {
    cleanup();
    routings.splice(0).forEach(routing => routing.dispose());
    window.history.replaceState(null, '', '/');
});

describe('when a filter is set on a routed page', () => {
    it('should write its value as a query parameter of the address', () => {
        load();
        fireEvent.change(search(), { target: { value: 'kettle' } });
        expect(window.location.search).toBe('?q=kettle');
    });

    it('should narrow the query it sets', () => {
        load();
        fireEvent.change(search(), { target: { value: 'kettle' } });
        expect(screen.getByRole('status', { name: 'AllProducts arguments' }).textContent).toBe('{"q":"kettle"}');
    });

    it('should keep its value after a reload', () => {
        load();
        fireEvent.change(search(), { target: { value: 'kettle' } });
        load();
        expect(search().value).toBe('kettle');
        expect(screen.getByRole('status', { name: 'AllProducts arguments' }).textContent).toBe('{"q":"kettle"}');
    });

    it('should not be a step back and forward go through', () => {
        load();
        const length = window.history.length;
        fireEvent.change(search(), { target: { value: 'kettle' } });
        fireEvent.change(search(), { target: { value: 'kettles' } });
        expect(window.history.length).toBe(length);
    });
});

describe('when a tab is chosen in the routed slot it owns', () => {
    it('should make the tab a segment of the slot in the address', () => {
        load();
        fireEvent.click(tab('Pricing'));
        expect(window.location.pathname).toBe('/ProductCatalog/@detail.detailTabs/pricing');
    });

    it('should choose the same tab after a reload', () => {
        load();
        fireEvent.click(tab('Pricing'));
        load();
        expect(tab('Pricing').getAttribute('aria-selected')).toBe('true');
    });

    it('should go back to the tab chosen before', async () => {
        load();
        fireEvent.click(tab('Pricing'));
        await act(async () => {
            const popped = new Promise(resolve => window.addEventListener('popstate', () => setTimeout(resolve, 0), { once: true }));
            window.history.back();
            await popped;
        });
        expect(tab('Overview').getAttribute('aria-selected')).toBe('true');
    });
});
