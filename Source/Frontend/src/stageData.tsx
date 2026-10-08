// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import type React from 'react';
import type { BindingExpression } from '@cratis/scene.model';
import type { StageRoutes } from './stageRoutes';

const DATA_CHANGED = 'cratis.stage.data-changed';

interface QueryState {
    name?: string;
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
}

export interface StageQueryResult {
    rows: Record<string, unknown>[];
    loading: boolean;
    error: string;
    refresh: () => void;
}

interface TypedBindingExpression {
    kind?: string;
    path?: string;
    query?: string;
    componentId?: string;
    componentPropertyPath?: string;
    property?: string;
    value?: unknown;
}

export interface StageDataState {
    locale: string;
    locales: string[];
    screen: string;
    routes: StageRoutes | undefined;
    queries: Record<string, QueryState>;
    selected: Record<string, unknown> | undefined;
    selections: Record<string, Record<string, unknown> | undefined>;
    selectRow: (scope: string, row: Record<string, unknown> | undefined) => void;
    clearSelection: (scope: string) => void;
    setState: (key: string, value: unknown) => void;
    refreshQuery: (query?: string) => void;
    resolveBinding: (binding: BindingExpression | TypedBindingExpression | string | undefined) => unknown;
    registerQueryResult: (scope: string, state: QueryState | undefined) => void;
    refreshVersion: number;
    refreshQueryName?: string;
}

const emptyState: StageDataState = {
    locale: '',
    locales: [],
    screen: '',
    routes: undefined,
    queries: {},
    selected: undefined,
    selections: {},
    selectRow: () => undefined,
    clearSelection: () => undefined,
    setState: () => undefined,
    refreshQuery: () => undefined,
    resolveBinding: () => undefined,
    registerQueryResult: () => undefined,
    refreshVersion: 0,
};

const StageDataContext = createContext<StageDataState>(emptyState);

export interface StageDataProviderProps {
    routes: StageRoutes | undefined;
    locale: string;
    locales: string[];
    screen: string;
    children: React.ReactNode;
}

export function StageDataProvider({ routes, locale, locales, screen, children }: StageDataProviderProps) {
    const [queries, setQueries] = useState<Record<string, QueryState>>({});
    const [selections, setSelections] = useState<Record<string, Record<string, unknown> | undefined>>({});
    const [activeSelection, setActiveSelection] = useState<string>();
    const [localState, setLocalState] = useState<Record<string, unknown>>({});
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

    const refreshQuery = useCallback((query?: string) =>
        setRefreshRequests(current => ({ query, version: current.version + 1 })), []);

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

    const resolveBinding = useCallback((binding: BindingExpression | TypedBindingExpression | string | undefined): unknown => {
        if (binding === undefined) return undefined;
        if (typeof binding === 'object' && 'kind' in binding && binding.kind) {
            return resolveTypedBinding(binding, selected, selections, queries, localState);
        }

        const path = typeof binding === 'string' ? binding : binding.path;
        return resolvePath(path, selected, selections, queries, localState, screen, locale, locales);
    }, [locale, locales, localState, queries, screen, selected, selections]);

    const state = useMemo<StageDataState>(() => ({
        locale,
        locales,
        screen,
        routes,
        queries,
        selected,
        selections,
        selectRow,
        clearSelection,
        setState: (key, value) => setLocalState(current => ({ ...current, [key]: value })),
        refreshQuery,
        resolveBinding,
        registerQueryResult,
        refreshVersion: refreshRequests.version,
        refreshQueryName: refreshRequests.query,
    }), [clearSelection, locale, locales, queries, refreshQuery, refreshRequests.query, refreshRequests.version, registerQueryResult, resolveBinding, routes, screen, selectRow, selected, selections]);

    return <StageDataContext.Provider value={state}>{children}</StageDataContext.Provider>;
}

export const useStageData = (): StageDataState => useContext(StageDataContext);

