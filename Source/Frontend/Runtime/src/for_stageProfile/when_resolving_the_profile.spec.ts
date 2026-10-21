// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import type { UiProfile } from '@cratis/scene.model';
import { stageCatalog, stageProfile } from '../blueprint';

const profile = (packages: string[]) => ({ name: 'App', targetPlatform: 'web', packages }) as UiProfile;

describe('when nothing is listed', () => {
    it('orders core, then Cratis.Components, then the blueprint, with no PrimeReact', () => {
        const packages = stageProfile([profile([])]).packages;
        expect(packages[0]).toBe('core');
        expect(packages[1]).toBe('Cratis.Components');
        expect(packages).not.toContain('PrimeReact');
        expect(packages).toHaveLength(3);
    });

    it('names the profile Stage when the scene declares none', () => {
        expect(stageProfile(undefined).name).toBe('Stage');
    });

    it('resolves the shared bare names to Cratis.Components', () => {
        expect(stageCatalog['Cratis.Components']).toContain('dataTable');
    });
});

describe('when PrimeReact is listed', () => {
    const packages = stageProfile([profile(['PrimeReact'])]).packages;

    it('ranks PrimeReact above core and Cratis.Components and below the blueprint', () => {
        expect(packages.slice(0, 3)).toEqual(['core', 'Cratis.Components', 'PrimeReact']);
        expect(packages.indexOf('PrimeReact')).toBeLessThan(packages.length - 1);
    });

    it('never lets core outrank a listed library even when the profile lists core last', () => {
        const listed = stageProfile([profile(['PrimeReact', 'core'])]).packages;
        expect(listed.indexOf('core')).toBeLessThan(listed.indexOf('PrimeReact'));
    });
});
