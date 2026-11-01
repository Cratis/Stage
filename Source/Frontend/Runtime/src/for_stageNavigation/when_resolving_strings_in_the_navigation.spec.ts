// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import type { NavigationModel } from '@cratis/scene.model';
import { stageNavigationModel } from '../stageNavigationModel';

const navigation = {
    application: { regions: [{ region: 'sidebar', items: [{ id: 'a', title: '$strings.nav.a', target: { module: 'M' } }, { id: 'b', title: '$strings.nav.missing' }, { id: 'c', title: 'Plain' }] }] },
    modules: [],
} as unknown as NavigationModel;
const titles = (dictionary?: Record<string, string>) =>
    (stageNavigationModel({ screens: [], navigation }, undefined, dictionary).navigation.application.regions[0].items as { title: string }[]).map(item => item.title);

describe('when the active locale has a dictionary', () => {
    it('replaces a reference with the text of the locale', () => expect(titles({ 'nav.a': 'Innsendinger' })[0]).toBe('Innsendinger'));
    it('leaves a key the dictionary lacks as written', () => expect(titles({ 'nav.a': 'x' })[1]).toBe('$strings.nav.missing'));
    it('leaves literal titles alone', () => expect(titles({ 'nav.a': 'x' })[2]).toBe('Plain'));
});

describe('when there is no dictionary', () => {
    it('keeps the references', () => expect(titles()[0]).toBe('$strings.nav.a'));
});
