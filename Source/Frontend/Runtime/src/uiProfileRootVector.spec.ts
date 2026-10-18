// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import type { Screen } from '@cratis/scene.model';
import type { ApplicationDocument } from '@cratis/scene.blueprint.default';
import { effectiveShellOf } from '@cratis/scene.blueprint.default';
import scene from '../../../Conformance.Specs/for_UiProfileRootVector/uiprofile-root.scene.json';
import expected from '../../../Conformance.Specs/for_UiProfileRootVector/uiprofile-root.shells.json';

// The `uiprofile-root` conformance vector: the corpus' ui-profile-root-positive document, as Stage's converter translates it
// (`Source/Conformance.Specs/for_UiProfileRootVector`), resolved to an effective shell by the TypeScript engine. The .NET spec holds
// the Scene engine's .NET resolver to the same `uiprofile-root.shells.json`, so the two engines answer the same for the same profile.
const application = scene as unknown as ApplicationDocument & { uiProfiles: { name: string }[] };
const anyScreen = { name: 'Vector', layout: 'AppShell', forms: [], contributions: [], slotContent: {} } as unknown as Screen;

function projected(profileName: string) {
    const profile = application.uiProfiles.find(candidate => candidate.name === profileName)!;
    const shell = effectiveShellOf(application, anyScreen, { profile: profile as never });
    return {
        name: shell.name,
        kind: shell.kind,
        source: shell.source,
        origin: shell.origin,
        primaryOutlet: shell.primaryOutlet,
        options: Object.fromEntries(Object.entries(shell.options ?? {}).sort(([left], [right]) => left.localeCompare(right))),
        diagnostics: shell.diagnostics.map(diagnostic => diagnostic.code),
    };
}

describe('the uiprofile-root conformance vector', () => {
    it('has the profiles the corpus document declares', () => {
        expect(application.uiProfiles.map(profile => profile.name)).toEqual(Object.keys(expected));
    });

    it.each(Object.keys(expected))('resolves the profile %s to the shell the .NET engine resolves it to', profileName => {
        expect(projected(profileName)).toEqual((expected as Record<string, unknown>)[profileName]);
    });
});
