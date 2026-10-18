// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { HorizontalAlignment, VerticalAlignment, Visibility, type ExternalComponent, type Screen } from '@cratis/scene.model';
import { composeStageScreen } from './App';

const element = (id: string, componentName: string, properties: Record<string, unknown> = {}) => ({
    id, name: id, componentName, properties, slots: {}, visibility: Visibility.Visible, isEnabled: true, opacity: 1, size: {}, zIndex: 0,
    minimumSize: {}, maximumSize: {}, margin: { left: 0, top: 0, right: 0, bottom: 0 }, horizontalAlignment: HorizontalAlignment.Stretch,
    verticalAlignment: VerticalAlignment.Stretch, borderThickness: { left: 0, top: 0, right: 0, bottom: 0 }, padding: { left: 0, top: 0, right: 0, bottom: 0 }, tabIndex: 0,
} as ExternalComponent);

const screen = { name: 'Sales', layout: 'AppShell', forms: [], contributions: [], slotContent: { content: [element('chart', 'Acme.Charts.chart'), element('bare', 'sparkline')] } } as unknown as Screen;
const scene = { layouts: [], screenTemplates: [], screens: [screen], uiProfiles: [{ name: 'Desktop', targetPlatform: 'web', packages: ['core', 'Acme.Charts'] }] } as never;
const approved = { 'Acme.Charts': ['chart', 'sparkline'] };
const content = (catalog?: Record<string, string[]>) =>
    ((composeStageScreen(scene, screen, [], '', catalog) as unknown as ExternalComponent).slots.content as unknown as ExternalComponent[]).map(_ => _.componentName);

describe('composing a screen that names a component of an approved custom package', () => {
    it('resolves a package-qualified name to the key the package registers it under', () => {
        expect(content(approved)).toContain('Acme.Charts:chart');
    });

    it('resolves a bare name only the approved package declares', () => {
        expect(content(approved)).toContain('Acme.Charts:sparkline');
    });

    it('leaves the names unresolved when the host approved no package, so the placeholder names what is missing', () => {
        expect(content()).toEqual(expect.arrayContaining(['Acme.Charts.chart', 'sparkline']));
    });

    it('never lets an approved package shadow what a Stage package declares', () => {
        const shadowing = { 'Acme.Charts': ['chart', 'sparkline'], core: ['sparkline'] };
        expect(content(shadowing)).not.toContain('core:sparkline');
    });
});
