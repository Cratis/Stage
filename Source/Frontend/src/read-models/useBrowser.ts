// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useState } from 'react';
import { endpoints } from './endpoints';
import type { BrowserState } from './BrowserState';
import type { Detail } from './Detail';
import type { Summary } from './Summary';
import { browserEndpoint } from './path';

function record(value: unknown): Record<string, unknown> {
    return value !== null && typeof value === 'object' ? value as Record<string, unknown> : {};
}

async function read<T>(endpoint: string, signal: AbortSignal, selected: boolean): Promise<T> {
    const response = await fetch(browserEndpoint(endpoint), { signal });
    if (response.ok) return response.json() as Promise<T>;

    const body = record(await response.json().catch(() => undefined));
    if (response.status === 503) {
        throw new Error(body.state === 'faulted'
            ? `The semantic session is faulted: ${String(body.reason ?? 'Its state is indeterminate.')} Restart the session before browsing read models.`
            : 'The semantic world is still loading. Refresh after it has been rebuilt.');
    }
    if (response.status === 404) {
        throw new Error(body.engine === 'eventmodel' && typeof body.reason === 'string'
            ? body.reason
            : selected ? 'The selected read model was not found.' : 'Read-model browsing is unavailable for this session.');
    }
    if (response.status === 501) {
        const reasons = Array.isArray(body.exceptionMessages) ? body.exceptionMessages.join(' ') : '';
        throw new Error(`Stage refused this model.${reasons ? ` ${reasons}` : ''}`);
    }

    throw new Error(`The Stage returned ${response.status}.`);
}

function errorMessage(error: unknown): string {
    return error instanceof Error ? error.message : 'Could not read the session\'s read models.';
}

/** Loads summaries on mount or refresh and cancels superseded detail reads independently. */
export function useBrowser() {
    const [state, setState] = useState<Omit<BrowserState, 'detail'>>({ loading: true });
    const [detailState, setDetailState] = useState<Pick<BrowserState, 'detail' | 'message'>>({});
    const [selectedId, setSelectedId] = useState<string>();
    const [revision, setRevision] = useState(0);

    useEffect(() => {
        const controller = new AbortController();
        const load = async () => {
            try {
                const list = await read<{ readModels: Summary[] }>(endpoints.readModels, controller.signal, false);
                if (!controller.signal.aborted) setState({ loading: false, readModels: list.readModels });
            } catch (error) {
                if (!controller.signal.aborted) setState({ loading: false, message: errorMessage(error) });
            }
        };
        void load();
        return () => controller.abort();
    }, [revision]);

    useEffect(() => {
        if (!selectedId) return;
        const controller = new AbortController();
        const load = async () => {
            try {
                const detail = await read<Detail>(endpoints.readModel(selectedId), controller.signal, true);
                if (!controller.signal.aborted) setDetailState({ detail });
            } catch (error) {
                if (!controller.signal.aborted) setDetailState({ message: errorMessage(error) });
            }
        };
        void load();
        return () => controller.abort();
    }, [selectedId, revision]);

    return {
        ...state,
        detail: state.readModels ? detailState.detail : undefined,
        message: state.message ?? detailState.message,
        selectedId,
        select: (id: string) => {
            if (id === selectedId) return;
            setDetailState({});
            setSelectedId(id);
        },
        refresh: () => {
            setState({ loading: true });
            setDetailState({});
            setRevision(previous => previous + 1);
        },
    };
}
