// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { HorizontalAlignment, VerticalAlignment, Visibility, type ExternalComponent, type Screen } from '@cratis/scene.model';
import { composeStageScreen } from './App';

const screens = (scene: unknown) => (scene as { screens: Screen[] }).screens;
const slot = (parent: ExternalComponent, name: string) => (parent.slots[name] ?? []) as unknown as ExternalComponent[];
const compose = (scene: unknown, screen: Screen) => composeStageScreen(scene as never, screen, [], '') as unknown as ExternalComponent;

const element = (id: string, componentName: string, properties: Record<string, unknown> = {}) => ({
    id, name: id, componentName, properties, slots: {}, visibility: Visibility.Visible, isEnabled: true, opacity: 1, size: {}, zIndex: 0,
    minimumSize: {}, maximumSize: {}, margin: { left: 0, top: 0, right: 0, bottom: 0 }, horizontalAlignment: HorizontalAlignment.Stretch,
    verticalAlignment: VerticalAlignment.Stretch, borderThickness: { left: 0, top: 0, right: 0, bottom: 0 }, padding: { left: 0, top: 0, right: 0, bottom: 0 }, tabIndex: 0,
} as ExternalComponent);

const screen = (name: string, template?: string): Screen => ({
    name, layout: 'AppShell', screenTemplate: template, forms: [], contributions: [],
    slotContent: template ? { body: [element(`${name}-body`, 'core:text')] } : { content: [element(`${name}-title`, 'core:title')] },
} as unknown as Screen);

describe('composing a screen in the default blueprint', () => {
    const scene = { layouts: [], screenTemplates: [{ name: 'Workspace', arrangement: undefined }], screens: [screen('Orders', 'Workspace'), screen('Payments')] } as never;

    it('wraps the screen in the blueprint application shell', () => {
        const composed = compose(scene, screens(scene)[0]);
        expect(composed.componentName).toBe('Cratis.Blueprint.Default:appShell');
    });

    it('fills the shell chrome - topbar, sidebar, menu, breadcrumb and footer', () => {
        const composed = compose(scene, screens(scene)[0]);
        expect(Object.keys(composed.slots)).toEqual(expect.arrayContaining(['topbar', 'sidebar', 'menu', 'breadcrumb', 'footer', 'content']));
    });

    it('lists every screen in the menu and marks the current one', () => {
        const composed = compose(scene, screens(scene)[0]);
        const items = slot(slot(composed, 'menu')[0], 'items').map(_ => _.properties.targetScreen);
        expect(items).toEqual(['Orders', 'Payments']);
    });

    it('places a screen that names a template inside that template', () => {
        const composed = compose(scene, screens(scene)[0]);
        expect(slot(composed, 'content')[0].componentName).toBe('Stage:template');
    });

    it('places a screen with no template straight into the shell content slot', () => {
        const composed = compose(scene, screens(scene)[1]);
        expect(slot(composed, 'content').map(_ => _.componentName)).toContain('core:title');
    });

    it('keeps an unsupported layout unresolved rather than substituting a default shell', () => {
        const custom = { ...screen('Custom'), layout: 'CustomShell' };
        const composed = compose({ layouts: [], screenTemplates: [], screens: [custom] }, custom);
        expect(composed.componentName).toBe('CustomShell');
    });

    it('preserves template chrome and screen content in the same authored slot', () => {
        const templated = screen('Orders', 'Workspace');
        const composed = compose({
            layouts: [],
            screenTemplates: [{ name: 'Workspace', arrangement: undefined, content: { body: [element('template-title', 'core:title')] } }],
            screens: [templated],
        }, templated);
        const template = slot(composed, 'content')[0];
        expect(slot(template, 'body').map(_ => _.id)).toEqual(['template-title', 'Orders-body']);
    });

    it('adds native command forms to the composed screen content', () => {
        const withForm = { ...screen('Orders'), forms: [{ name: 'Register order', forCommand: 'RegisterOrder', fields: [{ name: 'orderNumber', label: 'Order #' }] }] };
        const composed = compose({ layouts: [], screenTemplates: [], screens: [withForm] }, withForm as Screen);
        const form = slot(composed, 'content').find(_ => _.componentName === 'Stage:commandForm');
        expect(form?.properties.command).toBe('RegisterOrder');
        expect(form?.properties.fields).toEqual([{ name: 'orderNumber', label: 'Order #' }]);
    });
});
