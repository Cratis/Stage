// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useContext, useMemo, type ReactNode } from 'react';
import type { Availability } from '@cratis/arc/availability';
import { ArcContext } from '@cratis/arc.react';
import { AvailabilityProvider, useAvailability } from '@cratis/arc.react/availability';
import { apiBasePathFor } from './hostedBasePath';
import type { StageRoutes } from './stageRoutes';

/** The exact Arc part key of each gated screen. Screens absent from the map are ungated. */
export type ScreenParts = Record<string, string>;

/** Says whether a screen may be rendered or linked to, and, for a screen an operator suspended, that it is only suspended. */
export interface ScreenAvailability {
    (screen: string | undefined): boolean;

    /** Whether the screen is unavailable because an operator suspended it, as opposed to not being in the caller's offer. */
    suspended?: (screen: string | undefined) => boolean;
}

function screenIsAvailable(screen: string | undefined, parts: ScreenParts | undefined, availability: Availability): boolean {
    if (!screen) return true;
    if (!parts) return true;
    if (!Object.hasOwn(parts, screen)) return true;
    return availability.isAvailable(parts[screen]);
}

function screenIsSuspended(screen: string | undefined, parts: ScreenParts | undefined, availability: Availability): boolean {
    if (!screen || !parts || !Object.hasOwn(parts, screen)) return false;
    return availability.availabilityOf(parts[screen]).isSuspended;
}

/**
 * Provides Arc's availability to everything below it, observed from the API base path the application is hosted under: the one Arc is configured with,
 * or the hosted prefix when it has none.
 */
export function StageAvailabilityProvider({ children }: { children: ReactNode }) {
    const arc = useContext(ArcContext);
    const hosted = useMemo(() => ({ ...arc, apiBasePath: apiBasePathFor(arc.apiBasePath) }), [arc]);
    return <ArcContext.Provider value={hosted}><AvailabilityProvider>{children}</AvailabilityProvider></ArcContext.Provider>;
}

/**
 * Hides a gated screen until its exact part is known to be available; failures stay closed. Ungated screens need no request.
 * Arc's availability, provided by the `AvailabilityProvider` the application mounts, carries the reason of every refusal, so a suspended screen can be told from one that is not offered.
 * @param routes The routes, including the gated screen map.
 * @param lookupFailed Whether the route lookup failed to establish the screen gate map.
 * @returns Whether a screen may be rendered or linked to.
 */
export function useScreenAvailability(routes: StageRoutes | undefined, lookupFailed = false): ScreenAvailability {
    const availability = useAvailability();
    const parts = routes?.screens;
    return useMemo(() => Object.assign(
        (screen: string | undefined) => lookupFailed ? !screen : screenIsAvailable(screen, parts, availability),
        { suspended: (screen: string | undefined) => !lookupFailed && screenIsSuspended(screen, parts, availability) }),
    [lookupFailed, parts, availability]);
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
