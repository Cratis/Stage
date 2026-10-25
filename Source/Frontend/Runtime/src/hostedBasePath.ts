// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The path the page is served under, without a trailing slash: empty at the root of a host, `/api/play/{id}` when a
 * reverse proxy (Studio's Play) hangs the Stage off a prefix. Registered command and query routes start with `/`, so without
 * this they would go to the root of the proxy's host instead of to the Stage.
 */
export function hostedBasePath(): string {
    const document = globalThis.document;
    const address = document?.baseURI ?? globalThis.location?.href;
    if (!address) return '';
    try {
        return new URL('.', address).pathname.replace(/\/+$/, '');
    } catch {
        return '';
    }
}

/** Arc's default configuration carries an empty base path, so an empty one means "not configured" just like an absent one. */
export function apiBasePathFor(arcApiBasePath?: string): string {
    return arcApiBasePath || hostedBasePath();
}
