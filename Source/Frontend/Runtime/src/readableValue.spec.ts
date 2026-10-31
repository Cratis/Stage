// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { readableValue } from './readableValue';

describe('reading a table cell value', () => {
    it.each([[true, 'Yes'], [false, 'No']])('should read the boolean %s as %s', (value, expected) => {
        expect(readableValue(value)).toEqual(expected);
    });

    it('should read an ISO date in the locale of the user', () => {
        expect(readableValue('2026-03-15')).toEqual(new Date(2026, 2, 15).toLocaleDateString());
    });

    it('should read an ISO date-time in the locale of the user', () => {
        expect(readableValue('2026-03-15T10:30:00Z')).toEqual(new Date('2026-03-15T10:30:00Z').toLocaleString());
    });

    it.each(['Alpha', '2026-13-45', '2026-02-31', 'Due 2026-03-15', '', 42, null, undefined])('should leave %s as it is', value => {
        expect(readableValue(value)).toBeUndefined();
    });
});
