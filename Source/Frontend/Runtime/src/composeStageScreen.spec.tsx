// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { FormGenerationMode, FormWidthUnit, HorizontalAlignment, VerticalAlignment, Visibility, type ExternalComponent, type Screen } from '@cratis/scene.model';
import { composeStageScreen, stageShellOf } from './App';

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
    const scene = { layouts: [], screenTemplates: [{ name: 'Workspace', slots: [], arrangement: undefined }], screens: [screen('Orders', 'Workspace'), screen('Payments')] } as never;

    it('wraps the screen in the blueprint application shell', () => {
        const composed = compose(scene, screens(scene)[0]);
        expect(composed.componentName).toBe('Cratis.Blueprint.Default:appShell');
    });

    it('fills the shell chrome - topbar, sidebar, breadcrumb and footer', () => {
        const composed = compose(scene, screens(scene)[0]);
        expect(Object.keys(composed.slots)).toEqual(expect.arrayContaining(['topbar', 'sidebar', 'breadcrumb', 'footer', 'content']));
    });

    it('lists every screen in the menu and marks the current one', () => {
        const composed = compose(scene, screens(scene)[0]);
        const items = slot(slot(composed, 'menu')[0], 'items').map(_ => _.properties.targetScreen);
        expect(items).toEqual(['Orders', 'Payments']);
    });

    it('leaves the topbar and sidebar without a region of their own when the model declares no navigation regions', () => {
        const composed = compose(scene, screens(scene)[0]);
        expect([slot(composed, 'topbar')[0].properties.regionNavigation, slot(composed, 'sidebar')[0].properties.regionNavigation]).toEqual([false, false]);
    });

    it('places a screen that names a template inside that template', () => {
        const composed = compose(scene, screens(scene)[0]);
        expect(slot(composed, 'content')[0].componentName).toBe('Scene:template');
    });

    it('places a screen with no template straight into the shell content slot', () => {
        const composed = compose(scene, screens(scene)[1]);
        expect(slot(composed, 'content').map(_ => _.componentName)).toContain('core:title');
    });

    it('falls back to the installed blueprint\'s shell for an unknown layout, as the Scene engine resolves it, and says so', () => {
        // The shell is the Scene engine's single answer (resolveEffectiveShell), the one the Studio preview uses; the build reports the
        // layout that does not exist (LayoutNotFound), and the engine names it in its diagnostics.
        const custom = { ...screen('Custom'), layout: 'CustomShell' };
        const scene = { layouts: [], screenTemplates: [], screens: [custom] };
        const composed = compose(scene, custom);
        expect(composed.componentName).toBe('Cratis.Blueprint.Default:appShell');
        expect(stageShellOf(scene).diagnostics.map(diagnostic => diagnostic.subject)).toContain('CustomShell');
    });

    it('replaces the template\'s seeded content for a slot the screen fills', () => {
        const templated = screen('Orders', 'Workspace');
        const composed = compose({
            layouts: [],
            screenTemplates: [{ name: 'Workspace', slots: [], arrangement: undefined, content: { body: [element('template-title', 'core:title')] } }],
            screens: [templated],
        }, templated);
        const template = slot(composed, 'content')[0];
        expect(slot(template, 'body').map(_ => _.id)).toEqual(['Orders-body']);
    });

    it('keeps the template\'s seeded content for a slot the screen does not fill', () => {
        const templated = screen('Orders', 'Workspace');
        const composed = compose({
            layouts: [],
            screenTemplates: [{ name: 'Workspace', slots: [], arrangement: undefined, content: { body: [element('template-title', 'core:title')], header: [element('template-header', 'core:title')] } }],
            screens: [templated],
        }, templated);
        const template = slot(composed, 'content')[0];
        expect(slot(template, 'header').map(_ => _.id)).toEqual(['template-header']);
    });

    it('adds native command forms with Scene 4.12 geometry to the composed screen content', () => {
        const layout = {
            columns: [{ index: 1, width: { unit: FormWidthUnit.Fraction, value: 1 } }],
            placements: [{ field: 'orderNumber', row: 1, column: 1 }],
        };
        const withForm = {
            ...screen('Orders'),
            forms: [{
                name: 'Register order',
                forCommand: 'RegisterOrder',
                generationMode: FormGenerationMode.Manual,
                layout,
                fields: [{ name: 'orderNumber', label: 'Order #' }],
            }],
        };
        const composed = compose({ layouts: [], screenTemplates: [], screens: [withForm] }, withForm as Screen);
        const form = slot(composed, 'content').find(_ => _.componentName === 'Stage:commandForm');
        expect(form?.properties.command).toBe('RegisterOrder');
        expect(form?.properties.generationMode).toBe(FormGenerationMode.Manual);
        expect(form?.properties.layout).toEqual(layout);
        expect(form?.properties.fields).toEqual([{ name: 'orderNumber', label: 'Order #' }]);
    });
});

