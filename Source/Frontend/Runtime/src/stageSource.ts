// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, createElement, useContext, type ReactNode } from 'react';
import type { StringsDictionary } from '@cratis/scene.engine';
import type { StageRoutes } from './stageRoutes';
import type { StageSceneApplication } from './App';

/** The content supplied by the host of a Stage application. */
export interface StageSource {
    scene(signal: AbortSignal): Promise<StageSceneApplication>;
    routes(signal: AbortSignal): Promise<StageRoutes | undefined>;
    locales(signal: AbortSignal): Promise<string[]>;
    strings(locale: string, signal: AbortSignal): Promise<StringsDictionary>;
}

/** What a generated application already holds when it is built. */
export interface StageStaticContent {
    scene: StageSceneApplication;
    routes: StageRoutes;
    strings: Record<string, StringsDictionary>;
}

/** Serves the document, routes and dictionaries built into an application. */
export function staticStageSource(content: StageStaticContent): StageSource {
    return {
        scene: async () => content.scene,
        routes: async () => content.routes,
        locales: async () => Object.keys(content.strings),
        strings: async locale => content.strings[locale] ?? {},
    };
}

/** Relative endpoint names shared with the live host's transport. */
export const stageEndpoints = {
    scene: 'stage/scene',
    routes: 'stage/routes',
    locales: 'stage/locales',
    strings: (locale: string) => `stage/strings/${encodeURIComponent(locale)}`,
};

const StageSourceContext = createContext<StageSource | undefined>(undefined);

/** Supplies the source the runtime below reads from. Transport belongs to the host. */
export function StageSourceProvider({ source, children }: { source: StageSource; children?: ReactNode }) {
    return createElement(StageSourceContext.Provider, { value: source }, children);
}

/** The source the runtime reads from. */
export function useStageSource(): StageSource {
    const source = useContext(StageSourceContext);
    if (!source) throw new Error('A StageSourceProvider is required to render a Stage application.');
    return source;
}
