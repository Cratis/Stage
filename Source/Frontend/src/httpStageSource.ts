// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { StringsDictionary } from '@cratis/scene.engine';
import type { StageRoutes, StageSceneApplication, StageSource } from '@cratis/stage.runtime';

import { readStageNotes, stageEndpoints } from '@cratis/stage.runtime';

/** Reads everything from the endpoints a running Stage host serves. */
export const httpStageSource: StageSource = {
    scene: async signal => {
        const response = await fetch(stageEndpoints.scene, { signal });
        if (!response.ok) throw new Error(`The Stage returned ${response.status}.`);
        return response.json() as Promise<StageSceneApplication>;
    },
    routes: async signal => {
        const response = await fetch(stageEndpoints.routes, { signal });
        return response.ok ? response.json() as Promise<StageRoutes> : undefined;
    },
    locales: async signal => {
        const response = await fetch(stageEndpoints.locales, { signal });
        return response.ok ? response.json() as Promise<string[]> : [];
    },
    strings: async (locale, signal) => {
        const response = await fetch(stageEndpoints.strings(locale), { signal });
        return response.ok ? response.json() as Promise<StringsDictionary> : {};
    },
    notes: signal => readStageNotes(signal),
};
