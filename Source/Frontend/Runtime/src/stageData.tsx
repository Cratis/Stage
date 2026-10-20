// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import type React from 'react';
import type { BindingExpression } from '@cratis/scene.model';
import { ArcContext } from '@cratis/arc.react';
import { ObservableQueryFor, type QueryResult } from '@cratis/arc/queries';
import { applyQueryChangeSet } from './applyQueryChangeSet';
import { registeredQuery, performQuery, subscribeQuery } from './queryExecutor';
import { createBindingResolver, resolveArgumentSource, validateBindingExpression } from '@cratis/scene.engine';
import type { StageRoutes } from './stageRoutes';
import { guardPath } from './stageGuards';
import { sessionIdentities } from './stageSession';

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

    /** The parameters the screen's address carries - and nothing else. */
    parameters: Record<string, string>;

    /**
     * The identities the session acts as, by the name a screen parameter gives them (`basketId`). They are not the screen's parameters: only
     * what reads the session on purpose does - a form populating the identity it does not show, the key a `data` declaration is read by
     * when the address does not carry it (`screen.keys.<name>` in a binding), and the query bindings the session narrows.
     */
    session: Record<string, string>;
    queries: Record<string, QueryState>;
    selected: Record<string, unknown> | undefined;
    selections: Record<string, Record<string, unknown> | undefined>;
    selectRow: (scope: string, row: Record<string, unknown> | undefined) => void;
    clearSelection: (scope: string) => void;
    setState: (key: string, value: unknown) => void;
    setComponentOutput: (componentId: string, path: string, value: unknown) => void;

    /**
     * Takes what the components of one rendered tree publish (a list's selected row, a search box's text) so bindings and interactions elsewhere
     * on the page read it. The screen and a dialog above it are each a tree of their own, so each publishes under its own scope and replaces only
     * what it published before; see `useComponentOutputsPublisher`.
     */
    publishComponentOutputs: (scope: string, outputs: Record<string, Record<string, unknown>>) => void;

    /** Forgets what a rendered tree published, once it is gone - a closed dialog - leaving what every other tree published. */
    withdrawComponentOutputs: (scope: string) => void;

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
    session: {},
    queries: {},
    selected: undefined,
    selections: {},
    selectRow: () => undefined,
    clearSelection: () => undefined,
    setState: () => undefined,
    setComponentOutput: () => undefined,
    publishComponentOutputs: () => undefined,
    withdrawComponentOutputs: () => undefined,
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
    const [outputContributions, setOutputContributions] = useState<Record<string, Record<string, Record<string, unknown>>>>({});
    const [refreshRequests, setRefreshRequests] = useState<{ query?: string; version: number }>({ version: 0 });

    // What every rendered tree published, as one: the screen's selected row and a dialog's search box resolve side by side.
    const publishedOutputs = useMemo(() => mergedOutputs(outputContributions), [outputContributions]);

    const registerQueryResult = useCallback((scope: string, state: QueryState | undefined) => {
        setQueries(current => {
            if (!state) {
                const { [scope]: _, ...remaining } = current;
                return remaining;
            }

            return { ...current, [scope]: state };
        });
    }, []);

    // The address's parameters stay the address's: a session identity reaches only what reads the session on purpose (see `session`).
    const parameters = addressParameters;

    // A command can create the identity it acts as - the shopper's basket - so the session is read again after every command.
    const session = useMemo(() => sessionIdentities(), [sessionVersion]);

    // What the address does not carry, the session does: the shopper's own basket is the one a command created, so a screen about "the basket" - a
    // `data` declaration read by `basketId` - needs no id in its address.
    const screenScope = useMemo(() => ({ name: screen, keys: { ...session, ...addressParameters } }), [addressParameters, screen, session]);

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
        if (typeof binding === 'string') return resolvePath(binding, selected, selections, queries, localState, parameters, screenScope, locale, locales);

        const scope = bindingScope(selected, queries, localState, { ...publishedOutputs, ...componentOutputState }, selections);

        // `component.<id>.<output>` reads what another element of the page publishes - the row a list has selected, the text a search box holds.
        if (!binding.kind && binding.path.startsWith('component.')) return resolveArgumentSource(binding.path, scope);
        if (!binding.kind) return resolvePath(binding.path, selected, selections, queries, localState, parameters, screenScope, locale, locales);

        const diagnostics = validateBindingExpression(binding, scope);
        if (diagnostics.length > 0) {
            console.warn(`Scene binding could not be resolved: ${diagnostics.map(_ => `${_.code}:${_.message}`).join('; ')}`);
        }

        return createBindingResolver(scope)(binding);
    }, [componentOutputState, locale, locales, localState, parameters, publishedOutputs, queries, screenScope, selected, selections]);

    const resolveArgument = useCallback(
        (source: string | undefined): unknown => source ? resolveArgumentSource(source, bindingScope(selected, queries, localState, { ...publishedOutputs, ...componentOutputState }, selections)) : undefined,
        [componentOutputState, localState, publishedOutputs, queries, selected, selections]);

    const publishComponentOutputs = useCallback((scope: string, outputs: Record<string, Record<string, unknown>>) =>
        setOutputContributions(current => ({ ...current, [scope]: outputs })), []);
    const withdrawComponentOutputs = useCallback((scope: string) => setOutputContributions(current => {
        if (!Object.prototype.hasOwnProperty.call(current, scope)) return current;
        const remaining = { ...current };
        delete remaining[scope];
        return remaining;
    }), []);
    const queryResults = useMemo(() => bindingScope(undefined, queries, {}, {}, {}).queryResults, [queries]);

    const state = useMemo<StageDataState>(() => ({
        locale,
        locales,
        screen,
        routes,
        routesReady,
        parameters,
        session,
        queries,
        selected,
        selections,
        selectRow,
        clearSelection,
        setState: (key, value) => setLocalState(current => writePath(current, key.split('.').filter(Boolean), value)),
        setComponentOutput,
        publishComponentOutputs,
        withdrawComponentOutputs,
        queryResults,
        refreshQuery,
        refreshAfterCommand,
        resolveBinding,
        resolveArgument,
        registerQueryResult,
        refreshVersion: refreshRequests.version,
        refreshQueryName: refreshRequests.query,
    }), [clearSelection, locale, locales, queries, refreshAfterCommand, refreshQuery, refreshRequests.query, refreshRequests.version, registerQueryResult, publishComponentOutputs, withdrawComponentOutputs, queryResults, resolveArgument, resolveBinding, routes, routesReady, parameters, session, screen, selectRow, selected, selections, setComponentOutput]);

    return <StageDataContext.Provider value={state}>{children}</StageDataContext.Provider>;
}

