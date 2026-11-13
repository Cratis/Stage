// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readdirSync, readFileSync } from 'node:fs';
import { basename, join } from 'node:path';
import ts from 'typescript';
import { navigationItemPolicies, navigationPropertyPolicies, navigationReferencePolicies } from '../navigationReferencePolicies';

type Consumer = { components: string[]; item?: string; data?: Record<string, string> };
const itemPolicies: Record<string, Record<string, unknown>> = { ...navigationReferencePolicies, ...navigationItemPolicies };
const localItems = { items: 'Local selection/timeline data, not screen navigation' };
const httpRoute = { route: 'Command/query HTTP endpoint, not a screen destination' };
const consumers: Record<string, Consumer> = {
    ActionButtonControl: { components: ['actionButton'] },
    Breadcrumb: { components: ['breadcrumb'], item: 'BreadcrumbItem' },
    Logo: { components: ['logo'] },
    MenuItem: { components: ['menuItem'] },
    UserMenu: { components: ['userMenu'], item: 'MenuEntry' },
    BulkBar: { components: [], data: { actions: 'Interaction trigger names, not navigation records' } },
    OrgTree: { components: [], data: { nodes: 'Organization chart records, not screen destinations' } },
    ScenePreviewHost: { components: [], item: 'OutletNavigationItem' },
    SubNavigation: { components: [], data: localItems },
    TimelineGrid: { components: [], data: localItems },
    stageComponents: { components: ['core:table', 'core:action', 'core:toolbarItem'], data: { ...httpRoute, navigateOnRowClickByParameter: 'Row identity parameter, not a screen destination' } },
    StageCommandForm: { components: ['Stage:commandForm'], data: httpRoute },
    StageExtensionComponent: { components: ['Stage:extensionComponent'] },
    StageConfirmDialog: { components: ['Stage:confirmDialog'], item: 'Decision', data: httpRoute },
    StageChrome: { components: ['Stage:navigationTree'], item: 'NavigationNode', data: { activeScreen: 'Current selection highlight, not a destination' } },
    stageSlot: { components: ['core:slot'], data: { showsBy: 'Screen parameter identity', showsFromElement: 'Source component identity', showsFrom: 'Source component identity' } },
    CoreNavigationBar: { components: ['core:navigationBar'], item: 'MenuEntry' },
    CoreGuardedAction: { components: ['core:guarded-action'], data: { navigateByParameter: 'Subject identity parameter, not a screen destination' } },
};

// Candidate vocabulary includes collection wrappers as well as scalar destinations. It is only a review trigger,
// never a production pruning heuristic. New readers/files fail until classified, including data-only candidates.
const candidate = /screen|destination|navigate|^to$|^href$|^route$|^target$|^items$|^actions$|^shows|^home$|^nodes$|^decisions$/i;
type Read = { field: string; item: boolean };

function propertyRead(node: ts.Node): Read | undefined {
    if (!ts.isPropertyAccessExpression(node)) return undefined;
    const receiver = node.expression.getText();
    if (receiver.endsWith('.properties') || receiver === 'properties') return { field: node.name.text, item: false };
    if (['item', 'node', 'decision'].includes(receiver)) return { field: node.name.text, item: true };
    return undefined;
}

function callRead(node: ts.Node): Read | undefined {
    if (!ts.isCallExpression(node)) return undefined;
    const [owner, key] = node.arguments;
    if (!owner || !key || !ts.isStringLiteral(key)) return undefined;
    const name = node.expression.getText();
    if (owner.getText() === 'element' && /^(read\w*|text)$/.test(name)) return { field: key.text, item: false };
    if (name === 'recordString') return { field: key.text, item: true };
    return undefined;
}

export function navigationReads(source: string): Read[] {
    const ast = ts.createSourceFile('consumer.tsx', source, ts.ScriptTarget.Latest, true, ts.ScriptKind.TSX);
    const reads: Read[] = [];
    const visit = (node: ts.Node) => {
        const read = propertyRead(node) ?? callRead(node);
        if (read && candidate.test(read.field)) reads.push(read);
        node.forEachChild(visit);
    };
    visit(ast);
    return reads;
}

export function unclassifiedConsumer(name: string, source: string): string[] {
    const consumer = consumers[name];
    return [...new Set(navigationReads(source).filter(read => !classified(consumer, read)).map(read => `${name}.${read.item ? 'item.' : ''}${read.field}`))];
}

function classified(consumer: Consumer | undefined, read: Read): boolean {
    if (!consumer) return false;
    if (consumer.data?.[read.field]) return true;
    if (read.item) return !!consumer.item && !!itemPolicies[consumer.item]?.[read.field];
    return consumer.components.some(component => !!navigationPropertyPolicies[component]?.[read.field]);
}

export function consumerSources(directory: string): { name: string; source: string }[] {
    return readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
        if (['dist', 'testing', 'node_modules'].includes(entry.name)) return [];
        const path = join(directory, entry.name);
        if (entry.isDirectory()) return consumerSources(path);
        if (!entry.name.endsWith('.tsx') || entry.name.includes('.spec.') || entry.name.includes('.test.')) return [];
        return [{ name: basename(entry.name, '.tsx'), source: readFileSync(path, 'utf8') }];
    });
}
