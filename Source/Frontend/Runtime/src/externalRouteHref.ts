// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** Keeps only absolute HTTP and HTTPS routes as external navigation links. */
export function externalRouteHref(route: string | undefined): string | undefined {
    try {
        const url = new URL(route ?? '');
        return url.protocol === 'http:' || url.protocol === 'https:' ? route : undefined;
    } catch {
        return undefined;
    }
}
