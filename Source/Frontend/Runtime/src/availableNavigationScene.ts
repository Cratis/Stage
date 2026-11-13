// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApplicationStructureIndex } from '@cratis/scene.engine';
import { InteractionTriggerKind } from '@cratis/scene.model';
import { matchSceneRoute, sceneRoutesFrom } from '@cratis/scene.react';
import type { StageSceneApplication } from './App';
import { arrangementNavigationPolicies, navigationItemPolicies, navigationPropertyPolicies, navigationReferencePolicies, type NavigationFieldPolicy } from './navigationReferencePolicies';
import { stageScreenFor } from './stageNavigationModel';

type ModelObject = Record<string, unknown>;
type IsAvailable = (screen: string | undefined) => boolean;
type Fields = Record<string, NavigationFieldPolicy>;
const policies: Record<string, Fields> = { ...navigationReferencePolicies, ...arrangementNavigationPolicies, ...navigationItemPolicies };
const outletShapes: Record<string, string> = {
    navigation: 'OutletNavigationItem', button: 'OutletButton', menuEntry: 'OutletMenuEntry', group: 'OutletGroup',
    tab: 'OutletTab', filter: 'OutletFilter', stat: 'OutletStat', separator: 'OutletSeparator', Action: 'ToolbarButton',
};

/** One navigable view, interpreting references only in classified semantic paths/shapes, never by matching arbitrary strings. */
export function availableNavigationScene<T extends StageSceneApplication>(scene: T, isAvailable: IsAvailable): T {
    return pruneNode(scene, 'Scene', navigationContext(scene, isAvailable)) as T;
}

function navigationContext(scene: StageSceneApplication, isAvailable: IsAvailable) {
    return {
        scene, isAvailable,
        index: new ApplicationStructureIndex(scene.structure ?? { modules: [] }),
        screenFor: stageScreenFor(scene.screens),
        routes: sceneRoutesFrom(scene.screens.map(screen => screen.name)),
        sliceAllowed: (slice: string) => scene.screens.filter(screen => screen.owner === slice).every(screen => isAvailable(screen.name)),
    };
}
type NavigationContext = ReturnType<typeof navigationContext>;
interface FieldContext { rule: NavigationFieldPolicy; parent: ModelObject; context: NavigationContext }

function selectedShape(shape: string, value: ModelObject): string {
    if (shape === 'OutletItem') return outletShapes[String(value.kind)] ?? 'OutletItemBase';
    if (shape === 'Action') return `${String(value.kind)}Action` in policies ? `${String(value.kind)}Action` : 'InteractionAction';
    if (shape === 'LegacyToolbarButton') return 'ToolbarButton';
    return shape;
}

function pruneNode(value: unknown, shape: string, context: NavigationContext): unknown {
    if (!isObject(value)) return value;
    return pruneFields(value, policies[selectedShape(shape, value)] ?? {}, context);
}

function pruneFields(value: ModelObject, fields: Fields, context: NavigationContext): unknown {
    const result = { ...value };
    for (const [key, rule] of Object.entries(fields)) {
        if (value[key] === undefined) continue;
        const filtered = fieldHandlers[rule.kind](value[key], { rule, parent: value, context });
        if (filtered === undefined) return undefined;
        result[key] = filtered;
    }
    return result;
}

function pruneList(value: unknown, shape: string, context: NavigationContext): unknown {
    if (!Array.isArray(value)) return value;
    return value.map(entry => pruneNode(entry, shape, context)).filter(entry => entry !== undefined);
}

function pruneSlots(value: unknown, context: NavigationContext): unknown {
    if (!isObject(value)) return value;
    // Keys name slots, not screens. Keep them even if an application screen happens to have the same name.
    return Object.fromEntries(Object.entries(value).map(([slot, elements]) => [slot, pruneList(elements, 'Element', context)]));
}

function componentPolicies(name: unknown): Fields {
    if (typeof name !== 'string') return {};
    const component = name.replace(/^Cratis\.Blueprint\.Default:/, '');
    return navigationPropertyPolicies[component] ?? {};
}

function pruneProperties(value: unknown, field: FieldContext): unknown {
    if (!isObject(value)) return value;
    const fields = componentPolicies(field.parent.componentName);
    const active = hasClickBehavior(field.parent.behaviors) ? Object.fromEntries(Object.entries(fields).filter(([, rule]) => rule.kind !== 'clickFallback')) : fields;
    return pruneFields(value, active, field.context);
}

function hasClickBehavior(behaviors: unknown): boolean {
    if (!Array.isArray(behaviors)) return false;
    return behaviors.some(behaviorHasClickBinding);
}

function behaviorHasClickBinding(behavior: unknown): boolean {
    if (!isObject(behavior)) return false;
    if (!Array.isArray(behavior.bindings)) return false;
    return behavior.bindings.some(isClickBinding);
}

function isClickBinding(binding: unknown): boolean {
    if (!isObject(binding)) return false;
    if (!isObject(binding.trigger)) return false;
    return binding.trigger.kind === InteractionTriggerKind.Click;
}

