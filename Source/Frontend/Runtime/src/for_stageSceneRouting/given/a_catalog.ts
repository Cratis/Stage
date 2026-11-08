// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OutletItemKind, OutletKind, SlotRole, type Screen, type ScreenTemplate } from '@cratis/scene.model';
import type { StageRouteSource } from '../../stageSceneRouting';

const element = (id: string, componentName: string, properties: Record<string, unknown> = {}) => ({ id, name: id, componentName, properties, slots: {} });

/**
 * The catalog of the Screenplay 6 documentation as the Stage receives it: a list template whose page is routed, a detail pane routed by
 * sku, a panel, a stats outlet and an outlet of tabs that owns the detail pane; a feature that adds a status filter; and a screen whose
 * detail shows the product a table has selected.
 */
export const catalog: StageRouteSource = {
    layouts: [{ name: 'AppShell', slots: [{ name: 'content', role: SlotRole.Page, routed: true, primary: true }] }],
    screenTemplates: [{
        name: 'ListPage',
        fitsSlot: 'content',
        slots: [
            { name: 'header' },
            { name: 'main' },
            { name: 'detail', role: SlotRole.Pane, routed: true, routedBy: ['sku'], optional: true },
            { name: 'activity', role: SlotRole.Panel, routed: true },
        ],
        outlets: [
            { name: 'filters', kind: OutletKind.Filters, slot: 'header', defaults: [{ kind: OutletItemKind.Filter, id: 'search', title: 'Search', sets: { query: 'AllProducts', parameter: 'q' } }] },
            { name: 'detailTabs', kind: OutletKind.Tabs, slot: 'detail', defaults: [] },
        ],
    } as ScreenTemplate],
    screens: [
        {
            name: 'ProductCatalog',
            layout: 'AppShell',
            screenTemplate: 'ListPage',
            forms: [],
            contributions: [],
            slotContent: {
                main: [element('products', 'core:table', { query: 'AllProducts' })],
                detail: [element('detail', 'core:slot', { name: 'detail', role: 'pane', routed: true, routedBy: ['sku'], optional: true, shows: 'ProductDetails', showsBy: 'sku', showsFromElement: 'products' })],
            },
        },
        { name: 'ProductDetails', layout: 'AppShell', forms: [], contributions: [], slotContent: { content: [element('product-title', 'core:title', { text: 'Product' })] } },
        { name: 'SetProductPrice', layout: 'AppShell', forms: [], contributions: [], slotContent: { content: [] } },
    ] as Screen[],
    outletLevels: [{
        kind: 'feature',
        name: 'Catalog.Products',
        refinements: [{ outlet: 'filters', items: [{ kind: OutletItemKind.Filter, id: 'status', title: 'Status', sets: { query: 'AllProducts', parameter: 'status' } }] }],
    }] as StageRouteSource['outletLevels'],
};
