// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ApplicationStructure, NavigationModel, Screen } from '@cratis/scene.model';
import type { EffectiveNavItem } from '@cratis/scene.engine';
import { resolveStringsValue } from '@cratis/scene.engine';
import type { SceneNavigationModel } from '@cratis/scene.react';

/** Whether the model declares navigation regions of its own - the application's or a module's. */
export function declaresRegions(navigation: NavigationModel | undefined): boolean {
    return (navigation?.application.regions.length ?? 0) > 0 || (navigation?.modules.length ?? 0) > 0;
}

/**
 * The screen a navigation item opens: the screen declared in the feature the item resolves to (a screen's `path` is the modules and
 * features it was declared in), the first screen declared beneath it, or the screen named like the feature - which is how the
 * application structure names a feature's screen.
 */
export function stageScreenFor(screens: Screen[]): (item: EffectiveNavItem) => string | undefined {
    const byPath = new Map<string, string>();
    for (const screen of screens) {
        const key = (screen.path ?? []).join('.');
        if (key.length > 0 && !byPath.has(key)) byPath.set(key, screen.name);
    }

    return item => {
        const resolved = item.target?.resolvedFeature;
        if (resolved === undefined) return undefined;

        // A feature with no screen of its own opens the first screen declared in a feature beneath it.
        const beneath = screens.find(screen => (screen.path ?? []).join('.').startsWith(`${resolved}.`))?.name;
        return byPath.get(resolved) ?? beneath ?? (screens.some(screen => screen.name === resolved) ? resolved : undefined);
    };
}

/** The module a screen was declared in - the first of its `path` - which decides whose sidebar the page shows. */
export function stageModuleOf(screens: Screen[]): (screen: string | undefined) => string | undefined {
    return screen => screens.find(candidate => candidate.name === screen)?.path?.[0];
}

/**
 * What Scene's navigation regions read: the model's structure and navigation, and the two hooks that are the host's - where an item
 * goes and which items the user may see. Hidden is hidden, never disabled, so nothing about an item the user may not see leaks.
 */
export function stageNavigationModel(
    scene: { screens: Screen[]; navigation?: NavigationModel; structure?: ApplicationStructure },
    isVisible?: (item: EffectiveNavItem) => boolean,
    dictionary?: Record<string, string>,
): SceneNavigationModel {
    const navigation = scene.navigation ?? { application: { regions: [] }, modules: [] };
    return {
        structure: scene.structure ?? { modules: [] },
        navigation: dictionary ? withStringsResolved(navigation, dictionary) : navigation,
        screenFor: stageScreenFor(scene.screens),
        moduleOf: stageModuleOf(scene.screens),
        ...(isVisible ? { isVisible } : {}),
    };
}

/**
 * The navigation with every `$strings.<key>` reference - an item's title, a group's label - replaced by the active locale's text.
 * Screens are resolved when they are composed; navigation is read from the model by Scene's regions, so it is resolved here. A key the
 * dictionary lacks stays as written, so a missing translation is seen rather than blank.
 */
export function withStringsResolved<T>(value: T, dictionary: Record<string, string>): T {
    if (typeof value === 'string') return resolveStringsValue(value, dictionary) as T;
    if (Array.isArray(value)) return value.map(entry => withStringsResolved(entry, dictionary)) as T;
    if (value !== null && typeof value === 'object') {
        return Object.fromEntries(Object.entries(value).map(([key, entry]) => [key, withStringsResolved(entry, dictionary)])) as T;
    }

    return value;
}