function pruneContribution(value: unknown, field: FieldContext): unknown {
    if (!isObject(value)) return value;
    if (field.parent.contributionPointName !== 'Navigation') return pruneNode(value, 'Element', field.context);
    const properties = isObject(value.properties) ? pruneFields(value.properties, navigationPropertyPolicies.authoredNavigation, field.context) : value.properties;
    if (properties === undefined) return undefined;
    return pruneNode({ ...value, properties }, 'Element', field.context);
}

function allowedScreen(value: unknown, context: NavigationContext): unknown {
    if (typeof value !== 'string') return value;
    return context.isAvailable(value) ? value : undefined;
}

function allowedRoute(value: unknown, field: FieldContext): unknown {
    if (typeof value !== 'string') return value;
    if (field.parent.kind === 'external') return value;
    try {
        const screen = matchSceneRoute(value.replace(/^#?\//, ''), field.context.routes)?.route.screen;
        return field.context.isAvailable(screen) ? value : undefined;
    } catch (error) {
        if (!(error instanceof URIError)) throw error;
        return undefined;
    }
}

function allowedModule(value: unknown, field: FieldContext): unknown {
    if (typeof value !== 'string') return value;
    const feature = field.parent.feature;
    const resolvedFeature = typeof feature === 'string' ? `${value}.${feature}` : field.context.index.firstFeatureId(value);
    return field.context.isAvailable(field.context.screenFor({ target: { module: value, resolvedFeature } })) ? value : undefined;
}

function allowedSlice(value: unknown, field: FieldContext): unknown {
    if (field.parent.opens === 'none') return value;
    if (typeof value !== 'string') return value;
    return field.context.sliceAllowed(value) ? value : undefined;
}

function allowedCommand(value: unknown, field: FieldContext): unknown {
    if (field.parent.opens === 'none') return value;
    if (typeof value !== 'string') return value;
    return field.context.index.findSlicesByCommand(value).every(field.context.sliceAllowed) ? value : undefined;
}

function allowedLegacyCommand(value: unknown, field: FieldContext): unknown {
    if (field.parent.kind === 'navigate') return pruneNode(value, 'DestinationReference', field.context);
    if (field.parent.kind === 'dialog') return allowedScreen(value, field.context);
    return allowedCommand(value, field);
}

function allowedInstanceOwner(value: unknown, context: NavigationContext): unknown {
    if (typeof value !== 'string') return value;
    const screen = /^(?:screen|dialog):(.*)$/.exec(value)?.[1];
    return context.isAvailable(screen) ? value : undefined;
}

function instancePolicy(parent: ModelObject): NavigationFieldPolicy | undefined {
    if (typeof parent.path !== 'string') return undefined;
    // These are exposed component-property paths, not arbitrary field names in application/query data.
    const property = parent.path.replace(/^properties\./, '');
    return navigationPropertyPolicies.authoredNavigation[property] ?? componentPolicies(parent.component)[property];
}

function pruneInstanceValue(value: unknown, field: FieldContext): unknown {
    const rule = instancePolicy(field.parent);
    if (!rule) return value;
    return fieldHandlers[rule.kind](value, { ...field, rule });
}

function pruneInstanceItems(value: unknown, field: FieldContext): unknown {
    if (!Array.isArray(value)) return value;
    return value.map(item => pruneContributedItem(item, field)).filter(item => item !== undefined);
}

function pruneContributedItem(value: unknown, field: FieldContext): unknown {
    if (!isObject(value) || !isObject(value.values)) return value;
    const rule = instancePolicy(field.parent);
    const fields = rule?.kind === 'list' ? policies[selectedShape(rule.shape ?? '', value.values)] ?? {} : componentPolicies(field.parent.component);
    const values = pruneFields(value.values, fields, field.context);
    return values === undefined ? undefined : { ...value, values };
}

const fieldHandlers: Record<string, (value: unknown, field: FieldContext) => unknown> = {
    keep: value => value,
    screen: (value, field) => allowedScreen(value, field.context),
    clickFallback: (value, field) => allowedScreen(value, field.context),
    node: (value, field) => pruneNode(value, field.rule.shape ?? '', field.context),
    list: (value, field) => pruneList(value, field.rule.shape ?? '', field.context),
    slots: (value, field) => pruneSlots(value, field.context),
    properties: pruneProperties,
    contribution: pruneContribution,
    route: allowedRoute,
    module: allowedModule,
    slice: allowedSlice,
    destinationSlice: (value, field) => allowedSlice([field.parent.module, field.parent.feature, value].filter(Boolean).join('.'), field) === undefined ? undefined : value,
    command: allowedCommand,
    legacyCommand: allowedLegacyCommand,
    instanceOwner: (value, field) => allowedInstanceOwner(value, field.context),
    instanceValue: pruneInstanceValue,
    instanceItems: pruneInstanceItems,
};

function isObject(value: unknown): value is ModelObject {
    if (value === null) return false;
    if (Array.isArray(value)) return false;
    return typeof value === 'object';
}
