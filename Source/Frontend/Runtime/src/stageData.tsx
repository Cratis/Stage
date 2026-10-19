// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import type React from 'react';
import type { BindingExpression } from '@cratis/scene.model';
import { Globals } from '@cratis/arc';
import { ServerSentEventQueryConnection } from '@cratis/arc/queries';
import { createBindingResolver, resolveArgumentSource, validateBindingExpression } from '@cratis/scene.engine';
import type { StageRoutes } from './stageRoutes';
import { guardPath } from './stageGuards';
import { sessionIdentifierPrefix } from './stageSession';

const DATA_CHANGED = 'cratis.stage.data-changed';

interface QueryState {
    name?: string;

    /** The read model's name, under which the rows are published to the binding scope as well - where a summary looks them up. */
    alias?: string;

    /** The rows are always a list in the binding scope, even when there is one: a path reads `0.count`, never `count`. */
    list?: boolean;
    route: string;
    rows: Record<string, unknown>[];
    loading: boolean;
    error: string;
    arguments: Record<string, unknown>;
}

export interface StageQueryRequest {
    scope: string;
    name?: string;
    route?: string;
    arguments?: Record<string, unknown>;
    ready?: boolean;

    /** Publishes the rows under this name too: the read model a `data` declaration shows, so a summary finds them by it. */
    alias?: string;

    /** Whether the rows are published to bindings as a list always, so a path such as `0.lineCount` reads the same for one row as for many. */
    list?: boolean;

    /**
     * Whether the result is published to the screen's bindings. A query read only to decide something - the item a
     * guard is evaluated against - is not, so it does not show up twice where the screen reads the same query.
     */
    publish?: boolean;
}

export interface StageQueryResult {
    rows: Record<string, unknown>[];
    loading: boolean;
    error: string;
    refresh: () => void;
}


export interface StageDataState {
    locale: string;
    locales: string[];
    screen: string;
    routes: StageRoutes | undefined;
    routesReady: boolean;
    parameters: Record<string, string>;
    queries: Record<string, QueryState>;
    selected: Record<string, unknown> | undefined;
    selections: Record<string, Record<string, unknown> | undefined>;
    selectRow: (scope: string, row: Record<string, unknown> | undefined) => void;
    clearSelection: (scope: string) => void;
    setState: (key: string, value: unknown) => void;
    setComponentOutput: (componentId: string, path: string, value: unknown) => void;

    /** Takes what the components of the rendered tree publish (a list's selected row, a search box's text) so bindings and interactions elsewhere on the page read it. */
    publishComponentOutputs: (outputs: Record<string, Record<string, unknown>>) => void;

    /** The rows of every query the page has read, by scope and by query name - what a `queryResult` binding resolves against. */
    queryResults: Record<string, unknown>;
    refreshQuery: (query?: string) => void;
    refreshAfterCommand: () => void;
    resolveBinding: (binding: BindingExpression | string | undefined) => unknown;
    /**
     * Resolves a command argument source the way Scene does: `component.<id>.<output>` reads what another
     * element publishes - such as the `value` an input holds or a table's `selectedItem` - and any other
     * source is a data-context path.
     */
    resolveArgument: (source: string | undefined) => unknown;
    registerQueryResult: (scope: string, state: QueryState | undefined) => void;
    refreshVersion: number;
    refreshQueryName?: string;
}

const emptyState: StageDataState = {
    locale: '',
    locales: [],
    screen: '',
    routes: undefined,
    routesReady: true,
    parameters: {},
    queries: {},
    selected: undefined,
    selections: {},
    selectRow: () => undefined,
    clearSelection: () => undefined,
    setState: () => undefined,
    setComponentOutput: () => undefined,
    publishComponentOutputs: () => undefined,
    queryResults: {},
    refreshQuery: () => undefined,
    refreshAfterCommand: () => undefined,
    resolveBinding: () => undefined,
    resolveArgument: () => undefined,
    registerQueryResult: () => undefined,
    refreshVersion: 0,
};

