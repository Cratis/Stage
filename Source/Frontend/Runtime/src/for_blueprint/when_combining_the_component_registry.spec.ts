// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect, it } from 'vitest';
import { coreComponents } from '@cratis/scene.react';
import { cratisComponents } from '@cratis/scene.components/runtime';
import { primeReactComponents } from '@cratis/scene.primereact';
import { defaultBlueprintComponents, previewComponents } from '@cratis/scene.blueprint.default';
import { primeReactLibrary } from '../primereact';
import { registerStageLibrary, stageLibraryComponent, stageRegistry } from '../blueprint';
import { stageRowMenuComponent } from '../stageRowMenuContext';

const override = () => null;
const key = Object.keys(coreComponents)[0];

it('combines core, Cratis.Components, default and preview components before host overrides, without PrimeReact', () => {
    for (const registry of [coreComponents, cratisComponents, primeReactComponents, defaultBlueprintComponents, previewComponents]) {
        expect(Object.keys(registry).length).toBeGreaterThan(0);
    }
    const combined = stageRegistry({ [key]: override });
    expect(combined).toEqual({ ...coreComponents, ...cratisComponents, ...defaultBlueprintComponents, ...previewComponents, [key]: override });
    expect(Object.keys(combined).some(name => name.startsWith('PrimeReact:'))).toBe(false);
    expect(stageLibraryComponent(stageRowMenuComponent)).toBeUndefined();
});

it('adds the PrimeReact components once the host has registered the library', () => {
    registerStageLibrary('PrimeReact', primeReactLibrary);
    const combined = stageRegistry({ [key]: override });
    expect(combined).toEqual({ ...coreComponents, ...cratisComponents, ...primeReactLibrary.components, ...defaultBlueprintComponents, ...previewComponents, [key]: override });
    expect(stageLibraryComponent(stageRowMenuComponent)).toBe(primeReactLibrary.components[stageRowMenuComponent]);
    expect(combined[key]).toBe(override);
});