export const useStageData = (): StageDataState => useContext(StageDataContext);

export function useStageQuery(request: StageQueryRequest): StageQueryResult {
    const { refreshQueryName, refreshVersion, registerQueryResult } = useStageData();
    const arc = useContext(ArcContext);
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
    const active = !!request.name && ready;
    const argumentsKey = JSON.stringify(request.arguments ?? {});

    useEffect(() => {
        if (!active) {
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
        const queryType = registeredQuery(request.name ?? '', request.route);
        setLoading(true);
        setError('');

        const failed = (reason: unknown) => {
            if (latest.current !== sequence || abort.signal.aborted) return;
            liveOpen.current = false;
            setRows([]);
            setError(reason instanceof Error ? reason.message : String(reason));
            setLoading(false);
        };
        let previousData: unknown = [];
        const receive = (result: QueryResult<unknown>) => {
            if (latest.current !== sequence || abort.signal.aborted) return;
            const failure = queryFailure(result);
            if (failure) { failed(new Error(failure)); return; }
            if (result.isReady === false) return;
            previousData = result.changeSet && Array.isArray(result.data) && result.data.length === 0 && Array.isArray(previousData)
                ? applyQueryChangeSet(previousData, result.changeSet)
                : result.data;
            setRows(normalizeRows(previousData));
            setError('');
            setLoading(false);
        };
        if (!queryType) {
            failed(new Error(`The modeled query “${request.name}” is not registered by this Stage.`));
            return () => abort.abort();
        }
        const observable = queryType.prototype instanceof ObservableQueryFor;
        const read = () => Promise.resolve()
            .then(() => performQuery(queryType, observable ? { ...queryArguments, waitForFirstResult: true } : queryArguments, arc))
            .then(receive).catch(failed);
        if (!observable) {
            void read();
            return () => abort.abort();
        }

        // Arc owns reconnection after delivery. Read once through Arc after its connection deadline if
        // no first ready result arrives, surfacing rejection instead of leaving the screen loading forever.
        liveOpen.current = true;
        let subscription: ReturnType<typeof subscribeQuery>;
        const deadline = globalThis.setTimeout(() => {
            if (latest.current !== sequence || abort.signal.aborted) return;
            liveOpen.current = false;
            subscription?.unsubscribe();
            subscription = undefined;
            void read();
        }, 15000);
        try {
            subscription = subscribeQuery(queryType, queryArguments, arc, result => {
                if (result.isReady !== false) globalThis.clearTimeout(deadline);
                receive(result);
            });
        } catch {
            globalThis.clearTimeout(deadline);
            liveOpen.current = false;
            void read();
        }
        return () => {
            abort.abort();
            globalThis.clearTimeout(deadline);
            liveOpen.current = false;
            subscription?.unsubscribe();
        };
    }, [active, arc, argumentsKey, localRefresh, publishResult, request.name, request.route, request.scope]);

    useEffect(() => {
        if (refreshVersion === 0) return;
        if (refreshQueryName && refreshQueryName !== request.name && refreshQueryName !== request.scope) return;

        // An open stream already carries every change, so a refresh must not tear it down and reopen it.
        if (liveOpen.current) return;
        setLocalRefresh(current => current + 1);
    }, [refreshQueryName, refreshVersion, request.name, request.scope]);

    useEffect(() => {
        if (!active) {
            publishResult(request.scope, undefined);
            return;
        }

        publishResult(request.scope, {
            name: request.name,
            list: request.list,
            alias: request.alias,
            route: request.route ?? '',
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

function mergedOutputs(contributions: Record<string, Record<string, Record<string, unknown>>>): Record<string, Record<string, unknown>> {
    const merged: Record<string, Record<string, unknown>> = {};
    for (const outputs of Object.values(contributions)) {
        for (const [componentId, values] of Object.entries(outputs)) merged[componentId] = { ...merged[componentId], ...values };
    }

    return merged;
}

/** What a `screen.` path reads: the screen's name, and the keys its `data` declarations are read by - the address's parameter, or the session's identity of that name. */
interface ScreenScope {
    name: string;
    keys: Record<string, string>;
}

function resolvePath(
    path: string | undefined,
    selected: Record<string, unknown> | undefined,
    selections: Record<string, Record<string, unknown> | undefined>,
    queries: Record<string, QueryState>,
    localState: Record<string, unknown>,
    parameters: Record<string, string>,
    screen: ScreenScope,
    locale: string,
    locales: string[],
): unknown {
    if (!path) return undefined;

    const parts = path.replace(/^\$\.?/, '').split('.').filter(Boolean);
    if (parts.length === 0) return undefined;

    if (parts[0] === 'screen') {
        if (parts.length === 1) return { name: screen.name };

        // A key neither the address nor the session carries is the one the user picked: the master list of a master-detail screen
        // is read by what it selects, so its details wait for a row instead of asking a query that needs one without it.
        const key = valueAt(screen, parts.slice(1));
        return key === undefined && parts[1] === 'keys' && parts.length === 3 ? valueAt(selected, [parts[2]]) : key;
    }
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

// What a result that carries no rows says about why: a denied or invalid subscription ends rather than retrying.
function queryFailure(result: unknown): string {
    if (!isRecord(result)) return '';
    if (result.isAuthorized === false) return 'The query answered 403.';
    if (result.isValid === false || result.hasExceptions === true || result.isSuccess === false) {
        const messages = Array.isArray(result.exceptionMessages) ? result.exceptionMessages.filter(message => typeof message === 'string') : [];
        return messages.length > 0 ? messages.join(' ') : 'The query could not be answered.';
    }

    return '';
}

function normalizeRows(data: unknown): Record<string, unknown>[] {
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