const StageDataContext = createContext<StageDataState>(emptyState);

/** When, after a command's own refresh, its queries are read again so an asynchronous projection has caught up. */
export const commandSettleDelays = [500, 1500, 3000];

export interface StageDataProviderProps {
    routes: StageRoutes | undefined;
    routesReady?: boolean;
    parameters?: Record<string, string>;
    locale: string;
    locales: string[];
    screen: string;
    children: React.ReactNode;
}

const noParameters: Record<string, string> = {};

export function StageDataProvider({ routes, routesReady = true, parameters: addressParameters = noParameters, locale, locales, screen, children }: StageDataProviderProps) {
    const [queries, setQueries] = useState<Record<string, QueryState>>({});
    const [sessionVersion, setSessionVersion] = useState(0);
    const [selections, setSelections] = useState<Record<string, Record<string, unknown> | undefined>>({});
    const [activeSelection, setActiveSelection] = useState<string>();
    const [localState, setLocalState] = useState<Record<string, unknown>>({});
    const [componentOutputState, setComponentOutputState] = useState<Record<string, Record<string, unknown>>>({});
    const [publishedOutputs, setPublishedOutputs] = useState<Record<string, Record<string, unknown>>>({});
    const [refreshRequests, setRefreshRequests] = useState<{ query?: string; version: number }>({ version: 0 });

    const registerQueryResult = useCallback((scope: string, state: QueryState | undefined) => {
        setQueries(current => {
            if (!state) {
                const { [scope]: _, ...remaining } = current;
                return remaining;
            }

            return { ...current, [scope]: state };
        });
    }, []);

    // What the address does not carry, the session does: the shopper's own basket is the one a command created, so a screen about "the basket" needs no id in its address.
    const parameters = useMemo(() => ({ ...sessionParameters(), ...addressParameters }), [addressParameters, sessionVersion]);

    const selectRow = useCallback((scope: string, row: Record<string, unknown> | undefined) => {
        setSelections(current => ({ ...current, [scope]: row }));
        if (row) {
            setActiveSelection(scope);
        } else {
            setActiveSelection(current => current === scope ? undefined : current);
        }
    }, []);

    const clearSelection = useCallback((scope: string) => {
        setSelections(current => ({ ...current, [scope]: undefined }));
        setActiveSelection(current => current === scope ? undefined : current);
    }, []);

    const setComponentOutput = useCallback((componentId: string, path: string, value: unknown) => {
        setComponentOutputState(current => {
            const componentState = isRecord(current[componentId]) ? current[componentId] : {};
            return { ...current, [componentId]: writePath(componentState, path.split('.').filter(Boolean), value) };
        });
    }, []);

    const refreshQuery = useCallback((query?: string) =>
        setRefreshRequests(current => ({ query, version: current.version + 1 })), []);

    // A command answers once its events are appended; a projection applies them afterwards. Chronicle - what a
    // generated application runs on - does that asynchronously, so a single refresh right after the answer can
    // read the read model one event behind. Reading again once the projection has had time to run is what makes
    // the screen show what the command did; a stale response is still discarded by the query's own sequencing.
    const settleTimers = useRef<ReturnType<typeof setTimeout>[]>([]);
    const refreshAfterCommand = useCallback(() => {
        setSessionVersion(current => current + 1);
        refreshQuery();
        for (const delay of commandSettleDelays) settleTimers.current.push(setTimeout(() => refreshQuery(), delay));
    }, [refreshQuery]);
    useEffect(() => () => settleTimers.current.forEach(clearTimeout), []);

    useEffect(() => {
        const dataChanged = () => refreshQuery();
        const refresh = (event: Event) => refreshQuery((event as CustomEvent<{ query?: string }>).detail?.query);
        globalThis.addEventListener(DATA_CHANGED, dataChanged);
        globalThis.addEventListener('cratis.scene.refresh', refresh);
        return () => {
            globalThis.removeEventListener(DATA_CHANGED, dataChanged);
            globalThis.removeEventListener('cratis.scene.refresh', refresh);
        };
    }, [refreshQuery]);

    const selected = activeSelection ? selections[activeSelection] : undefined;

    const resolveBinding = useCallback((binding: BindingExpression | string | undefined): unknown => {
        if (binding === undefined) return undefined;

        // A guard is evaluated against the item its interaction is about - a table supplies the row it happened
        // on. Resolved without one, it is nothing: the interaction engine reports it and runs neither branch.
        if (typeof binding !== 'string' && binding.path === guardPath) return undefined;
        if (typeof binding === 'string') return resolvePath(binding, selected, selections, queries, localState, parameters, screen, locale, locales);

        const scope = bindingScope(selected, queries, localState, { ...publishedOutputs, ...componentOutputState }, selections);

        // `component.<id>.<output>` reads what another element of the page publishes - the row a list has selected, the text a search box holds.
        if (!binding.kind && binding.path.startsWith('component.')) return resolveArgumentSource(binding.path, scope);
        if (!binding.kind) return resolvePath(binding.path, selected, selections, queries, localState, parameters, screen, locale, locales);

        const diagnostics = validateBindingExpression(binding, scope);
        if (diagnostics.length > 0) {
            console.warn(`Scene binding could not be resolved: ${diagnostics.map(_ => `${_.code}:${_.message}`).join('; ')}`);
        }

        return createBindingResolver(scope)(binding);
    }, [componentOutputState, locale, locales, localState, parameters, publishedOutputs, queries, screen, selected, selections]);

    const resolveArgument = useCallback(
        (source: string | undefined): unknown => source ? resolveArgumentSource(source, bindingScope(selected, queries, localState, { ...publishedOutputs, ...componentOutputState }, selections)) : undefined,
        [componentOutputState, localState, publishedOutputs, queries, selected, selections]);

    const publishComponentOutputs = useCallback((outputs: Record<string, Record<string, unknown>>) => setPublishedOutputs(outputs), []);
    const queryResults = useMemo(() => bindingScope(undefined, queries, {}, {}, {}).queryResults, [queries]);

    const state = useMemo<StageDataState>(() => ({
        locale,
        locales,
        screen,
        routes,
        routesReady,
        parameters,
        queries,
        selected,
        selections,
        selectRow,
        clearSelection,
        setState: (key, value) => setLocalState(current => writePath(current, key.split('.').filter(Boolean), value)),
        setComponentOutput,
        publishComponentOutputs,
        queryResults,
        refreshQuery,
        refreshAfterCommand,
        resolveBinding,
        resolveArgument,
        registerQueryResult,
        refreshVersion: refreshRequests.version,
        refreshQueryName: refreshRequests.query,
    }), [clearSelection, locale, locales, queries, refreshAfterCommand, refreshQuery, refreshRequests.query, refreshRequests.version, registerQueryResult, publishComponentOutputs, queryResults, resolveArgument, resolveBinding, routes, routesReady, parameters, screen, selectRow, selected, selections, setComponentOutput]);

    return <StageDataContext.Provider value={state}>{children}</StageDataContext.Provider>;
}

