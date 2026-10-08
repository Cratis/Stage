// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type React from 'react';
import type { BindingExpression } from '@cratis/scene.model';
import type { StageRoutes } from './stageRoutes';

const DATA_CHANGED = 'cratis.stage.data-changed';

interface QueryState {
    route: string;
    rows: Record<string, unknown>[];
    loading: boolean;
    error: string;
}

export interface StageDataState {
    locale: string;
    locales: string[];
    screen: string;
    queries: Record<string, QueryState>;
    selected: Record<string, unknown> | undefined;
    setSelected: (row: Record<string, unknown> | undefined) => void;
    setState: (key: string, value: unknown) => void;
    refreshQuery: (query?: string) => void;
    resolveBinding: (binding: BindingExpression | string | undefined) => unknown;
}

const emptyState: StageDataState = {
    locale: '',
    locales: [],
    screen: '',
    queries: {},
    selected: undefined,
    setSelected: () => undefined,
    setState: () => undefined,
    refreshQuery: () => undefined,
    resolveBinding: () => undefined,
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
    const [selected, setSelected] = useState<Record<string, unknown>>();
    const [localState, setLocalState] = useState<Record<string, unknown>>({});

    const readQuery = useCallback(async (name: string, route: string, signal?: AbortSignal) => {
        setQueries(current => ({
            ...current,
            [name]: { route, rows: current[name]?.rows ?? [], loading: true, error: '' },
        }));

        try {
            const response = await fetch(route.replace(/^\//, ''), { headers: { Accept: 'application/json' }, signal });
            if (!response.ok) throw new Error(`The query answered ${response.status}.`);
            const payload = await response.json() as unknown;
            const data = normalizeRows(payload);
            setQueries(current => ({ ...current, [name]: { route, rows: data, loading: false, error: '' } }));
            setSelected(current => current ?? data[0]);
        } catch (reason) {
            if (signal?.aborted) return;
            setQueries(current => ({
                ...current,
                [name]: { route, rows: current[name]?.rows ?? [], loading: false, error: reason instanceof Error ? reason.message : String(reason) },
            }));
        }
    }, []);

    const refreshQuery = useCallback((query?: string) => {
        if (!routes) return;
        for (const [name, route] of Object.entries(routes.queries)) {
            if (query && query !== name) continue;
            void readQuery(name, route);
        }
    }, [readQuery, routes]);

    useEffect(() => {
        if (!routes) return;
        const abort = new AbortController();
        for (const [name, route] of Object.entries(routes.queries)) {
            void readQuery(name, route, abort.signal);
        }

        const dataChanged = () => refreshQuery();
        const refresh = (event: Event) => refreshQuery((event as CustomEvent<{ query?: string }>).detail?.query);
        globalThis.addEventListener(DATA_CHANGED, dataChanged);
        globalThis.addEventListener('cratis.scene.refresh', refresh);
        return () => {
            abort.abort();
            globalThis.removeEventListener(DATA_CHANGED, dataChanged);
            globalThis.removeEventListener('cratis.scene.refresh', refresh);
        };
    }, [readQuery, refreshQuery, routes]);

    const resolveBinding = useCallback((binding: BindingExpression | string | undefined): unknown => {
        const path = typeof binding === 'string' ? binding : binding?.path;
        if (!path) return undefined;

        const parts = path.replace(/^\$\.?/, '').split('.').filter(Boolean);
        if (parts.length === 0) return undefined;

        if (parts[0] === 'screen') return parts.length === 1 ? { name: screen } : valueAt({ name: screen }, parts.slice(1));
        if (parts[0] === 'locale') return locale;
        if (parts[0] === 'locales') return locales;

        if (parts[0] === 'query') {
            return resolveQueryPath(queries, parts.slice(1));
        }

        if (parts[0] === 'data' || parts[0] === 'selected' || parts[0] === 'current') {
            return valueAt(selected, parts.slice(1));
        }

        if (parts[0] === 'state') {
            return valueAt(localState, parts.slice(1));
        }

        return valueAt(selected, parts) ?? resolveQueryPath(queries, parts) ?? valueAt(localState, parts);
    }, [locale, locales, localState, queries, screen, selected]);

    const state = useMemo<StageDataState>(() => ({
        locale,
        locales,
        screen,
        queries,
        selected,
        setSelected,
        setState: (key, value) => setLocalState(current => ({ ...current, [key]: value })),
        refreshQuery,
        resolveBinding,
    }), [locale, locales, queries, refreshQuery, resolveBinding, screen, selected]);

    return <StageDataContext.Provider value={state}>{children}</StageDataContext.Provider>;
}

export const useStageData = (): StageDataState => useContext(StageDataContext);

export function dataChanged() {
    globalThis.dispatchEvent(new CustomEvent(DATA_CHANGED));
}

function normalizeRows(payload: unknown): Record<string, unknown>[] {
    const data = isRecord(payload) && 'data' in payload ? payload.data : payload;
    if (Array.isArray(data)) return data.filter(isRecord);
    return isRecord(data) ? [data] : [];
}

function resolveQueryPath(queries: Record<string, QueryState>, parts: string[]): unknown {
    if (parts.length === 0) return undefined;

    if (queries[parts[0]]) {
        const rows = queries[parts[0]].rows;
        if (parts.length === 1) return rows;
        if (rows.length === 1) return valueAt(rows[0], parts.slice(1));
        return rows.map(row => valueAt(row, parts.slice(1)));
    }

    for (const query of Object.values(queries)) {
        const first = query.rows[0];
        const value = valueAt(first, parts);
        if (value !== undefined) return value;
    }

    return undefined;
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
