// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApplicationStructureIndex } from '@cratis/scene.engine';
import { InteractionActionKind, type DestinationReference } from '@cratis/scene.model';
import { matchSceneRoute, sceneRoutesFrom } from '@cratis/scene.react';
import type { StageSceneApplication } from './App';
import { stageScreenFor } from './stageNavigationModel';

type ModelObject = Record<string, unknown>;
type IsAvailable = (screen: string | undefined) => boolean;
const screenReferences = ['targetScreen', 'navigateToScreen', 'initialScreen', 'shows'];
const destinationReferences = ['target', 'destination', 'navigateTo'];

/**
 * The single navigation input for chrome, regions, contributions and opening actions. Resolve destinations against the original
 * scene before pruning, so removing a screen cannot silently retarget a module/feature entry to a different screen.
 */
export function availableNavigationScene<T extends StageSceneApplication>(scene: T, isAvailable: IsAvailable): T {
    const allowed = navigationAllowed(scene, isAvailable);
    const owned = { ...scene, screens: scene.screens.filter(screen => isAvailable(screen.name)) };
    return pruneNavigation(owned, allowed) as T;
}

function navigationContext(scene: StageSceneApplication, isAvailable: IsAvailable) {
    return {
        isAvailable,
        index: new ApplicationStructureIndex(scene.structure ?? { modules: [] }),
        screenFor: stageScreenFor(scene.screens),
        routes: sceneRoutesFrom(scene.screens.map(screen => screen.name)),
        sliceAllowed: (slice: string) => scene.screens.filter(screen => screen.owner === slice).every(screen => isAvailable(screen.name)),
    };
}

type NavigationContext = ReturnType<typeof navigationContext>;

function navigationAllowed(scene: StageSceneApplication, isAvailable: IsAvailable): (value: ModelObject) => boolean {
    const context = navigationContext(scene, isAvailable);
    const destinationAllowed = (destination: DestinationReference) => availableDestination(destination, context);
    return value => referencesAllowed(value, isAvailable, destinationAllowed) && openingAllowed(value, context.index, context.sliceAllowed);
}

function availableDestination(destination: DestinationReference, context: NavigationContext): boolean {
    if (![destination.screen, destination.dialog, destination.slice].every(context.isAvailable)) return false;
    if (destination.slice && !context.sliceAllowed(qualifiedSlice(destination))) return false;
    if (!moduleAvailable(destination, context)) return false;
    if (destination.kind === 'external') return true;
    if (!destination.route) return true;
    const route = destination.route.replace(/^#?\//, '');
    return context.isAvailable(matchSceneRoute(route, context.routes)?.route.screen);
}

function moduleAvailable(destination: DestinationReference, context: NavigationContext): boolean {
    const module = destination.module;
    if (!module) return true;
    const resolvedFeature = destination.feature ? `${module}.${destination.feature}` : context.index.firstFeatureId(module);
    return context.isAvailable(context.screenFor({ target: { module, resolvedFeature } }));
}

function qualifiedSlice(destination: DestinationReference): string {
    return [destination.module, destination.feature, destination.slice].filter(Boolean).join('.');
}

function referencesAllowed(value: ModelObject, isAvailable: IsAvailable, destinationAllowed: (destination: DestinationReference) => boolean): boolean {
    if (!instanceAllowed(value, isAvailable, destinationAllowed)) return false;
    const screens = screenReferences.map(key => value[key]);
    if (value.kind === InteractionActionKind.Navigate) screens.push(value.screen);
    if (value.kind === InteractionActionKind.OpenDialog) screens.push(value.dialogTemplate);
    if (!screens.every(screen => typeof screen !== 'string' || isAvailable(screen))) return false;
    return destinationReferences.map(key => value[key]).filter(isObject).every(destinationAllowed);
}

function instanceAllowed(value: ModelObject, isAvailable: IsAvailable, destinationAllowed: (destination: DestinationReference) => boolean): boolean {
    if (typeof value.instance !== 'string') return true;
    const [kind, ...name] = value.instance.split(':');
    if (['screen', 'dialog'].includes(kind) && !isAvailable(name.join(':'))) return false;
    return contributedPropertyAllowed(value, isAvailable, destinationAllowed);
}

function contributedPropertyAllowed(value: ModelObject, isAvailable: IsAvailable, destinationAllowed: (destination: DestinationReference) => boolean): boolean {
    if (typeof value.path !== 'string') return true;
    const property = value.path.split('.').at(-1) ?? '';
    if (screenReferences.includes(property) && typeof value.value === 'string') return isAvailable(value.value);
    if (destinationReferences.includes(property) && isObject(value.value)) return destinationAllowed(value.value);
    return true;
}

function openingAllowed(value: ModelObject, index: ApplicationStructureIndex, sliceAllowed: (slice: string) => boolean): boolean {
    if (value.opens === 'none') return true;
    if (typeof value.slice === 'string') return sliceAllowed(value.slice);
    if (typeof value.command !== 'string' || value.opens === undefined) return true;
    return index.findSlicesByCommand(value.command).every(sliceAllowed);
}

/** Walk the model's nested items, slots, refinements and behaviors, not just one presentation's menu list. */
function pruneNavigation(value: unknown, allowed: (value: ModelObject) => boolean): unknown {
    if (Array.isArray(value)) return value.map(entry => pruneNavigation(entry, allowed)).filter(entry => entry !== undefined);
    if (!isObject(value)) return value;
    if (!allowed(value)) return undefined;
    const entries = Object.entries(value).map(([key, entry]) => [key, pruneNavigation(entry, allowed)] as const);
    // A contribution/element whose required content/properties was denied must disappear with it, not leave an empty button.
    if (entries.some(([key, entry]) => lostRequiredContent(value, key, entry))) return undefined;
    return Object.fromEntries(entries.filter(([, entry]) => entry !== undefined));
}

function lostRequiredContent(value: ModelObject, key: string, entry: unknown): boolean {
    if (entry !== undefined) return false;
    if (!['content', 'properties', 'value'].includes(key)) return false;
    return isObject(value[key]);
}

function isObject(value: unknown): value is ModelObject {
    if (value === null) return false;
    if (Array.isArray(value)) return false;
    return typeof value === 'object';
}