export const useStageData = (): StageDataState => useContext(StageDataContext);

export function useStageQuery(request: StageQueryRequest): StageQueryResult {
    const { refreshQueryName, refreshVersion, registerQueryResult } = useStageData();
    const publish = request.publish !== false;
    const publishResult = useCallback((scope: string, state: QueryState | undefined) => {
        if (publish) registerQueryResult(scope, state);
    }, [publish, registerQueryResult]);
    const [rows, setRows] = useState<Record<string, unknown>[]>([]);
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);
    const [localRefresh, setLocalRefresh] = useState(0);
    const latest = useRef(0);
    const liveOpen = useRef(false);
    const ready = request.ready ?? true;
    const active = !!request.route && ready;
    const argumentsKey = JSON.stringify(request.arguments ?? {});

    useEffect(() => {
        if (!active || !request.route) {
            latest.current += 1;
            setRows([]);
            setError('');
            setLoading(false);
            publishResult(request.scope, undefined);
            return;
        }

        const sequence = latest.current + 1;
        latest.current = sequence;
        const abort = new AbortController();
        const queryArguments = JSON.parse(argumentsKey) as Record<string, unknown>;
        const url = routeWithArguments(request.route, queryArguments);

        setLoading(true);
        setError('');

        const read = () => {
            fetch(url, { headers: { Accept: 'application/json' }, signal: abort.signal })
                .then(async response => {
                    if (!response.ok) throw new Error(`The query answered ${response.status}.`);
                    return response.json() as Promise<unknown>;
                })
                .then(payload => {
                    if (latest.current !== sequence || abort.signal.aborted) return;
                    setRows(normalizeRows(payload));
                    setError('');
                })
                .catch(reason => {
                    if (latest.current !== sequence || abort.signal.aborted) return;
                    setRows([]);
                    setError(reason instanceof Error ? reason.message : String(reason));
                })
                .finally(() => {
                    if (latest.current === sequence && !abort.signal.aborted) setLoading(false);
                });
        };

        // A live query is subscribed to rather than read: the server pushes a new result whenever its read model
        // changes, so a list shows what a command - issued from this client or any other - did without a refresh.
        if (isLiveRoute(request.route)) {
            liveOpen.current = true;
            const connection = new ServerSentEventQueryConnection<unknown>(new URL(withoutLiveMarker(url), globalThis.location.href));

            // A subscription the server refuses or drops never reaches the result callback: the connection only logs
            // it. Reading the route once asks the server why, and shows the answer the way a snapshot query does.
            let fellBack = false;
            const fallBack = () => {
                if (fellBack || latest.current !== sequence || abort.signal.aborted) return;
                fellBack = true;
                liveOpen.current = false;
                connection.disconnect();
                read();
            };

            const previous = Globals.eventSourceFactory;
            if (!previous && typeof EventSource === 'undefined') {
                fallBack();
            } else {
                let source: EventSource | undefined;
                Globals.eventSourceFactory = address => (source = previous ? previous(address) : new EventSource(address));
                try {
                    connection.connect(result => {
                        if (latest.current !== sequence || abort.signal.aborted) return;
                        const failure = queryFailure(result);
                        if (failure) {
                            setRows([]);
                            setError(failure);
                        } else {
                            setRows(normalizeRows(result));
                            setError('');
                        }

                        setLoading(false);
                    });
                } finally {
                    Globals.eventSourceFactory = previous;
                }

                const opened = source as EventSource | undefined;
                if (opened) {
                    const logged = opened.onerror;
                    opened.onerror = function (this: EventSource, event: Event) {
                        logged?.call(this, event);
                        if (opened.readyState === eventSourceClosed) fallBack();
                    };
                } else {
                    fallBack();
                }
            }

            return () => {
                abort.abort();
                liveOpen.current = false;
                connection.disconnect();
            };
        }

        read();

        return () => abort.abort();
    }, [active, argumentsKey, localRefresh, publishResult, request.name, request.route, request.scope]);

    useEffect(() => {
        if (refreshVersion === 0) return;
        if (refreshQueryName && refreshQueryName !== request.name && refreshQueryName !== request.scope) return;

        // An open stream already carries every change, so a refresh must not tear it down and reopen it.
        if (liveOpen.current) return;
        setLocalRefresh(current => current + 1);
    }, [refreshQueryName, refreshVersion, request.name, request.scope]);

    useEffect(() => {
        if (!active || !request.route) {
            publishResult(request.scope, undefined);
            return;
        }

        publishResult(request.scope, {
            name: request.name,
            list: request.list,
            alias: request.alias,
            route: request.route,
            rows,
            loading,
            error,
            arguments: JSON.parse(argumentsKey) as Record<string, unknown>,
        });
    }, [active, argumentsKey, error, loading, publishResult, request.alias, request.list, request.name, request.route, request.scope, rows]);

    return { rows, loading, error, refresh: () => { if (!liveOpen.current) setLocalRefresh(current => current + 1); } };
}

