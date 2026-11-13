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

describe('the availability-filtered navigation view', () => {
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
        const model = { ...scene, screens: [{ ...scene.screens[1], behaviors: [{ bindings: [{ trigger: { kind: 'Signal', signalName: 'Go' }, actions }] }] }] } as unknown as StageSceneApplication;
        expect(availableNavigationScene(model, isAvailable).screens[0].behaviors?.[0].bindings[0].actions).toEqual([actions[2]]);
    });

    it('retains source identity and data without mutating the input', () => {
        const result = availableNavigationScene(scene, isAvailable);
        expect(result.screens.map(screen => screen.name)).toEqual(['Home']);
        expect(scene.screens.map(screen => screen.name)).toEqual(['Restricted', 'Home']);
        expect(result.screens[0]).not.toBe(scene.screens[1]);
    });
});
