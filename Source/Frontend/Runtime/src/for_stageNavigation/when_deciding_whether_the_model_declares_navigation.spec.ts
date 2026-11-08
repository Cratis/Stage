// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import type { NavigationModel } from '@cratis/scene.model';
import { declaresRegions } from '../stageNavigationModel';

const outlet = (defaults: unknown[]) => ({ name: 'Navigation', kind: 'navigation', slot: 'navigation', defaults });
const navigationOf = (outlets: unknown[], modules: unknown[] = []) => ({ application: { outlets }, modules }) as unknown as NavigationModel;

describe('when the model has no navigation', () => {
    it('declares none', () => expect(declaresRegions(undefined)).toBe(false));
});

describe('when the layout declares a navigation outlet that lists nothing', () => {
    it('declares none, so the authored entries stay the menu', () => expect(declaresRegions(navigationOf([outlet([])]))).toBe(false));
});

describe('when the navigation outlet lists items', () => {
    it('declares navigation', () => expect(declaresRegions(navigationOf([outlet([{ kind: 'navigation', id: 'home', title: 'Home' }])]))).toBe(true));
});

describe('when a module refines or contributes to the navigation', () => {
    it('declares navigation', () => expect(declaresRegions(navigationOf([outlet([])], [{ module: 'Workspaces', contributions: [] }]))).toBe(true));
});
