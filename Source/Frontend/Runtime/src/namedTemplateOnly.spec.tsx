// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import type { ExternalComponent, Screen } from '@cratis/scene.model';
import { composeStageScreen, stageComposition } from './App';

const slot = (parent: ExternalComponent, name: string) => (parent.slots[name] ?? []) as unknown as ExternalComponent[];

describe('the stage composition', () => {
    const scene = { layouts: [], screenTemplates: [], screens: [] } as never;

    it('composes with only the template the screen names', () => expect(stageComposition(scene, [], '').namedTemplateOnly).toBe(true));
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

describe('composing a screen that names a template which fits an unqualified slot', () => {
    const section = {
        name: 'Overview', layout: 'AppShell', screenTemplate: 'SliceSection', forms: [], contributions: [],
        slotContent: {},
    } as unknown as Screen;
    const scene = { layouts: [], screenTemplates: [], screens: [section] } as never;
    const content = slot(composeStageScreen(scene, section, [], '') as unknown as ExternalComponent, 'content');

    const templateIds = (node: unknown): string[] => {
        const element = node as { id?: string; slots?: Record<string, unknown[]> };
        return [
            ...(element.id?.startsWith('template-') ? [element.id] : []),
            ...Object.values(element.slots ?? {}).flatMap(children => children.flatMap(templateIds)),
        ];
    };

    it('composes only the named template throughout the content tree', () => expect(content.flatMap(templateIds)).toEqual(['template-Overview-SliceSection']));
});
