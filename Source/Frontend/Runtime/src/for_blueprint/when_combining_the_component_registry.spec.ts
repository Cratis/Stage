// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect, it } from 'vitest';
import { coreComponents } from '@cratis/scene.react';
import { primeReactComponents } from '@cratis/scene.primereact';
import { defaultBlueprintComponents, previewComponents } from '@cratis/scene.blueprint.default';
import { stageRegistry } from '../blueprint';

it('combines core, PrimeReact, default and preview components before host overrides', () => {
    const override = () => null;
    const key = Object.keys(coreComponents)[0];
    for (const registry of [coreComponents, primeReactComponents, defaultBlueprintComponents, previewComponents]) {
        expect(Object.keys(registry).length).toBeGreaterThan(0);
    }
    const combined = stageRegistry({ [key]: override });
    expect(combined).toEqual({ ...coreComponents, ...primeReactComponents, ...defaultBlueprintComponents, ...previewComponents, [key]: override });
    expect(combined[key]).toBe(override);
});
