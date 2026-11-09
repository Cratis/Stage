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

    it('should read a date in the first century in that century', () => {
        const expected = new Date(2000, 0, 5);
        expected.setFullYear(1);
        expect(readableValue('0001-01-05')).toEqual(expected.toLocaleDateString());
    });

    it('should read an ISO date-time in the locale of the user', () => {
        expect(readableValue('2026-03-15T10:30:00Z')).toEqual(new Date('2026-03-15T10:30:00Z').toLocaleString());
    });

    it('should join the one field of each child of a collection', () => {
        expect(readableValue([{ label: 'probe' }, { label: 'urgent' }])).toEqual('probe, urgent');
    });

    it('should join the values of a collection of scalars', () => {
        expect(readableValue(['probe', 'urgent'])).toEqual('probe, urgent');
    });

    it('should read the item count of children with several fields', () => {
        expect(readableValue([{ label: 'probe', color: 'red' }, { label: 'urgent', color: 'blue' }])).toEqual('2 items');
    });

    it('should read a single child with several fields as one item', () => {
        expect(readableValue([{ label: 'probe', color: 'red' }])).toEqual('1 item');
    });

    it('should read an empty collection as an empty cell', () => {
        expect(readableValue([])).toEqual('');
    });

    it.each(['Alpha', '2026-13-45', '2026-02-31', 'Due 2026-03-15', '', 42, null, undefined])('should leave %s as it is', value => {
        expect(readableValue(value)).toBeUndefined();
    });
});
