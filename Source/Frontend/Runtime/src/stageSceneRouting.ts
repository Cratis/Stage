// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, useContext, useMemo, useSyncExternalStore } from 'react';
import { OutletItemKind, OutletKind, SlotRole, type ExternalComponent, type Layout, type Outlet, type OutletItem, type SceneElement, type Screen, type ScreenTemplate, type Slot } from '@cratis/scene.model';
import { sliceRouteSegment, type OutletLevel } from '@cratis/scene.engine';
import {
    emptySceneRouteState,
    parseRoutedSlotPath,
    routedSlotPath,
    useSceneQueryParameters,
    withQueryParameter,
    withSlotClosed,
    withSlotOpened,
    withTabSelected,
    type SceneHistory,
    type SceneNavigationState,
    type SceneRouter,
    type SceneRouteState,
    type SlotRoute,
} from '@cratis/scene.react';

/**
 * What the Stage needs to know of an application to put its routed slots, tabs and filters in the address.
 */
export interface StageRouteShape {
    /** The routed slots - never a panel - by name, with the parameters each is routed by, in order. */
    routedSlots: Record<string, string[]>;

    /** The outlets of tabs that own a routed slot, with the slot each owns: the chosen tab is a segment of that slot. */
    tabSlots: Record<string, string>;

    /** The query parameter each filter writes, with the query it sets; undefined when it names none. */
    filters: Record<string, string | undefined>;

    /** Whether the page slot is routed, so the filters' values are in the address. */
    pageRouted: boolean;

    /** The screen a slot route's segment opens, by segment (`set-product-price`). */
    segmentScreens: Record<string, string>;

    /** The screen a slot shows for a selection (`show ProductDetails by sku from products.selection`), by slot. */
    slotDefaults: Record<string, string>;
}

/** The parts of a Stage application the route shape is read from. */
export interface StageRouteSource {
    layouts?: Layout[];
    screenTemplates?: ScreenTemplate[];
    screens?: Screen[];
    outletLevels?: OutletLevel[];
}

/** A shape with nothing routed: every slot, tab and filter stays in memory. */
export const unroutedShape: StageRouteShape = { routedSlots: {}, tabSlots: {}, filters: {}, pageRouted: false, segmentScreens: {}, slotDefaults: {} };

/**
 * Reads what is routed in an application: the routed slots of its layouts and templates, the outlets of tabs that own one, the parameters
 * its filters write, whether its page slot is routed, and the screens a slot route can name.
 *
 * @param source The layouts, templates, screens and outlet levels of the application.
 * @returns The route shape.
 */
export function stageRouteShapeOf(source: StageRouteSource): StageRouteShape {
    const slots: Slot[] = [...(source.layouts ?? []).flatMap(layout => layout.slots ?? []), ...(source.screenTemplates ?? []).flatMap(template => template.slots ?? [])];
    const routedSlots: Record<string, string[]> = {};
    for (const slot of slots) {
        if (slot.role === SlotRole.Panel || !(slot.routed || (slot.routedBy?.length ?? 0) > 0)) continue;
        routedSlots[slot.name] = slot.routedBy ?? [];
    }

    const outlets: Outlet[] = [
        ...(source.layouts ?? []).flatMap(layout => layout.outlets ?? []),
        ...(source.screenTemplates ?? []).flatMap(template => template.outlets ?? []),
        ...(source.outletLevels ?? []).flatMap(level => level.outlets ?? []),
    ];
    const tabSlots: Record<string, string> = {};
    for (const outlet of outlets) {
        if (outlet.kind === OutletKind.Tabs && outlet.slot && routedSlots[outlet.slot]) tabSlots[outlet.name] = outlet.slot;
    }

    const items: OutletItem[] = [
        ...outlets.flatMap(outlet => outlet.defaults ?? []),
        ...(source.outletLevels ?? []).flatMap(level => [
            ...(level.refinements ?? []).flatMap(refinement => refinement.items ?? []),
            ...(level.contributions ?? []).flatMap(contribution => contribution.items ?? []),
        ]),
        ...(source.screens ?? []).flatMap(screen => (screen.outletContributions ?? []).flatMap(contribution => contribution.items ?? [])),
    ];
    const filters: Record<string, string | undefined> = {};
    for (const item of flattened(items)) {
        if (item.kind !== OutletItemKind.Filter) continue;
        filters[item.sets?.parameter ?? item.id] = item.sets?.query;
    }

    const segmentScreens: Record<string, string> = {};
    const slotDefaults: Record<string, string> = {};
    for (const screen of source.screens ?? []) {
        segmentScreens[sliceRouteSegment(screen.name)] = screen.name;
        for (const element of Object.values(screen.slotContent ?? {}).flat().flatMap(elementsOf)) {
            const properties = (element as ExternalComponent).properties ?? {};
            if ((element as ExternalComponent).componentName === 'core:slot' && typeof properties.shows === 'string' && typeof properties.name === 'string') {
                slotDefaults[properties.name] = properties.shows;
            }
        }
    }

    const pageRouted = slots.some(slot => slot.role === SlotRole.Page && (slot.routed === true || (slot.routedBy?.length ?? 0) > 0));
    return { routedSlots, tabSlots, filters, pageRouted, segmentScreens, slotDefaults };
}

