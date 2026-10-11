// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import type { ExternalComponent, Screen } from '@cratis/scene.model';
import { composeStageScreen } from './App';
import { authoredNavigationEntries, navigationPathTo, navigationTree } from './navigationTree';

const contribution = (label: string, targetScreen: string, hierarchy: string[], order?: number) => ({
    contributionPointName: 'Navigation',
    order,
    content: { id: `${targetScreen}-nav`, name: targetScreen, componentName: 'core:contribution', properties: { label, targetScreen, hierarchy }, slots: {} },
});

const screen = (name: string, contributions: ReturnType<typeof contribution>[]): Screen => ({
    name, layout: 'AppShell', screenTemplate: undefined, forms: [], contributions, slotContent: {},
} as unknown as Screen);

// A feature's contribution is attached to each of its screens, so Browsing's entry arrives on both of Browsing's screens.
const list = contribution('Invoices', 'InvoiceList', ['Invoicing', 'Browsing'], 0);
const details = contribution('Invoice details', 'InvoiceDetails', ['Invoicing', 'Browsing', 'Details'], 1);
const customers = contribution('Customers', 'CustomerList', ['Crm'], 2);
const screens = [screen('InvoiceList', [list]), screen('InvoiceDetails', [list, details]), screen('CustomerList', [customers])];

describe('the authored navigation hierarchy', () => {
    it('reads each authored entry once', () => {
        expect(authoredNavigationEntries(screens).map(_ => _.targetScreen)).toEqual(['InvoiceList', 'InvoiceDetails', 'CustomerList']);
    });

    it('nests an entry under the module and every feature it was declared in', () => {
        const tree = navigationTree(authoredNavigationEntries(screens));
        expect(tree.map(_ => _.label)).toEqual(['Invoicing', 'Crm']);
        const browsing = tree[0].children[0];
        expect(browsing.label).toBe('Browsing');
        expect(browsing.children.map(_ => _.label)).toEqual(['Invoices', 'Details']);
        expect(browsing.children[1].children.map(_ => _.label)).toEqual(['Invoice details']);
    });

    it('finds the labels leading to a screen', () => {
        const tree = navigationTree(authoredNavigationEntries(screens));
        expect(navigationPathTo(tree, 'InvoiceList')).toEqual(['Invoicing', 'Browsing', 'Invoices']);
        expect(navigationPathTo(tree, 'Unlisted')).toEqual([]);
    });

    it('orders by the authored order, not by the screen order', () => {
        const entries = authoredNavigationEntries([screen('B', [contribution('B', 'B', [], 1)]), screen('A', [contribution('A', 'A', [], 0)])]);
        expect(entries.map(_ => _.targetScreen)).toEqual(['A', 'B']);
    });
});

describe('composing a screen with authored navigation', () => {
    const compose = (name: string) => composeStageScreen({ layouts: [], screenTemplates: [], screens } as never, screens.find(_ => _.name === name)!, [], '') as unknown as ExternalComponent;
    const slot = (parent: ExternalComponent, name: string) => (parent.slots[name] ?? []) as unknown as ExternalComponent[];

    it('puts the navigation tree in the menu instead of one flat entry per screen', () => {
        const menu = slot(compose('InvoiceList'), 'menu')[0];
        expect(menu.componentName).toBe('Stage:navigationTree');
        expect(menu.properties.activeScreen).toBe('InvoiceList');
    });

    it('shows the hierarchy leading to the current screen as the breadcrumb', () => {
        const breadcrumb = slot(compose('InvoiceDetails'), 'breadcrumb')[0];
        expect((breadcrumb.properties.items as { label: string }[]).map(_ => _.label)).toEqual(['Invoicing', 'Browsing', 'Details', 'Invoice details']);
    });

    it('keeps the flat list of screens when the model authored no navigation', () => {
        const plain = [screen('Orders', []), screen('Payments', [])];
        const composed = composeStageScreen({ layouts: [], screenTemplates: [], screens: plain } as never, plain[0], [], '') as unknown as ExternalComponent;
        expect(slot(composed, 'menu')[0].componentName).not.toBe('Stage:navigationTree');
    });
});
