// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** Where a session keeps the identity it acts as, one per identifier concept. */
export const sessionIdentifierPrefix = 'stage.session.';

/**
 * The identity the session already holds for an identifier concept - the shopper's basket - without creating one.
 *
 * @param concept The identifier concept, as the command's schema names it: `BasketId`.
 * @returns The identity, or nothing when the session holds none or storage is refused.
 */
export function heldSessionIdentity(concept: string): string | undefined {
    try {
        return globalThis.localStorage?.getItem(`${sessionIdentifierPrefix}${concept}`) || undefined;
    } catch {
        return undefined;
    }
}

/**
 * The identity the session acts as for an identifier concept: the one it holds, or a new one it keeps from now on - the same basket for
 * every add, change and checkout in this browser, a different one in another.
 *
 * @param concept The identifier concept, as the command's schema names it: `BasketId`.
 * @returns The identity.
 */
export function sessionIdentity(concept: string): string {
    const existing = heldSessionIdentity(concept);
    if (existing) return existing;

    const created = globalThis.crypto.randomUUID();
    try {
        globalThis.localStorage?.setItem(`${sessionIdentifierPrefix}${concept}`, created);
    } catch {
        // storage is refused: the identity lasts for this form only
    }

    return created;
}
