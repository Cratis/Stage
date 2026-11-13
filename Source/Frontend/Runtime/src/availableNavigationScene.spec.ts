// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { InteractionActionKind, SliceKind } from '@cratis/scene.model';
import type { StageSceneApplication } from './App';
import { availableNavigationScene } from './availableNavigationScene';

const screenOf = (name: string, owner: string) => ({ name, owner, path: owner.split('.').slice(0, -1), slice: owner, viewModelId: name, layout: '', forms: [], contributions: [], routeParameterBindings: {}, slotContent: {} });
const scene: StageSceneApplication = {
    layouts: [], screenTemplates: [],
    screens: [screenOf('Restricted', 'Work.Private.Edit'), screenOf('Home', 'Work.Public.List')],
    structure: { modules: [{ id: 'Work', name: 'Work', features: [
        { id: 'Private', name: 'Private', features: [], slices: [{ id: 'Edit', name: 'Edit', kind: SliceKind.StateChange, command: 'EditPrivate' }] },
        { id: 'Public', name: 'Public', features: [], slices: [{ id: 'List', name: 'List', kind: SliceKind.StateView }] },
    ], slices: [] }] },
};
const isAvailable = (name: string | undefined) => name !== 'Restricted';

function navigationItems(items: Record<string, unknown>[]) {
    const model = { ...scene, navigation: { application: { outlets: [{ name: 'sidebar', kind: 'navigation', defaults: items }] }, modules: [] } } as unknown as StageSceneApplication;
    return availableNavigationScene(model, isAvailable).navigation?.application?.outlets[0].defaults;
}

function authoredStrings(value: unknown): string[] {
    if (typeof value === 'string') return [value];
    if (Array.isArray(value)) return value.flatMap(authoredStrings);
    if (value === null || typeof value !== 'object') return [];
    return Object.entries(value).flatMap(([key, entry]) => [key, ...authoredStrings(entry)]);
}

const futureReferenceShapes = [
    { name: 'scalar', wrap: (id: string): unknown => id },
    { name: 'array', wrap: (id: string): unknown => [id] },
    { name: 'unknown wrapper', wrap: (id: string): unknown => ({ futureTarget: id }) },
    { name: 'nested wrappers', wrap: (id: string): unknown => ({ outer: { futureNestedTarget: id } }) },
    { name: 'dictionary key', wrap: (id: string): unknown => ({ [id]: { label: 'Private entry' } }) },
    { name: 'literal binding', wrap: (id: string): unknown => ({ kind: 'literal', path: '', value: id }) },
    { name: 'screen owner', wrap: (id: string): unknown => `screen:${id}` },
    { name: 'dialog owner', wrap: (id: string): unknown => `dialog:${id}` },
    { name: 'hash link', wrap: (id: string): unknown => `#/${id}?from=future` },
    { name: 'path link', wrap: (id: string): unknown => `/${id}` },
];

