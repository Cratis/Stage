// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import checkedIn from '../../../Conformance.Specs/for_SceneEnumerationNaming/scene-model-enumerations.json';

// The .NET side writes each Scene enumeration the way the TypeScript model spells it, from the checked-in map
// `Source/Conformance.Specs/for_SceneEnumerationNaming/scene-model-enumerations.json`. That map is only worth anything
// while it is the installed package's real declarations, so this is where it is held to them.
const sources = import.meta.glob('../../node_modules/@cratis/scene.model/dist/esm/**/*.js', { query: '?raw', import: 'default', eager: true }) as Record<string, string>;

function declaredEnumerations(): Record<string, string> {
    const found: Record<string, string> = {};
    for (const source of Object.values(sources)) {
        for (const match of source.matchAll(/(\w+)\["\w+"\] = "(\w+)"/g)) found[match[1]] ??= match[2];
    }

    return Object.fromEntries(Object.entries(found).sort(([left], [right]) => left.localeCompare(right)));
}

describe('the Scene enumeration naming map', () => {
    it('reads the installed @cratis/scene.model declarations', () => {
        expect(Object.keys(sources).length).toBeGreaterThan(10);
    });

    it('is exactly what the installed @cratis/scene.model declares', () => {
        expect(declaredEnumerations()).toEqual(checkedIn);
    });
});
