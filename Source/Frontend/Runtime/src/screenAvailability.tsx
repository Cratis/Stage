// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useState, type ReactNode } from 'react';
import type { StageRoutes } from './stageRoutes';

/** The exact Arc part key of each gated screen. Screens absent from the map are ungated. */
export type ScreenParts = Record<string, string>;

/**
 * Reads Arc 23.5's request-specific unavailable-part snapshot. A failed request never opens a gated screen.
 * @param signal Cancels an abandoned request.
 * @returns The exact names of unavailable parts.
 */
export async function fetchUnavailableScreens(signal: AbortSignal): Promise<Set<string>> {
    const response = await fetch('/.cratis/availability?waitForFirstResult=true', { signal, credentials: 'same-origin' });
    if (!response.ok) throw new Error('Unable to read screen availability.');
    const result: unknown = await response.json();
    if (!validSnapshot(result)) throw new Error('Invalid screen availability.');
    return new Set(result.data.unavailable.map(part => part.name));
}

interface AvailabilitySnapshot {
    isSuccess: boolean;
    data: { unavailable: { name: string }[] };
}

function validSnapshot(result: unknown): result is AvailabilitySnapshot {
    const snapshot = result as Partial<AvailabilitySnapshot> | null;
    if (snapshot?.isSuccess !== true) return false;
    return unavailableParts(snapshot.data?.unavailable);
}

function unavailableParts(value: unknown): value is { name: string }[] {
    return Array.isArray(value) && value.every(part => typeof part?.name === 'string');
}

function screenIsAvailable(screen: string | undefined, parts: ScreenParts | undefined, unavailable: Set<string> | undefined): boolean {
    if (!screen || !parts || !Object.hasOwn(parts, screen)) return true;
    return unavailable !== undefined && !unavailable.has(parts[screen]);
}

/**
 * Hides a gated screen until its exact part is known to be available; failures stay closed. Ungated screens need no request.
 * @param routes The routes, including the gated screen map.
 * @returns Whether a screen may be rendered or linked to.
 */
export function useScreenAvailability(routes: StageRoutes | undefined): (screen: string | undefined) => boolean {
    const parts = routes?.screens;
    const key = JSON.stringify(parts ?? {});
    const [snapshot, setSnapshot] = useState<{ key: string; unavailable: Set<string> }>();
    useEffect(() => {
        if (key === '{}') return;
        let abort = new AbortController();
        // Replace this Stage-owned bridge with Arc's availability components once Showrunner supports Arc 23.
        // Arc 22 has no availability export; do not override Showrunner's Arc peer dependencies to import it.
        const refresh = () => {
            abort.abort();
            const request = new AbortController();
            abort = request;
            setSnapshot(undefined);
            void fetchUnavailableScreens(request.signal)
                .then(unavailable => { if (!request.signal.aborted) setSnapshot({ key, unavailable }); })
                .catch(() => { /* Keep the pending snapshot closed on failure. The next refresh can retry. */ });
        };
        refresh();
        const interval = globalThis.setInterval(refresh, 30_000);
        globalThis.addEventListener('focus', refresh);
        return () => {
            abort.abort();
            globalThis.clearInterval(interval);
            globalThis.removeEventListener('focus', refresh);
        };
    }, [key]);
    return useCallback(screen => screenIsAvailable(screen, parts, snapshot?.key === key ? snapshot.unavailable : undefined), [parts, key, snapshot]);
}

/** Waits for the route lookup so content cannot flash before its screen gate map arrives. */
export function ScreenRoutesReady({ ready, children }: { ready: boolean; children: ReactNode }) {
    if (!ready) return <main className='stage-message'><h1>Preparing the Stage</h1></main>;
    return children;
}

/** Refuses a requested screen before mounting its content, including data bindings and command controls. */
export function ScreenAvailabilityGuard({ screens, isAvailable, children }: {
    screens: (string | undefined)[];
    isAvailable: (screen: string | undefined) => boolean;
    children: ReactNode;
}) {
    if (screens.some(screen => !isAvailable(screen))) {
        return <main className='stage-message' role='alert'><h1>Screen not found</h1></main>;
    }
    return children;
}
