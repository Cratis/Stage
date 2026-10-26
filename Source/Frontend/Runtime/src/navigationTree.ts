// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DestinationReference, Screen } from '@cratis/scene.model';

/** The contribution point the application's navigation is contributed to. */
export const navigationContributionPoint = 'Navigation';

/** One authored navigation entry: what it is called, the screen it opens and where in the application it was declared. */
export interface NavigationEntry {
    label: string;
    targetScreen: string;
    /** The module and features, outermost first, the entry was declared in. */
    hierarchy: string[];
    order?: number;
    /** The authored identity of the entry: one entry per id, however many screens it arrives on. */
    id?: string;
    /** The section heading the entry is listed under within its feature. */
    group?: string;
    /** The icon the entry was authored with. */
    icon?: string;
    /** How chrome shows the entry - `icon`, `text` or `both`; any other value is a hint the theme may read. */
    presentation?: string;
    /** Where the entry opens when that is not simply the screen: an outlet, a dialog or an external route. */
    destination?: DestinationReference;
}

/** A node of the navigation tree: a group (module or feature) with children, or an entry that opens a screen. */
export interface NavigationNode {
    label: string;
    /** The authored identity of the entry; absent on a group. */
    id?: string;
    /** Whether the node is a heading made from an entry's `group`, rather than a module or feature. */
    section?: true;
    /** The screen the node opens; absent on a group. */
    screen?: string;
    icon?: string;
    presentation?: string;
    destination?: DestinationReference;
    children: NavigationNode[];
}

/**
 * The navigation the model authored: every `Navigation` contribution on any screen, once.
 *
 * A feature's contribution is attached to every screen of the feature, so the same entry arrives several times;
 * an entry is the same when it opens the same screen from the same place under the same label.
 *
 * @param screens The scene's screens.
 * @returns The authored entries, in the order the model gave them (ordered ones first, by `order`).
 */
export function authoredNavigationEntries(screens: Screen[]): NavigationEntry[] {
    const seen = new Set<string>();
    const entries: NavigationEntry[] = [];
    for (const screen of screens) {
        for (const contribution of screen.contributions ?? []) {
            if (contribution.contributionPointName !== navigationContributionPoint) continue;
            const properties = (contribution.content?.properties ?? {}) as Record<string, unknown>;
            const label = properties.label;
            const targetScreen = properties.targetScreen;
            if (typeof label !== 'string' || typeof targetScreen !== 'string') continue;

            const hierarchy = Array.isArray(properties.hierarchy) ? properties.hierarchy.filter((part): part is string => typeof part === 'string') : [];
            const id = typeof properties.id === 'string' ? properties.id : undefined;
            const key = id !== undefined ? JSON.stringify(['id', id]) : JSON.stringify([hierarchy, targetScreen, label]);
            if (seen.has(key)) continue;
            seen.add(key);
            const order = contribution.order ?? (typeof properties.order === 'number' ? properties.order : undefined);
            const destination = isDestination(properties.destination) ? properties.destination : undefined;
            entries.push({
                label,
                targetScreen,
                hierarchy,
                order,
                ...(id !== undefined ? { id } : {}),
                ...(typeof properties.group === 'string' ? { group: properties.group } : {}),
                ...(typeof properties.icon === 'string' ? { icon: properties.icon } : {}),
                ...(typeof properties.presentation === 'string' ? { presentation: properties.presentation } : {}),
                ...(destination && opensElsewhere(destination) ? { destination } : {}),
            });
        }
    }

    return entries
        .map((entry, index) => ({ entry, index }))
        .sort((left, right) => (left.entry.order ?? Number.MAX_SAFE_INTEGER) - (right.entry.order ?? Number.MAX_SAFE_INTEGER) || left.index - right.index)
        .map(({ entry }) => entry);
}

/**
 * Lays the entries out as the application hierarchy the model declared: a group per module and per feature,
 * nested as deep as the model nests them, with each entry under the feature it was declared in. Groups and
 * entries keep the order of the entries, so an ordered entry orders the group it sits in.
 *
 * @param entries The authored entries.
 * @returns The top-level nodes.
 */
export function navigationTree(entries: NavigationEntry[]): NavigationNode[] {
    const root: NavigationNode = { label: '', children: [] };
    for (const entry of entries) {
        let parent = root;
        for (const part of entry.hierarchy) {
            let group = parent.children.find(candidate => candidate.screen === undefined && !candidate.section && candidate.label === part);
            if (!group) {
                group = { label: part, children: [] };
                parent.children.push(group);
            }

            parent = group;
        }

        if (entry.group) {
            let section = parent.children.find(candidate => candidate.section && candidate.label === entry.group);
            if (!section) {
                section = { label: entry.group, section: true, children: [] };
                parent.children.push(section);
            }

            parent = section;
        }

        parent.children.push({
            label: entry.label,
            ...(entry.id ? { id: entry.id } : {}),
            screen: entry.targetScreen,
            ...(entry.icon ? { icon: entry.icon } : {}),
            ...(entry.presentation ? { presentation: entry.presentation } : {}),
            ...(entry.destination ? { destination: entry.destination } : {}),
            children: [],
        });
    }

    return root.children;
}

/**
 * The labels leading to a screen in the tree - each group, then the entry - for the breadcrumb.
 *
 * @param nodes The navigation tree.
 * @param screen The screen to find.
 * @returns The labels from the outermost group to the entry, or an empty list when no entry opens the screen.
 */
export function navigationPathTo(nodes: NavigationNode[], screen: string): string[] {
    for (const node of nodes) {
        if (node.screen === screen) return [node.label];
        const below = navigationPathTo(node.children, screen);
        if (below.length > 0) return node.section ? below : [node.label, ...below];
    }

    return [];
}

function isDestination(value: unknown): value is DestinationReference {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
}

/** Whether an entry opens somewhere a plain link to its screen does not reach: an outlet, a dialog or an external route. */
export function opensElsewhere(destination: DestinationReference): boolean {
    return destination.outlet !== undefined || destination.dialog !== undefined || destination.kind === 'external' || destination.kind === 'dialog';
}

/**
 * The hierarchy the screens declare, for the screens no navigation entry lists.
 *
 * A screen records the modules and features it was declared in, so one the model gave no entry still has a place:
 * under its own module and feature, beside the entries that do list screens there. A screen declared outside any
 * module has no place to be under, and goes with the other screens that have none.
 *
 * @param screens The scene's screens.
 * @param listed The names of the screens an authored entry opens.
 * @param fallback The group a screen with no recorded place goes under.
 * @returns One entry for every unlisted screen, labelled with its own name.
 */
export function unlistedNavigationEntries(screens: Screen[], listed: ReadonlySet<string>, fallback: string): NavigationEntry[] {
    return screens
        .filter(screen => !listed.has(screen.name))
        .map(screen => ({
            label: screen.name,
            targetScreen: screen.name,
            hierarchy: screen.path && screen.path.length > 0 ? [...screen.path] : [fallback],
        }));
}
