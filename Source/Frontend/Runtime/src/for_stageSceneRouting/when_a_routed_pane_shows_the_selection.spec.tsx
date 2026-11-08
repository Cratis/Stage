// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, cleanup, fireEvent, render, screen } from '../testing/render';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import type { ExternalComponent } from '@cratis/scene.model';
import { SceneRouterProvider, SceneSlotContext, type ComponentRegistry } from '@cratis/scene.react';
import { StageDataProvider, useStageData } from '../stageData';
import { StageSlot } from '../stageSlot';
import { stageComponents } from '../stageComponents';
import { StageRouteShapeContext, createStageSceneRouting, stageRouteShapeOf, useStageSceneRouter, type StageSceneRouting } from '../stageSceneRouting';
import { catalog } from './given/a_catalog';

const shape = stageRouteShapeOf(catalog);
const detail = catalog.screens![0].slotContent.detail[0] as ExternalComponent;
const details = { id: 'product-details', name: 'product-details', componentName: 'core:title', properties: { text: 'Product details' }, slots: {} } as unknown as ExternalComponent;
const registry = stageComponents as unknown as ComponentRegistry;
const routings: StageSceneRouting[] = [];

function Products() {
    const data = useStageData();
    return (
        <>
            <button type='button' onClick={() => data.selectRow('products', { sku: 'NK-101', name: 'Nordic Kettle' })}>Nordic Kettle</button>
            <button type='button' onClick={() => data.selectRow('products', { sku: 'FM-220', name: 'Fjord Mug' })}>Fjord Mug</button>
        </>
    );
}

function Page({ routing }: { routing: StageSceneRouting }) {
    const router = useStageSceneRouter(routing);
    return (
        <StageRouteShapeContext.Provider value={shape}>
            <SceneRouterProvider router={router}>
                <SceneSlotContext.Provider value={{ screens: { ProductDetails: details }, registry }}>
                    <StageDataProvider routes={{ commands: {} }} locale='en' locales={['en']} screen='ProductCatalog'>
                        <Products />
                        <StageSlot element={detail} slots={{}} />
                    </StageDataProvider>
                </SceneSlotContext.Provider>
            </SceneRouterProvider>
        </StageRouteShapeContext.Provider>
    );
}

// What a reload does: a new router reads the address, and the page is drawn again from it.
function load() {
    const routing = createStageSceneRouting(window, 'path', shape);
    routings.push(routing);
    routing.history.listen(() => undefined);
    return render(<Page routing={routing} />);
}

function pane() {
    return document.querySelector('[data-scene-slot="detail"]');
}

beforeEach(() => window.history.replaceState(null, '', '/ProductCatalog'));
afterEach(() => {
    cleanup();
    routings.splice(0).forEach(routing => routing.dispose());
    window.history.replaceState(null, '', '/');
});

describe('when a row is selected for a routed pane that shows the selection', () => {
    it('should keep the optional pane hidden until something is selected', () => {
        load();
        expect(pane()).toBeNull();
    });

    it('should show the selected product in the pane', async () => {
        load();
        fireEvent.click(screen.getByRole('button', { name: 'Nordic Kettle' }));
        expect(await screen.findByText('Product details')).toBeDefined();
        expect(pane()?.getAttribute('data-scene-slot-route')).toBe('NK-101');
    });

    it('should put the selection in the address', async () => {
        load();
        fireEvent.click(screen.getByRole('button', { name: 'Nordic Kettle' }));
        await screen.findByText('Product details');
        expect(window.location.pathname).toBe('/ProductCatalog/@detail/NK-101');
    });

    it('should show the same product after a reload, or from a shared link', async () => {
        window.history.replaceState(null, '', '/ProductCatalog/@detail/FM-220');
        load();
        expect(await screen.findByText('Product details')).toBeDefined();
        expect(pane()?.getAttribute('data-scene-slot-screen')).toBe('ProductDetails');
        expect(pane()?.getAttribute('data-scene-slot-route')).toBe('FM-220');
    });

    it('should go back to the product selected before', async () => {
        load();
        fireEvent.click(screen.getByRole('button', { name: 'Nordic Kettle' }));
        await screen.findByText('Product details');
        fireEvent.click(screen.getByRole('button', { name: 'Fjord Mug' }));
        expect(pane()?.getAttribute('data-scene-slot-route')).toBe('FM-220');

        await act(async () => {
            const popped = new Promise(resolve => window.addEventListener('popstate', () => setTimeout(resolve, 0), { once: true }));
            window.history.back();
            await popped;
        });
        expect(pane()?.getAttribute('data-scene-slot-route')).toBe('NK-101');
        expect(window.location.pathname).toBe('/ProductCatalog/@detail/NK-101');
    });

    it('should empty the pane and the address when it is closed', async () => {
        load();
        fireEvent.click(screen.getByRole('button', { name: 'Nordic Kettle' }));
        await screen.findByText('Product details');
        fireEvent.click(screen.getByRole('button', { name: 'Close' }));
        expect(pane()).toBeNull();
        expect(window.location.pathname).toBe('/ProductCatalog');
    });
});
