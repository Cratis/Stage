// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import type { ExternalComponent, Screen } from '@cratis/scene.model';
import { composeStageScreen } from './App';
import { authoredNavigationEntries, navigationPathTo, navigationTree, unlistedNavigationEntries } from './navigationTree';

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

    it('carries what the entry was authored with: icon, presentation and where it opens', () => {
        const dialog = { ...contribution('Help', 'HelpScreen', ['Crm']), content: { ...contribution('Help', 'HelpScreen', ['Crm']).content, properties: { label: 'Help', targetScreen: 'HelpScreen', hierarchy: ['Crm'], icon: 'help', presentation: 'icon', destination: { dialog: 'HelpDialog', kind: 'dialog', routeParameterBindings: {} } } } };
        const help = authoredNavigationEntries([screen('HelpScreen', [dialog])])[0];
        expect([help.icon, help.presentation, help.destination?.dialog]).toEqual(['help', 'icon', 'HelpDialog']);
        const [crm] = navigationTree([help]);
        expect([crm.children[0].icon, crm.children[0].presentation, crm.children[0].destination?.dialog]).toEqual(['help', 'icon', 'HelpDialog']);
    });

    it('does not mistake an entry that simply opens its screen for one that opens elsewhere', () => {
        const plain = contribution('Plain', 'Plain', []);
        const withDestination = { ...plain, content: { ...plain.content, properties: { ...plain.content.properties, destination: { screen: 'Plain', routeParameterBindings: {} } } } };
        expect(authoredNavigationEntries([screen('Plain', [withDestination])])[0].destination).toBeUndefined();
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

    it('keeps a screen with no navigation entry reachable under its own name', () => {
        const withUnlisted = [...screens, screen('AuditLog', [])];
        const composed = composeStageScreen({ layouts: [], screenTemplates: [], screens: withUnlisted } as never, withUnlisted[3], [], '') as unknown as ExternalComponent;
        const nodes = slot(composed, 'menu')[0].properties.nodes as { label: string; children: { label: string; screen?: string }[] }[];
        expect(nodes.at(-1)?.label).toBe('Other screens');
        expect(nodes.at(-1)?.children.map(_ => _.screen)).toEqual(['AuditLog']);
        expect((slot(composed, 'breadcrumb')[0].properties.items as { label: string }[]).map(_ => _.label)).toEqual(['Audit log']);
    });

    it('places a screen no entry lists under the module and feature it was declared in', () => {
        const placed = { ...screen('InvoiceAudit', []), path: ['Invoicing', 'Browsing'] } as Screen;
        const withUnlisted = [...screens, placed];
        const composed = composeStageScreen({ layouts: [], screenTemplates: [], screens: withUnlisted } as never, placed, [], '') as unknown as ExternalComponent;
        const nodes = slot(composed, 'menu')[0].properties.nodes as { label: string; children: { label: string; screen?: string; children: { label: string; screen?: string }[] }[] }[];
        const browsing = nodes[0].children[0];
        expect(nodes.map(_ => _.label)).toEqual(['Invoicing', 'Crm']);
        expect(browsing.children.map(_ => _.screen ?? _.label)).toEqual(['InvoiceList', 'Details', 'InvoiceAudit']);
        expect((slot(composed, 'breadcrumb')[0].properties.items as { label: string }[]).map(_ => _.label)).toEqual(['Invoicing', 'Browsing', 'Invoice audit']);
    });

    it('lists the unlisted screens with no recorded place under the fallback group', () => {
        const entries = unlistedNavigationEntries([...screens, screen('AuditLog', [])], new Set(['InvoiceList', 'InvoiceDetails', 'CustomerList']), 'Other screens');
        expect(entries.map(_ => [_.targetScreen, _.hierarchy])).toEqual([['AuditLog', ['Other screens']]]);
    });

    it('keeps the flat list of screens when the model authored no navigation', () => {
        const plain = [screen('Orders', []), screen('Payments', [])];
        const composed = composeStageScreen({ layouts: [], screenTemplates: [], screens: plain } as never, plain[0], [], '') as unknown as ExternalComponent;
        expect(slot(composed, 'menu')[0].componentName).not.toBe('Stage:navigationTree');
    });
});
