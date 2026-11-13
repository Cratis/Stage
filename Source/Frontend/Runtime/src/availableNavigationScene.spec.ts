// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { InteractionActionKind, InteractionTriggerKind, SliceKind } from '@cratis/scene.model';
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
    it('preserves layout, template and slot namespaces when they collide with a denied screen', () => {
        const slot = { name: 'Restricted', routed: true };
        const layout = { name: 'Restricted', slots: [slot] };
        const template = { name: 'Restricted', slots: [slot], fitsSlot: 'Restricted' };
        const home = { ...scene.screens[1], layout: 'Restricted', screenTemplate: 'Restricted', slotContent: { Restricted: [] } };
        const model = { ...scene, layouts: [layout], screenTemplates: [template], screens: [scene.screens[0], home] };
        const result = availableNavigationScene(model, isAvailable);
        expect(result.screens).toEqual([home]);
        expect(result.layouts).toEqual([layout]);
        expect(result.screenTemplates).toEqual([template]);
        expect(result.screens[0].slotContent).toEqual({ Restricted: [] });
    });

    it('preserves data ids, dictionary keys, literal bindings and opaque component properties with colliding names', () => {
        const properties = { id: 'Restricted', label: 'Restricted', width: '100%', rows: [{ id: 'Restricted', homeTargetScreen: 'Restricted' }],
            data: { Restricted: 'Restricted' }, binding: { kind: 'literal', value: 'Restricted' } };
        const element = { id: 'Restricted', name: 'Restricted', componentName: 'core:table', properties, slots: {} };
        const home = { ...scene.screens[1], slotContent: { content: [element] } };
        const model = { ...scene, screens: [scene.screens[0], home], commandSchemas: { Restricted: 'Restricted' } };
        const result = availableNavigationScene(model, isAvailable);
        expect(result.screens).toEqual([home]);
        expect(result.commandSchemas).toEqual(model.commandSchemas);
        expect(result.structure).toEqual(scene.structure);
    });

    it('refuses malformed routes only at a semantic destination path', () => {
        expect(navigationItems([{ kind: 'navigation', id: 'bad', title: 'Bad', target: { route: '#/Restricted%ZZ' } }])).toEqual([]);
        const model = { ...scene, data: { href: '#/Restricted%ZZ', homeTargetScreen: 'Restricted' } };
        expect(availableNavigationScene(model, isAvailable).data).toEqual(model.data);
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

    it('filters exposed collection entries by their consumer shape, without reinterpreting data property paths', () => {
        const model = { ...scene, instanceContributions: [
            { instance: 'screen:Home', component: 'menu', path: 'items', items: [
                { id: 'Restricted', values: { label: 'Restricted', destination: { screen: 'Home' } } },
                { id: 'denied', values: { destination: { screen: 'Restricted' } } },
            ] },
            { instance: 'screen:Home', component: 'core:table', path: 'data.targetScreen', value: 'Restricted' },
        ] };
        const result = availableNavigationScene(model, isAvailable).instanceContributions;
        expect(result?.[0].items).toEqual([model.instanceContributions[0].items?.[0]]);
        expect(result?.[1]).toEqual(model.instanceContributions[1]);
    });

    it('filters destinations in runtime navigation trees and decision-dialog controls', () => {
        const controls = [
            { id: 'tree', componentName: 'Stage:navigationTree', properties: { nodes: [
                { label: 'Available', screen: 'Home', children: [] },
                { label: 'Denied', screen: 'Restricted', children: [] },
            ] }, slots: {} },
            { id: 'decisions', componentName: 'Stage:confirmDialog', properties: { home: 'Home', decisions: [
                { command: 'Keep', navigateToScreen: 'Home' },
                { command: 'Remove', navigateToScreen: 'Restricted' },
            ] }, slots: {} },
        ];
        const model = { ...scene, screens: [{ ...scene.screens[1], slotContent: { content: controls } }] };
        const content = availableNavigationScene(model, isAvailable).screens[0].slotContent.content;
        expect(content[0].properties.nodes).toEqual([controls[0].properties.nodes?.[0]]);
        expect(content[1].properties.decisions).toEqual([controls[1].properties.decisions?.[0]]);
    });

    it('checks the action-button fallback only when Click does not override it', () => {
        const button = (id: string, trigger: InteractionTriggerKind) => ({ id, componentName: 'actionButton', properties: { targetScreen: 'Restricted' }, slots: {}, behaviors: [{
            name: 'Home', bindings: [{ trigger: { kind: trigger }, actions: [{ kind: InteractionActionKind.Navigate, screen: 'Home' }] }],
        }] });
        const model = { ...scene, screens: [{ ...scene.screens[1], slotContent: { content: [
            button('click', InteractionTriggerKind.Click), button('change', InteractionTriggerKind.Change),
        ] } }] };
        expect(availableNavigationScene(model, isAvailable).screens[0].slotContent.content.map(element => element.id)).toEqual(['click']);
    });

    it('filters authored form-response redirects without removing allowed continuations', () => {
        const actions = [{ kind: InteractionActionKind.Navigate, screen: 'Home' }, { kind: InteractionActionKind.Navigate, screen: 'Restricted' }];
        const model = { ...scene, screens: [{ ...scene.screens[1], slotContent: { content: [
            { id: 'form', componentName: 'Stage:commandForm', properties: { command: 'Save', onSubmit: actions }, slots: {} },
        ] } }] };
        expect(availableNavigationScene(model, isAvailable).screens[0].slotContent.content[0].properties.onSubmit).toEqual([actions[0]]);
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
