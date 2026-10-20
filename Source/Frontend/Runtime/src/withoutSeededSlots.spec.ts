// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import type { Screen } from '@cratis/scene.model';
import { withoutSeededSlots } from './withoutSeededSlots';

const screen = (name: string, template: string | undefined, slotContent: Record<string, unknown[]>): Screen => ({ name, layout: 'AppShell', screenTemplate: template, slotContent, forms: [], contributions: [] } as unknown as Screen);
const listScreen = { name: 'ListScreen', slots: [{ name: 'header' }, { name: 'toolbar' }, { name: 'filters' }, { name: 'list' }, { name: 'bulkBar' }] };

describe('a screen that fills some of the slots of its template', () => {
    const [placed] = withoutSeededSlots([screen('Inbox', 'ListScreen', { header: [{ id: 'h' }], list: [{ id: 'l' }] })], [listScreen]);

    it('keeps what it filled', () => expect([placed.slotContent.header, placed.slotContent.list]).toEqual([[{ id: 'h' }], [{ id: 'l' }]]));
    it('leaves every other slot explicitly empty', () => expect([placed.slotContent.toolbar, placed.slotContent.filters, placed.slotContent.bulkBar]).toEqual([[], [], []]));
});

describe('a screen that fills every slot of its template', () => {
    const original = screen('Inbox', 'Small', { a: [{ id: 'a' }] });

    it('is the same screen', () => expect(withoutSeededSlots([original], [{ name: 'Small', slots: [{ name: 'a' }] }])[0]).toBe(original));
});

describe('a screen that fills none of the slots of its template', () => {
    const original = screen('Gallery', 'ListScreen', {});

    it('shows the template as the blueprint ships it', () => expect(withoutSeededSlots([original], [listScreen])[0]).toBe(original));
});

describe('a screen that names no template', () => {
    const original = screen('Plain', undefined, { content: [{ id: 'c' }] });

    it('is the same screen', () => expect(withoutSeededSlots([original], [listScreen])[0]).toBe(original));
});

describe('a screen that names a template nobody knows', () => {
    const original = screen('Odd', 'Unknown', {});

    it('is the same screen', () => expect(withoutSeededSlots([original], [listScreen])[0]).toBe(original));
});

describe('two templates of the same name', () => {
    const [placed] = withoutSeededSlots([screen('S', 'T', { extra: [{ id: 'e' }] })], [{ name: 'T', slots: [{ name: 'own' }, { name: 'extra' }] }, { name: 'T', slots: [{ name: 'blueprint' }] }]);

    it('uses the first, which is the application\'s own', () => expect(Object.keys(placed.slotContent).sort()).toEqual(['extra', 'own']));
});