export function dataChanged() {
    globalThis.dispatchEvent(new CustomEvent(DATA_CHANGED));
}

function bindingScope(
    selected: Record<string, unknown> | undefined,
    queries: Record<string, QueryState>,
    localState: Record<string, unknown>,
    componentOutputState: Record<string, Record<string, unknown>>,
    selections: Record<string, Record<string, unknown> | undefined>,
) {
    // A query bound as a list is registered last, so a path that indexes into it wins the name over a table that reads the same query.
    const ordered = Object.entries(queries).sort(([, left], [, right]) => Number(left.list === true) - Number(right.list === true));
    const queryResults = Object.fromEntries(ordered.flatMap(([scope, state]) => {
        const value = state.list ? state.rows : queryValue(state.rows);
        return [[scope, value], ...(state.name ? [[state.name, value] as const] : []), ...(state.alias ? [[state.alias, value] as const] : [])];
    }));

    return {
        dataContext: selected,
        queryResults,
        componentOutputs: componentOutputs(componentOutputState, selections),
        state: localState,
    };
}

function queryValue(rows: Record<string, unknown>[]): unknown {
    return rows.length === 1 ? rows[0] : rows;
}

function componentOutputs(
    componentOutputState: Record<string, Record<string, unknown>>,
    selections: Record<string, Record<string, unknown> | undefined>,
): Record<string, Record<string, unknown>> {
    const outputs: Record<string, Record<string, unknown>> = { ...componentOutputState };
    for (const [componentId, selection] of Object.entries(selections)) {
        if (!selection) continue;
        outputs[componentId] = { ...selection, ...(outputs[componentId] ?? {}) };
    }

    return outputs;
}

