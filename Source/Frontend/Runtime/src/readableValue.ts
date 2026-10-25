// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const isoDate = /^(\d{4})-(\d{2})-(\d{2})$/;
const isoDateTime = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:?\d{2})?$/;

/**
 * A value of a table cell the way a person reads it, recognised by its runtime type or its ISO format.
 *
 * A boolean reads Yes or No; an ISO date or date-time reads in the user's locale. Anything else - or a text that only looks like a date
 * but is not one - is left as it is.
 *
 * @param value The value the row carries.
 * @returns The text to show, or undefined when the value is not one this reads.
 */
export function readableValue(value: unknown): string | undefined {
    if (typeof value === 'boolean') return value ? 'Yes' : 'No';
    if (typeof value !== 'string') return undefined;

    const date = isoDate.exec(value);
    if (date) {
        const parsed = new Date(Number(date[1]), Number(date[2]) - 1, Number(date[3]));
        return Number.isNaN(parsed.getTime()) || parsed.getMonth() !== Number(date[2]) - 1 ? undefined : parsed.toLocaleDateString();
    }

    if (isoDateTime.test(value)) {
        const parsed = new Date(value);
        return Number.isNaN(parsed.getTime()) ? undefined : parsed.toLocaleString();
    }

    return undefined;
}
