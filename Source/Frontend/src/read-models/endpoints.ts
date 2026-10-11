// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** The host-only browser endpoints, relative to the page so a proxied session still finds them. */
export const endpoints = {
    readModels: 'stage/read-models',
    readModel: (id: string) => `stage/read-models/${encodeURIComponent(id)}`,
};
