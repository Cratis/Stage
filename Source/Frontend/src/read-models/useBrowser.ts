// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useState } from 'react';
import { stageEndpoints } from '../stageSource';
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

/** Reads the live world on selection or refresh; cancellation prevents older requests from replacing it. */
export function useBrowser() {
    const [state, setState] = useState<BrowserState>({ loading: true });
    const [selectedId, setSelectedId] = useState<string>();
    const [revision, setRevision] = useState(0);

    useEffect(() => {
        const controller = new AbortController();
        const load = async () => {
            try {
                const list = await read<{ readModels: Summary[] }>(stageEndpoints.readModels, controller.signal, false);
                const detail = selectedId ? await read<Detail>(stageEndpoints.readModel(selectedId), controller.signal, true) : undefined;
                if (!controller.signal.aborted) setState({ loading: false, readModels: list.readModels, detail });
            } catch (error) {
                if (!controller.signal.aborted) setState({ loading: false, message: error instanceof Error ? error.message : 'Could not read the session\'s read models.' });
            }
        };
        void load();
        return () => controller.abort();
    }, [selectedId, revision]);

    return {
        ...state,
        selectedId,
        select: (id: string) => {
            if (id === selectedId) return;
            setState({ loading: true });
            setSelectedId(id);
        },
        refresh: () => {
            setState({ loading: true });
            setRevision(previous => previous + 1);
        },
    };
}
