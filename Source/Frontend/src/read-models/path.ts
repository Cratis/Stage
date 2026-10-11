// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** Only the browser page path switches away from the normal Scene application. */
export function isBrowserPath(path: string): boolean {
    return /\/read-models\/?$/.test(path);
}

/** Keeps the endpoint relative to the session base, including a trailing slash on the browser page. */
export function browserEndpoint(endpoint: string, path = window.location.pathname): string {
    return path.endsWith('/read-models/') ? `../${endpoint}` : endpoint;
}