function flattened(items: readonly OutletItem[]): OutletItem[] {
    return items.flatMap(item => item.kind === OutletItemKind.Group ? [item, ...flattened(item.items ?? [])] : [item]);
}

function elementsOf(element: SceneElement): SceneElement[] {
    const slots = (element as ExternalComponent).slots ?? {};
    return [element, ...Object.values(slots).flat().flatMap(elementsOf)];
}

/** An address the Stage reads: the Scene navigation's part, and what the routed slots, tabs and filters say. */
export interface StageAddress {
    /** The part the Scene navigation host matches against its routes (`ProductCatalog?id=1`), or undefined when there is none. */
    scene: string | undefined;

    /** What the address says the routed slots, tabs and filters show. */
    route: SceneRouteState;
}

const groupMark = '@';

/**
 * Reads an address relative to the application (`ProductCatalog/@detail/NK-101/@detail.detailTabs/pricing?q=kettle`): the Scene
 * navigation's part, then a group per routed slot (`@<slot>` with its route values and segment) and per chosen tab of a routed slot
 * (`@<slot>.<outlet>` with the tab), and the filters' values among the query parameters when the page slot is routed.
 *
 * @param address The address, without a leading slash or hash.
 * @param shape What is routed in the application.
 * @returns The Scene navigation's part, and the route state.
 */
export function parseStageAddress(address: string, shape: StageRouteShape): StageAddress {
    const separator = address.indexOf('?');
    const path = separator < 0 ? address : address.slice(0, separator);
    const search = separator < 0 ? '' : address.slice(separator + 1);
    const segments = path.split('/').filter(segment => segment.length > 0);
    const first = segments.findIndex(segment => segment.startsWith(groupMark));
    const scenePath = (first < 0 ? segments : segments.slice(0, first)).join('/');

    const slots: Record<string, SlotRoute> = {};
    const tabs: Record<string, string> = {};
    for (let index = first; index >= 0 && index < segments.length;) {
        let end = index + 1;
        while (end < segments.length && !segments[end].startsWith(groupMark)) end++;
        const name = decodeURIComponent(segments[index].slice(groupMark.length));
        const rest = segments.slice(index + 1, end).join('/');
        const dot = name.indexOf('.');
        if (dot > 0) {
            if (rest) tabs[name.slice(dot + 1)] = decodeURIComponent(rest);
        } else if (shape.routedSlots[name]) {
            const route = parseRoutedSlotPath(rest, shape.routedSlots[name]);
            const screen = (route.segment ? shape.segmentScreens[route.segment] : undefined) ?? shape.slotDefaults[name];
            slots[name] = screen ? { ...route, screen } : route;
        }
        index = end;
    }

    const sceneQuery = new URLSearchParams();
    const query: Record<string, string> = {};
    for (const [name, value] of new URLSearchParams(search)) {
        if (shape.pageRouted && name in shape.filters) {
            if (value !== '') query[name] = value;
        } else {
            sceneQuery.append(name, value);
        }
    }

    const sceneSearch = sceneQuery.toString();
    return {
        scene: scenePath ? (sceneSearch ? `${scenePath}?${sceneSearch}` : scenePath) : undefined,
        route: { slots, tabs, query },
    };
}