function writePath(current: Record<string, unknown>, path: string[], value: unknown): Record<string, unknown> {
    const [head, ...tail] = path;
    if (!head) return current;
    if (tail.length === 0) return { ...current, [head]: value };
    return { ...current, [head]: writePath(isRecord(current[head]) ? current[head] : {}, tail, value) };
}

function resolvePath(
    path: string | undefined,
    selected: Record<string, unknown> | undefined,
    selections: Record<string, Record<string, unknown> | undefined>,
    queries: Record<string, QueryState>,
    localState: Record<string, unknown>,
    parameters: Record<string, string>,
    screen: string,
    locale: string,
    locales: string[],
): unknown {
    if (!path) return undefined;

    const parts = path.replace(/^\$\.?/, '').split('.').filter(Boolean);
    if (parts.length === 0) return undefined;

    if (parts[0] === 'screen') return parts.length === 1 ? { name: screen } : valueAt({ name: screen }, parts.slice(1));
    if (parts[0] === 'locale') return locale;
    if (parts[0] === 'locales') return locales;
    if (parts[0] === 'query') return resolveQueryResult(queries, parts[1], parts.slice(2).join('.'));
    if (parts[0] === 'data' || parts[0] === 'selected' || parts[0] === 'current') return resolveDataPath(parts.slice(1).join('.'), selected, selections);
    if (parts[0] === 'state') return valueAt(localState, parts.slice(1));
    if (parts[0] === 'parameters') return parts.length === 1 ? parameters : parameters[parts[1]];

    return valueAt(selected, parts) ?? resolveQueryResult(queries, undefined, parts.join('.')) ?? valueAt(localState, parts);
}

