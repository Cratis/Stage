// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { stageEndpoints, type StageSource } from './stageSource';

/** A modeled name and its display-only description. Identity is always resolved by the host. */
export interface SandboxActingUser {
    name: string;
    displayName: string;
    personas: string[];
}

/** The live sandbox's browser-local selection. Static and published sources do not offer it. */
export interface SandboxActingUserState {
    enabled: true;
    users: SandboxActingUser[];
    current: string | null;
}

/** Reads only the host-gated surface, relative to the current sandbox base URL. */
export async function readSandboxActingUser(signal: AbortSignal, fetcher: typeof fetch = fetch): Promise<SandboxActingUserState | undefined> {
    const response = await fetcher(stageEndpoints.actingUser, { signal, credentials: 'same-origin' });
    if (!response.ok) return undefined;
    const state = await response.json() as SandboxActingUserState;
    return state.enabled === true && Array.isArray(state.users) && state.users.length > 0 ? state : undefined;
}

/** Writes only a modeled name (or clears it); no caller-supplied roles or claims travel in the selection. */
export async function writeSandboxActingUser(name: string | null, fetcher: typeof fetch = fetch): Promise<void> {
    const response = await fetcher(stageEndpoints.actingUser, {
        method: 'PUT', credentials: 'same-origin', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ name }),
    });
    if (!response.ok) throw new Error(`The sandbox could not change the acting user (${response.status}).`);
}

/** Refreshes identity, authorization and subscriptions together, only after the host accepts the selection. */
export async function changeSandboxActingUser(source: StageSource, name: string | null, reload: () => void): Promise<void> {
    if (!source.selectActingUser) return;
    await source.selectActingUser(name);
    reload();
}