/**
 * Writes an address relative to the application from the Scene navigation's part and the route state - the reverse of
 * {@link parseStageAddress}. Only what is routed is written: a slot that is not routed, a tab whose slot is not routed and, unless the
 * page slot is routed, the filters' values stay out of the address.
 *
 * @param scene The Scene navigation's part.
 * @param route The route state.
 * @param shape What is routed in the application.
 * @returns The address, without a leading slash or hash.
 */
export function stageAddress(scene: string | undefined, route: SceneRouteState, shape: StageRouteShape): string {
    const value = scene ?? '';
    const separator = value.indexOf('?');
    const scenePath = separator < 0 ? value : value.slice(0, separator);
    const sceneSearch = separator < 0 ? '' : value.slice(separator + 1);

    const segments = scenePath.split('/').filter(segment => segment.length > 0).map(escapeMark);
    for (const [slot, routedBy] of Object.entries(shape.routedSlots)) {
        const shown = route.slots[slot];
        if (shown) segments.push(`${groupMark}${encodeURIComponent(slot)}`, ...routedSlotPath(shown, routedBy).split('/').filter(part => part.length > 0).map(escapeMark));
        for (const [outlet, owner] of Object.entries(shape.tabSlots)) {
            const tab = route.tabs[outlet];
            if (owner === slot && tab) segments.push(`${groupMark}${encodeURIComponent(slot)}.${encodeURIComponent(outlet)}`, escapeMark(encodeURIComponent(tab)));
        }
    }

    const query = new URLSearchParams(sceneSearch);
    if (shape.pageRouted) {
        for (const [name, parameter] of Object.entries(route.query)) {
            if (parameter !== '') query.set(name, parameter);
        }
    }

    const search = query.toString();
    return search ? `${segments.join('/')}?${search}` : segments.join('/');
}

// A route value or segment that starts with the group mark would read as a group, so its mark is written encoded.
function escapeMark(segment: string): string {
    return segment.startsWith(groupMark) ? `%40${segment.slice(groupMark.length)}` : segment;
}

/** How the Stage addresses screens: by the fragment (`#/Screen`) or by the path (`/Screen`). */
export type StageRoutingMode = 'hash' | 'path';

/** The browser the routing reads and writes the address of. */
export type StageRoutingBrowser = Pick<Window, 'location' | 'history' | 'addEventListener' | 'removeEventListener'>;

/**
 * The Stage's router: one address for the Scene navigation host (its {@link SceneHistory}) and for the routed slots, tabs and filters
 * (a {@link SceneRouter} through {@link useStageSceneRouter}).
 */
export interface StageSceneRouting {
    /** The history the Scene navigation host reads and writes its part of the address through. */
    history: SceneHistory;

    /** The route state. */
    getState(): SceneRouteState;

    /** Listens for changes of the route state. */
    subscribe(listener: () => void): () => void;

    /** Opens a screen in a slot: a history entry when the slot is routed. */
    openInSlot(slot: string, route: SlotRoute): void;

    /** Empties a slot: a history entry when the slot is routed. */
    closeSlot(slot: string): void;

    /** Chooses a tab: a history entry when the slot the tabs own is routed. */
    selectTab(outlet: string, tab: string): void;

    /** Writes a filter's value: in the address, without a history entry, when the page slot is routed. */
    setQueryParameter(name: string, value: string | undefined): void;

