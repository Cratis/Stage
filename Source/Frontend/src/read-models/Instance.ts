// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** The key and named JSON values of one committed read-model instance. */
export interface Instance {
    key: unknown;
    values: Record<string, unknown>;
}
