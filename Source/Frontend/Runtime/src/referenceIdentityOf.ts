// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** A scalar reference key, retaining its primitive type and treating zero as present. */
export function referenceIdentityOf(value: unknown): string | number | undefined {
    if (typeof value === 'number') return Number.isFinite(value) ? value : undefined;
    return typeof value === 'string' && value !== '' ? value : undefined;
}
