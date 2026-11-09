// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const isoDate = /^(\d{4})-(\d{2})-(\d{2})$/;
const isoDateTime = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:?\d{2})?$/;

/**
 * A value of a table cell the way a person reads it, recognised by its runtime type or its ISO format.
 *
 * A boolean reads Yes or No; an ISO date or date-time reads in the user's locale. Anything else - or a text that only looks like a date
 * but is not one - is left as it is. A collection reads as its values joined with a comma when it holds scalars or objects with a single
 * field, and as its item count when its objects carry several fields.
 *
 * @param value The value the row carries.
 * @returns The text to show, or undefined when the value is not one this reads.
 */
export function readableValue(value: unknown): string | undefined {
    if (typeof value === 'boolean') return value ? 'Yes' : 'No';
    if (Array.isArray(value)) return readableCollection(value);
    if (typeof value !== 'string') return undefined;

    return readableDate(value) ?? readableDateTime(value);
}

/** A collection as its values joined with a comma, or its item count when the items carry several fields. */
function readableCollection(items: readonly unknown[]): string {
    if (items.length === 0) return '';

    const values = items.map(singleValue);
    if (values.every(value => value !== undefined)) return values.map(value => readableValue(value) ?? String(value)).join(', ');
    return items.length === 1 ? '1 item' : `${items.length} items`;
}

/** The scalar an item stands for - itself, or the one field of an object - or undefined when it is anything else. */
function singleValue(item: unknown): unknown {
    if (item === null || typeof item !== 'object') return item ?? '';
    if (Array.isArray(item)) return undefined;

    const fields = Object.values(item);
    return fields.length === 1 && (fields[0] === null || typeof fields[0] !== 'object') ? fields[0] ?? '' : undefined;
}

/** An ISO date as the user's locale writes it, or undefined when the text is not one - or names a day the calendar does not have. */
function readableDate(value: string): string | undefined {
    const date = isoDate.exec(value);
    if (!date) return undefined;

    const parsed = new Date(2000, Number(date[2]) - 1, Number(date[3]));
    parsed.setFullYear(Number(date[1]));
    return Number.isNaN(parsed.getTime()) || parsed.getMonth() !== Number(date[2]) - 1 ? undefined : parsed.toLocaleDateString();
}

/** An ISO date-time as the user's locale writes it, or undefined when the text is not one. */
function readableDateTime(value: string): string | undefined {
    if (!isoDateTime.test(value)) return undefined;

    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? undefined : parsed.toLocaleString();
}
