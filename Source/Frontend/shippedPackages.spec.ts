// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import { coreComponents, corePackageManifest } from '@cratis/scene.react';
import { cratisComponentsPackageManifest } from '@cratis/scene.components/runtime';
import { primeReactPackageManifest } from '@cratis/scene.primereact';
import { defaultBlueprintManifest } from '@cratis/scene.blueprint.default';

/**
 * The packages the Stage frontend ships, as the renderer's catalog reads them (`Rendering.Cratis/Shipped/ShippedPackages.json`).
 * The frontend and the renderer must resolve a `ui profile` against the same components, layouts and templates (REN1/REN2), so the
 * document is generated from the manifests the frontend actually renders with and this specification fails when it is stale.
 * Run with `STAGE_UPDATE_SHIPPED_PACKAGES=1` to rewrite it after the Scene packages change.
 */
const documentPath = resolve(process.cwd(), '../Rendering.Cratis/Shipped/ShippedPackages.json');

/**
 * A dependency is kept when the Stage ships it. PrimeReact declares the Tailwind styling package, which the Stage does not ship - it
 * styles PrimeReact itself (`styledPrimeReact`) - so the dependency would never be satisfiable for a profile resolved against the shipped catalog.
 */
const shippedNames = new Set(['core', 'scene.web', 'Cratis.Components', primeReactPackageManifest.name, defaultBlueprintManifest.name]);

function shipped(manifest: { name: string; version: string; kind: string; dependencies?: { name: string; versionRange?: string }[]; components: string[]; layouts: string[]; screenTemplates: string[]; dialogTemplates: string[]; themes: string[] }) {
    return {
        name: manifest.name,
        version: manifest.version,
        kind: manifest.kind,
        dependencies: (manifest.dependencies ?? []).filter(dependency => shippedNames.has(dependency.name)),
        components: manifest.components,
        layouts: manifest.layouts,
        screenTemplates: manifest.screenTemplates,
        dialogTemplates: manifest.dialogTemplates,
        themes: manifest.themes,
    };
}

/**
 * Scene's core manifest lists its components by bare name, while the registry a scene renders with - and so the names a Screenplay
 * directive is translated to - is keyed `core:<name>`. The renderer resolves the names the translation produced, so the shipped
 * core declares both forms, plus the `core:contribution` wrapper the Stage composes contributions with.
 */
const core = {
    ...corePackageManifest,
    components: [...new Set([...corePackageManifest.components, ...Object.keys(coreComponents), 'core:contribution'])],
};

describe('the shipped packages document', () => {
    const expected = `${JSON.stringify([core, cratisComponentsPackageManifest, primeReactPackageManifest, defaultBlueprintManifest].map(shipped), null, 2)}\n`;

    if (process.env.STAGE_UPDATE_SHIPPED_PACKAGES) {
        writeFileSync(documentPath, expected);
    }

    it('matches the manifests of core, Cratis.Components, PrimeReact and the default blueprint', () => {
        expect(readFileSync(documentPath, 'utf8')).toBe(expected);
    });
});