    /** Stops listening to the browser. */
    dispose(): void;
}

const stateKey = 'sceneNavigation';

/**
 * Creates the Stage's router over the browser's address.
 *
 * Only routed slot changes and chosen tabs are history entries, so back and forward move through them. A filter replaces the current
 * entry. A dialog or drawer opened over the page does not change the address and is not a history entry either: the Scene navigation
 * host's push of the same address replaces the entry, and closing it puts the page it opened over back.
 *
 * @param browser The browser.
 * @param mode Whether screens are addressed by the fragment or by the path.
 * @param shape What is routed in the application.
 * @returns The routing.
 */
export function createStageSceneRouting(browser: StageRoutingBrowser, mode: StageRoutingMode, shape: StageRouteShape): StageSceneRouting {
    const relative = (): string => mode === 'hash'
        ? browser.location.hash.replace(/^#\/?/, '')
        : `${browser.location.pathname}${browser.location.search}`.replace(/^\//, '');
    const href = (address: string): string => mode === 'hash' ? `${browser.location.pathname}${browser.location.search}#/${address}` : `/${address}`;
    const stored = (): SceneNavigationState | undefined => (browser.history.state as Record<string, SceneNavigationState> | null)?.[stateKey];
    const sceneOf = (url: string | undefined): string | undefined => url === undefined ? undefined : parseStageAddress(url, shape).scene;

    let route: SceneRouteState = parseStageAddress(relative(), shape).route;
    let overlaidOn: SceneNavigationState | undefined;
    const listeners = new Set<() => void>();
    const sceneListeners = new Set<(url: string | undefined, state: SceneNavigationState | undefined) => void>();
    const emit = () => listeners.forEach(listener => listener());
    const write = (kind: 'push' | 'replace', scene: string | undefined, state?: SceneNavigationState) => {
        const data = state ? { [stateKey]: state } : browser.history.state;
        const address = href(stageAddress(scene, route, shape));
        if (kind === 'push') browser.history.pushState(data, '', address);
        else browser.history.replaceState(data, '', address);
    };
    const change = (next: SceneRouteState, kind: 'push' | 'replace' | undefined) => {
        if (next === route) return;
        route = next;
        emit();
        if (kind) write(kind, sceneOf(relative()));
    };

    const onAddressChanged = () => {
        overlaidOn = undefined;
        const address = parseStageAddress(relative(), shape);
        const next = withRoutedParts(route, address.route, shape);
        if (!sameRoute(next, route)) {
            route = next;
            emit();
        }
        sceneListeners.forEach(listener => listener(address.scene, stored()));
    };
    // The browser is listened to while anything listens to the routing, so a routing nothing uses holds no listener.
    let attached = false;
    const attach = () => {
        if (attached) return;
        attached = true;
        browser.addEventListener('popstate', onAddressChanged);
        if (mode === 'hash') browser.addEventListener('hashchange', onAddressChanged);
    };
    const detach = () => {
        if (!attached || listeners.size > 0 || sceneListeners.size > 0) return;
        attached = false;
        browser.removeEventListener('popstate', onAddressChanged);
        if (mode === 'hash') browser.removeEventListener('hashchange', onAddressChanged);
    };

    return {
        history: {
            location: () => sceneOf(relative()),
            state: stored,
            push: state => {
                if (sceneOf(state.url) === sceneOf(relative())) {
                    // The same address with a dialog or drawer over it, or without one: not a place to go back to.
                    if (state.dialog && !stored()?.dialog) overlaidOn = stored();
                    write('replace', state.url, state);
                    return;
                }
                overlaidOn = undefined;
                // Another screen does not keep this one's slots, tabs and filters.
                route = emptySceneRouteState;
                emit();
                write('push', state.url, state);
            },
            replace: state => write('replace', state.url, state),
            back: () => {
                const page = overlaidOn;
                if (!page) {
                    browser.history.back();
                    return;
                }
                overlaidOn = undefined;
                write('replace', page.url, page);
                sceneListeners.forEach(listener => listener(sceneOf(page.url), page));
            },
            listen: listener => {
                sceneListeners.add(listener);
                attach();
                return () => {
                    sceneListeners.delete(listener);
                    detach();
                };
            },
        },
        getState: () => route,
        subscribe: listener => {
            listeners.add(listener);
            attach();
            return () => {
                listeners.delete(listener);
                detach();
            };
        },
        openInSlot: (slot, slotRoute) => change(withSlotOpened(route, slot, slotRoute), shape.routedSlots[slot] ? 'push' : undefined),
        closeSlot: slot => change(route.slots[slot] ? withSlotClosed(route, slot) : route, shape.routedSlots[slot] ? 'push' : undefined),
        selectTab: (outlet, tab) => change(route.tabs[outlet] === tab ? route : withTabSelected(route, outlet, tab), shape.tabSlots[outlet] ? 'push' : undefined),
        setQueryParameter: (name, value) => change((route.query[name] ?? '') === (value ?? '') ? route : withQueryParameter(route, name, value), shape.pageRouted ? 'replace' : undefined),
        dispose: () => {
            listeners.clear();
            sceneListeners.clear();
            detach();
        },
    };
}

// What the address says replaces what is routed; what is not routed is kept as it was in memory.
function withRoutedParts(current: SceneRouteState, address: SceneRouteState, shape: StageRouteShape): SceneRouteState {
    const slots = Object.fromEntries(Object.entries(current.slots).filter(([slot]) => !shape.routedSlots[slot]));
    const tabs = Object.fromEntries(Object.entries(current.tabs).filter(([outlet]) => !shape.tabSlots[outlet]));
    return {
        slots: { ...slots, ...address.slots },
        tabs: { ...tabs, ...address.tabs },
        query: shape.pageRouted ? address.query : current.query,
    };
}

function sameRoute(left: SceneRouteState, right: SceneRouteState): boolean {
    return JSON.stringify(left) === JSON.stringify(right);
}

/**
 * The {@link SceneRouter} of the Stage's routing, for `SceneRouterProvider`: a new one each time the route state changes, so what reads it
 * draws again.
 *
 * @param routing The Stage's routing.
 * @returns The router.
 */
export function useStageSceneRouter(routing: StageSceneRouting): SceneRouter {
    const state = useSyncExternalStore(routing.subscribe, routing.getState, routing.getState);
    return useMemo(() => ({
        state,
        openInSlot: routing.openInSlot,
        closeSlot: routing.closeSlot,
        selectTab: routing.selectTab,
        setQueryParameter: routing.setQueryParameter,
    }), [routing, state]);
}

/** The route shape of the running application, for what reads the filters' values. */
export const StageRouteShapeContext = createContext<StageRouteShape>(unroutedShape);

/**
 * The values the filters wrote for a query: those of the filters that set one of its parameters, and of those that name no query.
 *
 * @param shape What is routed in the application, and which query each filter sets.
 * @param query The values the filters wrote, by query parameter.
 * @param queryName The query.
 * @returns The query's arguments from the filters.
 */
export function filterArgumentsFor(shape: StageRouteShape, query: Record<string, string>, queryName: string | undefined): Record<string, string> {
    return Object.fromEntries(Object.entries(query).filter(([name, value]) => value !== '' && name in shape.filters && (shape.filters[name] === undefined || shape.filters[name] === queryName)));
}

/**
 * The values the filters on the page wrote for a query.
 *
 * @param queryName The query.
 * @returns The query's arguments from the filters.
 */
export function useStageFilterArguments(queryName: string | undefined): Record<string, string> {
    const shape = useContext(StageRouteShapeContext);
    const query = useSceneQueryParameters();
    const key = JSON.stringify(filterArgumentsFor(shape, query, queryName));
    return useMemo(() => JSON.parse(key) as Record<string, string>, [key]);
}