export function useStageQuery(request: StageQueryRequest): StageQueryResult {
    const { refreshQueryName, refreshVersion, registerQueryResult } = useStageData();
    const [rows, setRows] = useState<Record<string, unknown>[]>([]);
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);
    const [localRefresh, setLocalRefresh] = useState(0);
    const latest = useRef(0);
    const argumentsKey = JSON.stringify(request.arguments ?? {});

    useEffect(() => {
        if (!request.route) {
            registerQueryResult(request.scope, undefined);
            return;
        }

        const sequence = latest.current + 1;
        latest.current = sequence;
        const abort = new AbortController();
        const queryArguments = JSON.parse(argumentsKey) as Record<string, unknown>;
        const url = routeWithArguments(request.route, queryArguments);

        setLoading(true);
        setError('');
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
                setError(reason instanceof Error ? reason.message : String(reason));
            })
            .finally(() => {
                if (latest.current === sequence && !abort.signal.aborted) setLoading(false);
            });

        return () => abort.abort();
    }, [argumentsKey, localRefresh, registerQueryResult, request.name, request.route, request.scope]);

    useEffect(() => {
        if (refreshVersion === 0) return;
        if (refreshQueryName && refreshQueryName !== request.name && refreshQueryName !== request.scope) return;
        setLocalRefresh(current => current + 1);
    }, [refreshQueryName, refreshVersion, request.name, request.scope]);

    useEffect(() => {
        registerQueryResult(request.scope, {
            name: request.name,
            route: request.route ?? '',
            rows,
            loading,
            error,
            arguments: JSON.parse(argumentsKey) as Record<string, unknown>,
        });
    }, [argumentsKey, error, loading, registerQueryResult, request.name, request.route, request.scope, rows]);

    return { rows, loading, error, refresh: () => setLocalRefresh(current => current + 1) };
}

export function dataChanged() {
    globalThis.dispatchEvent(new CustomEvent(DATA_CHANGED));
}

function resolveTypedBinding(
    binding: TypedBindingExpression,
    selected: Record<string, unknown> | undefined,
    selections: Record<string, Record<string, unknown> | undefined>,
    queries: Record<string, QueryState>,
    localState: Record<string, unknown>,
): unknown {
    switch (binding.kind) {
        case 'literal': return binding.value;
        case 'dataContext': return resolveDataPath(binding.path ?? binding.property, selected, selections);
        case 'queryResult': return resolveQueryResult(queries, binding.query, binding.path ?? binding.property);
        case 'componentProperty': return resolveComponentProperty(localState, binding.componentId, binding.componentPropertyPath ?? binding.property);
        default: return undefined;
    }
}

function resolveComponentProperty(localState: Record<string, unknown>, componentId: string | undefined, property: string | undefined): unknown {
    if (!property) return undefined;
    if (!componentId) return localState[property];
    const componentState = localState[componentId];
    return valueAt(componentState, [property]) ?? localState[`${componentId}.${property}`];
}

function resolvePath(
    path: string | undefined,
    selected: Record<string, unknown> | undefined,
    selections: Record<string, Record<string, unknown> | undefined>,
    queries: Record<string, QueryState>,
    localState: Record<string, unknown>,
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

    return valueAt(selected, parts) ?? resolveQueryResult(queries, undefined, parts.join('.')) ?? valueAt(localState, parts);
}

function resolveDataPath(path: string | undefined, selected: Record<string, unknown> | undefined, selections: Record<string, Record<string, unknown> | undefined>): unknown {
    const parts = path?.split('.').filter(Boolean) ?? [];
    if (parts.length > 0 && Object.hasOwn(selections, parts[0])) return valueAt(selections[parts[0]], parts.slice(1));
    return parts.length === 0 ? selected : valueAt(selected, parts);
}

function resolveQueryResult(queries: Record<string, QueryState>, query: string | undefined, path: string | undefined): unknown {
    const matching = Object.entries(queries)
        .filter(([scope, state]) => !query || state.name === query || scope === query)
        .map(([, state]) => state.rows);
    if (matching.length === 0) return undefined;
    const rows = matching.flat();
    const parts = path?.split('.').filter(Boolean) ?? [];
    if (parts.length === 0) return rows;
    if (rows.length === 1) return valueAt(rows[0], parts);
    return rows.map(row => valueAt(row, parts));
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
