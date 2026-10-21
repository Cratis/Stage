// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import type { ExternalComponent, Screen, ScreenTemplate } from '@cratis/scene.model';
import { composeStageScreen } from './App';
import { standaloneScreenTemplates } from './standaloneScreenTemplates';

const template = (name: string, fitsSlot?: string) => ({ name, fitsSlot, slots: [], content: {} }) as unknown as ScreenTemplate;
const slot = (parent: ExternalComponent, name: string) => (parent.slots[name] ?? []) as unknown as ExternalComponent[];

describe('standalone screen templates', () => {
    const templates = standaloneScreenTemplates([template('WorkspaceScreen', 'content'), template('MasterDetailScreen', 'WorkspaceScreen.moduleContent'), template('Board', 'AppShell.content'), template('Plain')]);
    const fitsOf = (name: string) => templates.find(candidate => candidate.name === name)!.fitsSlot;

    it('places a template that fits another template\'s slot in the content slot', () => expect(fitsOf('MasterDetailScreen')).toBe('content'));
    it('keeps a template that fits the content slot', () => expect(fitsOf('WorkspaceScreen')).toBe('content'));
    it('keeps a slot qualified by something that is not a template, such as the layout', () => expect(fitsOf('Board')).toBe('AppShell.content'));
    it('keeps a template that fits nothing', () => expect(fitsOf('Plain')).toBeUndefined());
});

// Acme Purchasing's Suppliers screen names the default blueprint's MasterDetailScreen and fills its list and details (StudioIssues: master-detail on Play).
describe('composing a screen that names a template which fits inside another one', () => {
    const suppliers = {
        name: 'Suppliers', layout: 'AppShell', screenTemplate: 'MasterDetailScreen', forms: [], contributions: [],
        slotContent: { list: [], details: [] },
    } as unknown as Screen;
    const scene = { layouts: [], screenTemplates: [], screens: [suppliers] } as never;
    const content = slot(composeStageScreen(scene, suppliers, [], '') as unknown as ExternalComponent, 'content');

    const ids = content.map(element => element.id);

    it('composes the template the screen names in the content slot', () => expect(ids).toContain('template-Suppliers-MasterDetailScreen'));
    it('does not wrap it in the workspace template', () => expect(ids.filter(id => id.startsWith('template-'))).toEqual(['template-Suppliers-MasterDetailScreen']));
    it('does not draw the workspace module navigation beside it', () => expect(JSON.stringify(content)).not.toContain('ModuleSections'));
});