describe('the availability-filtered navigation view', () => {
    it.each(futureReferenceShapes)('guards references in future properties regardless of field name ($name)', ({ wrap }) => {
        const denied = 'FuturePrivateScreen';
        for (let index = 0; index < 8; index++) {
            const key = `newPropertyNotKnownToTheFilter${index}`;
            const model = {
                ...scene,
                screens: [{ ...scene.screens[0], name: denied }, scene.screens[1]],
                [key]: wrap(denied),
                allowedFutureProperty: 'Home',
            };
            const result = availableNavigationScene(model, name => name !== denied);
            expect(authoredStrings(result).some(value => value.includes(denied))).toBe(false);
            expect(result.allowedFutureProperty).toBe('Home');
            expect(result.screens.map(screen => screen.name)).toEqual(['Home']);
        }
    });

    it('preserves ordinary strings with percent signs while refusing malformed authored links', () => {
        const model = { ...scene, futureItems: [
            { label: 'CSS', width: '100%' },
            { label: 'Malformed link', href: '#/Restricted%ZZ' },
        ] };
        expect(availableNavigationScene(model, isAvailable).futureItems).toEqual([model.futureItems[0]]);
    });

    it('guards composite destinations in new wrapper fields without an enumerated wrapper-name list', () => {
        const model = { ...scene, newDestinationProperty: { module: 'Work', feature: 'Private' } };
        expect(availableNavigationScene(model, isAvailable).newDestinationProperty).toBeUndefined();
    });

    it('prunes homeTargetScreen without treating it as a special field', () => {
        const model = { ...scene, futureItems: [
            { id: 'private-breadcrumb', homeTargetScreen: 'Restricted' },
            { id: 'public-breadcrumb', homeTargetScreen: 'Home' },
        ] };
        expect(availableNavigationScene(model, isAvailable).futureItems).toEqual([model.futureItems[1]]);
    });
    it('resolves module and feature entries before removing screens, without retargeting to an available feature', () => {
        expect(navigationItems([
            { kind: 'navigation', id: 'module', title: 'Work', target: { module: 'Work' } },
            { kind: 'navigation', id: 'private', title: 'Private', target: { module: 'Work', feature: 'Private' } },
            { kind: 'navigation', id: 'public', title: 'Public', target: { module: 'Work', feature: 'Public' } },
        ])?.map(item => item.id)).toEqual(['public']);
    });

    it('filters nested destinations, route-only links and slice-opening actions, but retains direct commands and external links', () => {
        expect(navigationItems([
            { kind: 'navigation', id: 'group', title: 'Group', children: [
                { kind: 'navigation', id: 'denied', title: 'Denied', target: { screen: 'Restricted' } },
                { kind: 'navigation', id: 'allowed', title: 'Allowed', target: { screen: 'Home' } },
            ] },
            { kind: 'navigation', id: 'route', title: 'Private route', target: { route: '/Restricted' } },
            { kind: 'button', id: 'slice', title: 'Open slice', slice: 'Work.Private.Edit' },
            { kind: 'button', id: 'command-dialog', title: 'Open command', command: 'EditPrivate', opens: 'dialog' },
            { kind: 'button', id: 'command-automatic', title: 'Open automatically', command: 'EditPrivate' },
            { kind: 'Action', id: 'legacy-command', title: 'Legacy command', target: 'EditPrivate' },
            { kind: 'button', id: 'run', title: 'Run', slice: 'Work.Private.Edit', opens: 'none' },
            { kind: 'navigation', id: 'external', title: 'External', target: { kind: 'external', route: 'https://example.invalid' } },
        ])).toEqual([
            { kind: 'navigation', id: 'group', title: 'Group', children: [{ kind: 'navigation', id: 'allowed', title: 'Allowed', target: { screen: 'Home' } }] },
            { kind: 'button', id: 'run', title: 'Run', slice: 'Work.Private.Edit', opens: 'none' },
            { kind: 'navigation', id: 'external', title: 'External', target: { kind: 'external', route: 'https://example.invalid' } },
        ]);
    });

    it('filters instance contribution destinations and owners, including scalar exposed navigation properties', () => {
        const model = { ...scene, instanceContributions: [
            { instance: 'screen:Home', component: 'nav', path: 'targetScreen', value: 'Restricted' },
            { instance: 'screen:Restricted', component: 'nav', path: 'targetScreen', value: 'Home' },
            { instance: 'screen:Home', component: 'nav', path: 'navigateTo', value: { screen: 'Restricted' } },
            { instance: 'screen:Home', component: 'nav', path: 'targetScreen', value: 'Home' },
        ] };
        expect(availableNavigationScene(model, isAvailable).instanceContributions).toEqual([model.instanceContributions[3]]);
    });

    it('removes denied behavior actions without stripping unrelated command behavior', () => {
        const actions = [
            { kind: InteractionActionKind.Navigate, screen: 'Restricted' },
            { kind: InteractionActionKind.OpenDialog, dialogTemplate: 'Restricted' },
            { kind: InteractionActionKind.ExecuteCommand, command: 'EditPrivate' },
        ];
        const model = { ...scene, screens: [scene.screens[0], { ...scene.screens[1], behaviors: [{ bindings: [{ trigger: { kind: 'Signal', signalName: 'Go' }, actions }] }] }] } as unknown as StageSceneApplication;
        expect(availableNavigationScene(model, isAvailable).screens[0].behaviors?.[0].bindings[0].actions).toEqual([actions[2]]);
    });

    it('retains source identity and data without mutating the input', () => {
        const result = availableNavigationScene(scene, isAvailable);
        expect(result.screens.map(screen => screen.name)).toEqual(['Home']);
        expect(scene.screens.map(screen => screen.name)).toEqual(['Restricted', 'Home']);
        expect(result.screens[0]).not.toBe(scene.screens[1]);
    });
});
