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
    diagnosePreview: { components: ['core:outletItems'] },
    navigationTree: { components: ['authoredNavigation'], item: 'NavigationNode' },
    stageNavigationModel: { components: [], item: 'OutletNavigationItem' },
    stageSceneRouting: { components: ['core:slot'], item: 'OutletGroup' },
    stageDataSources: { components: [], data: httpRoute },
    stageDialogScreens: { components: ['core:action'], data: httpRoute },
    stageSliceDialogScreen: { components: ['core:action'] },
    SubNavigation: { components: [], data: localItems },
    TimelineGrid: { components: [], data: localItems },
    stageComponents: { components: ['core:table', 'core:action', 'core:toolbarItem'], data: { ...httpRoute, navigateOnRowClickByParameter: 'Row identity parameter, not a screen destination', item: 'Selected row data, not a screen destination' } },
    StageCommandForm: { components: ['Stage:commandForm'], data: httpRoute },
    StageExtensionComponent: { components: ['Stage:extensionComponent'], data: { system: 'Extension system identity', component: 'Extension component identity', properties: 'Extension input data, not screen destinations' } },
    sliceSurface: { components: [], data: { command: 'Command identity used to identify form submits', label: 'Submit label copied while composing a screen object, not a screen id' } },
    StageConfirmDialog: { components: ['Stage:confirmDialog'], item: 'Decision', data: httpRoute },
    StageChrome: { components: ['Stage:navigationTree'], item: 'NavigationNode', data: { activeScreen: 'Current selection highlight, not a destination' } },
    stageSlot: { components: ['core:slot'], data: { showsBy: 'Screen parameter identity', showsFromElement: 'Source component identity', showsFrom: 'Source component identity' } },
    CoreNavigationBar: { components: ['core:navigationBar'], item: 'MenuEntry' },
    ScreenplayCoreComponents: { components: ['core:navigate'] },
    stageFormContinuation: { components: ['Stage:commandForm'] },
    CoreGuardedAction: { components: ['core:guarded-action'], data: { navigateByParameter: 'Subject identity parameter, not a screen destination' } },
};

// Candidate vocabulary includes collection wrappers as well as scalar destinations. It is only a review trigger,
// never a production pruning heuristic. New readers/files fail until classified, including data-only candidates.
const candidate = /screen|destination|navigate|redirect|link|^to$|^href$|^route$|^target$|^items$|^actions$|^shows|^home$|^nodes$|^decisions$|^onSubmit$/i;
type Read = { field: string; item: boolean };

function propertyBag(node: ts.Node, aliases: Set<string>): boolean {
    if (ts.isAsExpression(node) || ts.isParenthesizedExpression(node)) return propertyBag(node.expression, aliases);
    const name = node.getText();
    return name.endsWith('.properties') || aliases.has(name);
}

function propertyRead(node: ts.Node, aliases: Set<string>): Read | undefined {
    if (ts.isElementAccessExpression(node)) return indexedRead(node, aliases);
    if (!ts.isPropertyAccessExpression(node)) return undefined;
    if (propertyBag(node.expression, aliases)) return { field: node.name.text, item: false };
    if (['item', 'node', 'decision'].includes(node.expression.getText())) return { field: node.name.text, item: true };
    return undefined;
}

function indexedRead(node: ts.ElementAccessExpression, aliases: Set<string>): Read | undefined {
    if (!ts.isStringLiteral(node.argumentExpression)) return undefined;
    if (!propertyBag(node.expression, aliases)) return undefined;
    return { field: node.argumentExpression.text, item: false };
}

function callRead(node: ts.Node, aliases: Set<string>): Read | undefined {
    if (!ts.isCallExpression(node)) return undefined;
    const [owner, key] = node.arguments;
    if (!owner || !key) return undefined;
    if (!ts.isStringLiteral(key)) return undefined;
    if (owner.getText() === 'element') return { field: key.text, item: false };
    if (propertyBag(owner, aliases)) return { field: key.text, item: false };
    if (node.expression.getText() === 'recordString') return { field: key.text, item: true };
    return undefined;
}

type ReaderScope = { aliases: Set<string>; variables: Map<string, ts.Expression> };

export function navigationReads(source: string): Read[] {
    const ast = ts.createSourceFile('consumer.tsx', source, ts.ScriptTarget.Latest, true, ts.ScriptKind.TSX);
    const scope: ReaderScope = { aliases: new Set(['properties']), variables: new Map() };
    const reads: Read[] = [];
    visitNodes(ast, node => trackVariable(node, scope));
    visitNodes(ast, node => reads.push(...navigationRead(node, scope)));
    return reads;
}

function trackVariable(node: ts.Node, scope: ReaderScope) {
    if (!ts.isVariableDeclaration(node)) return;
    if (!node.initializer) return;
    if (!ts.isIdentifier(node.name)) return;
    scope.variables.set(node.name.text, node.initializer);
    if (propertyBag(node.initializer, scope.aliases)) scope.aliases.add(node.name.text);
}

function navigationRead(node: ts.Node, scope: ReaderScope): Read[] {
    const read = propertyRead(node, scope.aliases) ?? callRead(node, scope.aliases);
    const reads = candidateRead(read);
    const target = navigationTarget(node);
    if (target) reads.push(...destinationReads(target, scope.aliases, scope.variables));
    return reads;
}

function candidateRead(read: Read | undefined): Read[] {
    if (!read) return [];
    return candidate.test(read.field) ? [read] : [];
}

function navigationTarget(node: ts.Node): ts.Expression | undefined {
    if (ts.isPropertyAssignment(node) && /^(targetScreen|screen)$/.test(node.name.getText())) return node.initializer;
    if (ts.isShorthandPropertyAssignment(node) && /^(targetScreen|screen)$/.test(node.name.text)) return node.name;
    if (!ts.isCallExpression(node)) return undefined;
    if (!/(?:^|\.)(navigate|navigateToScreen|screenHash|toShellHref)$/.test(node.expression.getText())) return undefined;
    return node.arguments[0];
}

function visitNodes(node: ts.Node, action: (node: ts.Node) => void) {
    action(node);
    node.forEachChild(child => visitNodes(child, action));
}

function destinationReads(node: ts.Node, aliases: Set<string>, variables: Map<string, ts.Expression>, seen = new Set<string>()): Read[] {
    const reads: Read[] = [];
    visitNodes(node, child => {
        const read = propertyRead(child, aliases) ?? callRead(child, aliases);
        if (read) reads.push(read);
        if (!ts.isIdentifier(child) || seen.has(child.text)) return;
        const value = variables.get(child.text);
        if (!value) return;
        seen.add(child.text);
        reads.push(...destinationReads(value, aliases, variables, seen));
    });
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
        if (!/\.tsx?$/.test(entry.name) || entry.name.includes('.spec.') || entry.name.includes('.test.')) return [];
        return [{ name: basename(entry.name).replace(/\.tsx?$/, ''), source: readFileSync(path, 'utf8') }];
    });
}
