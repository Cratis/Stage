// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { ArcContext } from '@cratis/arc.react';
import { fetchUnavailableParts, type UnavailableParts } from './availabilityClient';
import type { StageRoutes } from './stageRoutes';

/** The exact Arc part key of each gated screen. Screens absent from the map are ungated. */
export type ScreenParts = Record<string, string>;

/** Says whether a screen may be rendered or linked to, and, for a screen an operator suspended, that it is only suspended. */
export interface ScreenAvailability {
    (screen: string | undefined): boolean;

    /** Whether the screen is unavailable because an operator suspended it, as opposed to not being in the caller's offer. */
    suspended?: (screen: string | undefined) => boolean;
}

function screenIsAvailable(screen: string | undefined, parts: ScreenParts | undefined, unavailable: UnavailableParts | undefined): boolean {
    if (!screen) return true;
    if (!parts) return true;
    if (!Object.hasOwn(parts, screen)) return true;
    return unavailable !== undefined && !unavailable.has(parts[screen]);
}

function screenIsSuspended(screen: string | undefined, parts: ScreenParts | undefined, unavailable: UnavailableParts | undefined): boolean {
    if (!screen || !parts || !unavailable || !Object.hasOwn(parts, screen)) return false;
    return unavailable.get(parts[screen]) === 'Suspended';
}

/**
 * Hides a gated screen until its exact part is known to be available; failures stay closed. Ungated screens need no request.
 * The Stage-owned client reads `/.cratis/availability` with the reason of every refusal, so a suspended screen can be told from one that is not offered.
 * @param routes The routes, including the gated screen map.
 * @param lookupFailed Whether the route lookup failed to establish the screen gate map.
 * @returns Whether a screen may be rendered or linked to.
 */
export function useScreenAvailability(routes: StageRoutes | undefined, lookupFailed = false): ScreenAvailability {
    const arc = useContext(ArcContext);
    const parts = routes?.screens;
    const key = JSON.stringify(parts ?? {});
    const [snapshot, setSnapshot] = useState<{ key: string; unavailable: UnavailableParts }>();
    useEffect(() => {
        if (key === '{}') return;
        let abort = new AbortController();
        // This Stage-owned client stays until the frontend is off Arc JS 22, which has no availability export (Cratis/Stage#1135).
        // Do not override Showrunner's Arc peer dependencies to import it.
        const refresh = () => {
            abort.abort();
            const request = new AbortController();
            abort = request;
            void fetchUnavailableParts(request.signal, arc)
                .then(unavailable => { if (!request.signal.aborted) setSnapshot({ key, unavailable }); })
                .catch(() => { if (!request.signal.aborted) setSnapshot(undefined); });
        };
        refresh();
        const interval = globalThis.setInterval(refresh, 30_000);
        globalThis.addEventListener('focus', refresh);
        return () => {
            abort.abort();
            globalThis.clearInterval(interval);
            globalThis.removeEventListener('focus', refresh);
        };
    }, [key, arc]);
    return useMemo(() => {
        const unavailable = snapshot?.key === key ? snapshot.unavailable : undefined;
        return Object.assign(
            (screen: string | undefined) => lookupFailed ? !screen : screenIsAvailable(screen, parts, unavailable),
            { suspended: (screen: string | undefined) => !lookupFailed && screenIsSuspended(screen, parts, unavailable) });
    }, [lookupFailed, parts, key, snapshot]);
}

/** Waits for the route lookup so content cannot flash before its screen gate map arrives. */
export function ScreenRoutesReady({ ready, children }: { ready: boolean; children: ReactNode }) {
    if (!ready) return <main className='stage-message'><h1>Preparing the Stage</h1></main>;
    return children;
}

/**
 * Refuses a requested screen before mounting its content, including data bindings and command controls. A screen an operator suspended
 * says so; any other refused screen is as if it did not exist.
 */
export function ScreenAvailabilityGuard({ screens, isAvailable, children }: {
    screens: (string | undefined)[];
    isAvailable: ScreenAvailability;
    children: ReactNode;
}) {
    const refused = screens.filter(screen => !isAvailable(screen));
    if (refused.length === 0) return children;
    if (isAvailable.suspended && refused.every(screen => isAvailable.suspended?.(screen))) {
        return <main className='stage-message' role='alert'><h1>Back soon</h1><p>This screen is switched off for now.</p></main>;
    }
    return <main className='stage-message' role='alert'><h1>Screen not found</h1></main>;
}