function resolveDataPath(path: string | undefined, selected: Record<string, unknown> | undefined, selections: Record<string, Record<string, unknown> | undefined>): unknown {
    const parts = path?.split('.').filter(Boolean) ?? [];
    if (parts.length > 0 && Object.hasOwn(selections, parts[0])) return valueAt(selections[parts[0]], parts.slice(1));
    return parts.length === 0 ? selected : valueAt(selected, parts);
}

function resolveQueryResult(queries: Record<string, QueryState>, query: string | undefined, path: string | undefined): unknown {
    const matching = Object.entries(queries)
        // A page-level read (a basket count, a `data` declaration's summary) is published for the bindings that name it; it is not one of the rows an unnamed path searches.
        .filter(([scope, state]) => query ? state.name === query || scope === query : !state.list)
        .map(([, state]) => state.rows);
    if (matching.length === 0) return undefined;
    const rows = matching.flat();
    const parts = path?.split('.').filter(Boolean) ?? [];
    if (parts.length === 0) return rows;
    if (rows.length === 1) return valueAt(rows[0], parts);
    return rows.map(row => valueAt(row, parts));
}

/** The identities the session holds (see `sessionIdentifier`), by the name a screen parameter gives them: `stage.session.BasketId` is `basketId`. */
function sessionParameters(): Record<string, string> {
    const found: Record<string, string> = {};
    try {
        const storage = globalThis.localStorage;
        for (let index = 0; storage && index < storage.length; index++) {
            const key = storage.key(index) ?? '';
            if (!key.startsWith(sessionIdentifierPrefix)) continue;
            const concept = key.slice(sessionIdentifierPrefix.length);
            const value = storage.getItem(key);
            if (concept && value) found[concept.charAt(0).toLowerCase() + concept.slice(1)] = value;
        }
    } catch {
        // storage is refused: the session holds no identities, and the address carries what a screen needs
    }

    return found;
}

function routeWithArguments(route: string, queryArguments: Record<string, unknown>): string {
    const [path, query = ''] = route.replace(/^\//, '').split('?');
    const parameters = new URLSearchParams(query);
    for (const [name, value] of Object.entries(queryArguments)) {
        if (value === undefined || value === null || value === '') continue;
        parameters.set(name, String(value));
    }

    const serialized = parameters.toString();
    return serialized ? `${path}?${serialized}` : path;
}

/** The marker a generated application puts on the route of a query the model declares `observable`. */
const liveMarker = 'waitForFirstResult';

/** The `readyState` of an `EventSource` the browser gave up on: a refused or failed subscription, not a retry. */
const eventSourceClosed = 2;

function isLiveRoute(route: string): boolean {
    return new URLSearchParams(route.split('?')[1] ?? '').get(liveMarker) === 'true';
}

function withoutLiveMarker(url: string): string {
    const [path, query = ''] = url.split('?');
    const parameters = new URLSearchParams(query);
    parameters.delete(liveMarker);
    const serialized = parameters.toString();
    return serialized ? `${path}?${serialized}` : path;
}

// What a result that carries no rows says about why: a denied or invalid subscription ends rather than retrying.
function queryFailure(result: unknown): string {
    if (!isRecord(result)) return '';
    if (result.isAuthorized === false) return 'The query answered 403.';
    if (result.isValid === false || result.hasExceptions === true) {
        const messages = Array.isArray(result.exceptionMessages) ? result.exceptionMessages.filter(message => typeof message === 'string') : [];
        return messages.length > 0 ? messages.join(' ') : 'The query could not be answered.';
    }

    return '';
}

function normalizeRows(payload: unknown): Record<string, unknown>[] {
    const data = isRecord(payload) && 'data' in payload ? payload.data : payload;
    if (Array.isArray(data)) return data.filter(isRecord);
    return isRecord(data) ? [data] : [];
}

function valueAt(value: unknown, parts: string[]): unknown {
    let current = value;
    for (const part of parts) {
        if (!isRecord(current)) return undefined;
        current = current[part];
    }

    return current;
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
}