describe('composing the no-UI PetClinic scene', () => {
    const names = ['RegisterOwner', 'UpdateOwner', 'FindOwners', 'AddPet', 'EditPet', 'PetsOfOwner', 'RecordVisit', 'PetVisitHistory', 'RegisterVet', 'VetList'];
    // The no-UI host payload has no profiles, packages or layouts. Its synthesized screens fill content
    // without selecting a layout; the non-null C# Scene model also represents that absence as an empty name.
    const scene = {
        uiProfiles: [], themes: [], layouts: [], screenTemplates: [], moduleTemplates: [], packages: [],
        screens: names.map(name => ({
            name, path: [], forms: [], contributions: [],
            slotContent: { content: [element(`${name}.title`, 'core:title', { text: name })] },
        })),
    };

    it('composes every screen inside the installed default shell rather than Application', () => {
        for (const active of screens(scene)) {
            const composed = compose(scene, active);
            expect(composed.componentName).toBe('Cratis.Blueprint.Default:appShell');
            expect(slot(composed, 'content').map(element => element.id)).toContain(`${active.name}.title`);
        }
        expect(stageShellOf(scene as never).diagnostics).toEqual([]);
    });

    it('keeps navigation to every synthesized PetClinic screen', () => {
        const composed = compose(scene, screens(scene)[0]);
        const items = slot(slot(composed, 'menu')[0], 'items').map(element => element.properties.targetScreen);
        expect(items).toEqual(names);
    });

    it('also accepts the empty layout name emitted by the C# Scene model', () => {
        const fromDotNet = { ...scene, screens: scene.screens.map(screen => ({ ...screen, layout: '' })) };
        expect(compose(fromDotNet, screens(fromDotNet)[0]).componentName).toBe('Cratis.Blueprint.Default:appShell');
        expect(stageShellOf(fromDotNet as never).diagnostics).toEqual([]);
    });
});

describe('composing a screen that fills a domain blueprint template', () => {
    const templated = screen('Leads', 'CrmLeadList');
    const scene = { layouts: [], screenTemplates: [], screens: [templated] } as never;
    const crmLeadList = { name: 'CrmLeadList', slots: [{ name: 'body' }], fitsSlot: 'content', content: { body: [element('crm-lead-list-title', 'core:title')] } };
    const compose = (blueprintTemplates?: unknown[]) =>
        composeStageScreen(scene, templated, [], '', undefined, blueprintTemplates as never) as unknown as ExternalComponent;

    it('places the screen in the template the approved blueprint brought', () => {
        const template = slot(compose([crmLeadList]), 'content')[0];
        expect(template.componentName).toBe('Scene:template');
        expect(slot(template, 'body').map(_ => _.id)).toEqual(['Leads-body']);
    });

    it('does not find the template when no blueprint brought it', () => {
        expect(slot(slot(compose(), 'content')[0], 'body').map(_ => _.id)).not.toContain('crm-lead-list-title');
    });
});
