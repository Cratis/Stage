// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ChangeSet } from '@cratis/arc/queries';

function identity(item: unknown): unknown {
    return item !== null && typeof item === 'object' && 'id' in item ? item.id : undefined;
}

function identityValue(value: unknown): unknown {
    if (value === null || typeof value !== 'object') return value;
    const text = value.toString();
    return text === '[object Object]' ? JSON.stringify(value) : text;
}

function identitiesEqual(left: unknown, right: unknown): boolean {
    if (left === right) return true;
    if (left === null || left === undefined || right === null || right === undefined) return false;
    if (typeof left === 'object' && 'equals' in left && typeof left.equals === 'function') return left.equals(right);
    if (typeof right === 'object' && 'equals' in right && typeof right.equals === 'function') return right.equals(left);
    return identityValue(left) === identityValue(right);
}

/** Mirrors Arc React's collection delta application; Arc QueryResult has already deserialized each item. */
export function applyQueryChangeSet(previous: unknown[], changeSet: ChangeSet<unknown>): unknown[] {
    const useIdentity = changeSet.removed.length > 0 ? identity(changeSet.removed[0]) !== undefined : changeSet.replaced.length > 0;
    if (useIdentity) {
        const retained = previous.filter(item => !changeSet.removed.some(removed => identitiesEqual(identity(item), identity(removed))));
        return [...retained.map(item => changeSet.replaced.find(replacement => identitiesEqual(identity(replacement), identity(item))) ?? item), ...changeSet.added];
    }
    const removed = new Set(changeSet.removed.map(item => JSON.stringify(item)));
    return [...previous.filter(item => !removed.has(JSON.stringify(item))), ...changeSet.added];
}
