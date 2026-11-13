// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApplicationStructureIndex } from '@cratis/scene.engine';
import { DestinationReferencePropertyNames, type DestinationReference } from '@cratis/scene.model';
import { matchSceneRoute, sceneRoutesFrom } from '@cratis/scene.react';
import type { StageSceneApplication } from './App';
import { stageScreenFor } from './stageNavigationModel';

type ModelObject = Record<string, unknown>;
type IsAvailable = (screen: string | undefined) => boolean;
interface NavigationRules {
    keyAllowed: (key: string) => boolean;
    referenceAllowed: (reference: string) => boolean;
    objectAllowed: (value: ModelObject) => boolean;
}
const destinationProperties = new Set<string>(DestinationReferencePropertyNames);

/**
 * The single navigation input for chrome, regions, contributions and opening actions. Screen references are recognized
 * by VALUE throughout the authored document, including unknown future properties, not by an allowlist of property names.
 * Resolve composite identities against the original scene, so pruning cannot silently retarget an entry.
 */
export function availableNavigationScene<T extends StageSceneApplication>(scene: T, isAvailable: IsAvailable): T {
    const rules = navigationRules(scene, isAvailable);
    const owned = { ...scene, screens: scene.screens.filter(screen => isAvailable(screen.name)) };
    return pruneNavigation(owned, rules, true) as T;
}

function navigationContext(scene: StageSceneApplication, isAvailable: IsAvailable) {
    const names = [...scene.screens, ...(scene.dialogTemplates ?? [])].map(screen => screen.name);
    return {
        isAvailable,
        denied: new Set(names.filter(name => !isAvailable(name))),
        index: new ApplicationStructureIndex(scene.structure ?? { modules: [] }),
        screenFor: stageScreenFor(scene.screens),
        routes: sceneRoutesFrom(names),
        sliceAllowed: (slice: string) => scene.screens.filter(screen => screen.owner === slice).every(screen => isAvailable(screen.name)),
    };
}

type NavigationContext = ReturnType<typeof navigationContext>;

function navigationRules(scene: StageSceneApplication, isAvailable: IsAvailable): NavigationRules {
    const context = navigationContext(scene, isAvailable);
    return {
        keyAllowed: key => !context.denied.has(key),
        referenceAllowed: reference => referenceAllowed(reference, context),
        objectAllowed: value => compositeDestinationAllowed(value, context) && openingAllowed(value, context),
    };
}

function referenceAllowed(reference: string, context: NavigationContext): boolean {
    if (context.denied.has(reference)) return false;
    // Instance owners carry the same identity using the model's screen:/dialog: protocol.
    const named = /^(?:screen|dialog):(.*)$/.exec(reference)?.[1];
    if (named !== undefined && context.denied.has(named)) return false;
    return routeReferenceAllowed(reference, context);
}

function routeReferenceAllowed(reference: string, context: NavigationContext): boolean {
    try {
        const route = reference.replace(/^#?\//, '');
        const screen = matchSceneRoute(route, context.routes)?.route.screen;
        return screen === undefined || !context.denied.has(screen);
    } catch (error) {
        if (!(error instanceof URIError)) throw error;
        // Authored strings also include CSS such as '100%'. Preserve non-URLs; malformed authored links fail closed.
        return !/^#?\//.test(reference);
    }
}

/** Recognize the destination's type shape, wherever it appears, rather than enumerating its possible wrapper fields. */
function compositeDestinationAllowed(value: ModelObject, context: NavigationContext): boolean {
    const module = value.module;
    if (typeof module !== 'string') return true;
    if (!Object.keys(value).every(key => destinationProperties.has(key))) return true;
    const destination = value as DestinationReference;
    const resolvedFeature = destination.feature ? `${module}.${destination.feature}` : context.index.firstFeatureId(module);
    if (!context.isAvailable(context.screenFor({ target: { module, resolvedFeature } }))) return false;
    const slice = [module, destination.feature, destination.slice].filter(Boolean).join('.');
    return destination.slice === undefined || context.sliceAllowed(slice);
}

function openingAllowed(value: ModelObject, context: NavigationContext): boolean {
    if (value.opens === 'none') return true;
    if (typeof value.slice === 'string') return context.sliceAllowed(value.slice);
    const command = openingCommand(value);
    return command === undefined || context.index.findSlicesByCommand(command).every(context.sliceAllowed);
}

function openingCommand(value: ModelObject): string | undefined {
    const kind = typeof value.kind === 'string' ? value.kind.toLowerCase() : '';
    if (kind === 'action' && typeof value.target === 'string') return value.target;
    if (typeof value.command !== 'string') return undefined;
    if (value.opens !== undefined) return value.command;
    return ['button', 'menuentry', 'action'].includes(kind) ? value.command : undefined;
}

/** A denied leaf removes its enclosing offering; arrays retain other offerings. No field-name knowledge is needed. */
function pruneNavigation(value: unknown, rules: NavigationRules, root = false): unknown {
    if (typeof value === 'string') return rules.referenceAllowed(value) ? value : undefined;
    if (Array.isArray(value)) return value.map(entry => pruneNavigation(entry, rules)).filter(entry => entry !== undefined);
    if (!isObject(value)) return value;
    return pruneObject(value, rules, root);
}

function pruneObject(value: ModelObject, rules: NavigationRules, root: boolean): unknown {
    if (!root && !rules.objectAllowed(value)) return undefined;
    const entries = Object.entries(value).filter(([key]) => rules.keyAllowed(key))
        .map(([key, entry]) => [key, pruneNavigation(entry, rules)] as const);
    // Propagate through arbitrary wrappers, not just today's content/properties/value fields. Keep the document root.
    const deniedChild = entries.some(([key, entry]) => entry === undefined && value[key] !== undefined);
    if (!root && deniedChild) return undefined;
    return Object.fromEntries(entries.filter(([, entry]) => entry !== undefined));
}

function isObject(value: unknown): value is ModelObject {
    if (value === null) return false;
    if (Array.isArray(value)) return false;
    return typeof value === 'object';
}
