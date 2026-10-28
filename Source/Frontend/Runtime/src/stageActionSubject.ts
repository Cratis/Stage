// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ExternalComponent } from '@cratis/scene.model';
import { humanize, type SchemaProperty } from './stageFormSchema';

/**
 * The explicit subject of an action (Screenplay decision 0110, Cratis/Stage#855): `for item` acts on the one selected row, `for screen` on the
 * screen itself. Stage carries it as the scene properties `actionSubject` and `subjectInputs` until Scene ships a typed `ActionSubject`. An
 * omitted subject is `undefined` and keeps every existing behavior.
 */
export type ActionSubject = 'item' | 'screen';

/** The rows a screen offers for selecting and the ones that are selected. */
export interface ScreenRows {
    selectableRows: Record<string, unknown>[];
    selectedRows: Record<string, unknown>[];
}

const itemPrefix = 'item.';

/** The explicit subject an action element names, or nothing when the subject was omitted. */
export function actionSubjectOf(element: ExternalComponent): ActionSubject | undefined {
    const subject = element.properties.actionSubject;
    return subject === 'item' || subject === 'screen' ? subject : undefined;
}

/** The one selected row an `item` subject acts on; two or more selected rows are ambiguous, so they name none. */
export function subjectRow(selectedRows: Record<string, unknown>[]): Record<string, unknown> | undefined {
    return selectedRows.length === 1 ? selectedRows[0] : undefined;
}

/** The rows an action reads: a `screen` subject never reads a selection, every other action reads the screen's rows as they are. */
export function rowsForSubject(subject: ActionSubject | undefined, rows: ScreenRows): ScreenRows {
    return subject === 'screen' ? { selectableRows: [], selectedRows: [] } : rows;
}

/** The command inputs an action binds from the selected row (`with <input> from item.<path>`), by input name. Empty values are dropped. */
export function subjectInputValues(element: ExternalComponent, row: Record<string, unknown>): Record<string, unknown> {
    const values: Record<string, unknown> = {};
    for (const [name, source] of Object.entries(subjectInputsOf(element))) {
        const value = valueAt(row, source);
        if (value !== undefined && value !== null && value !== '') values[name] = value;
    }

    return values;
}

/**
 * The action an `item` subject opens: a row action whose bound inputs come from the selected row, and whose form asks for neither those inputs
 * nor a required identifier the row carries. A form with no fields of its own starts from every property of the command, so what remains is asked.
 */
export function itemSubjectElement(element: ExternalComponent, schema: SchemaProperty[], row: Record<string, unknown>): ExternalComponent {
    const bound = new Set(Object.keys(subjectInputsOf(element)));
    const carried = new Set(schema.filter(property => property.identifier !== undefined && property.required && Object.hasOwn(row, property.name)).map(property => property.name));
    const isSupplied = (name: string) => bound.has(name) || carried.has(name);
    const own = Array.isArray(element.properties.fields) ? element.properties.fields as Record<string, unknown>[] : undefined;
    const fields = own
        ? own.filter(field => !isSupplied(fieldProperty(field)))
        : schema.filter(property => !isSupplied(property.name)).map(property => ({ name: property.name, label: humanize(property.name) }));
    const asked = own || schema.length > 0 ? { fields } : {};

    return {
        ...element,
        properties: { ...element.properties, rowAction: true, subjectValues: subjectInputValues(element, row), ...asked },
    } as ExternalComponent;
}

function fieldProperty(field: Record<string, unknown>): string {
    if (typeof field.sourceProperty === 'string' && field.sourceProperty !== '') return field.sourceProperty;
    return typeof field.name === 'string' ? field.name : '';
}

/** The `item.<path>` source of each bound input, keyed by input name, read from the `{ subject: 'item.x' }` shape the scene carries. */
function subjectInputsOf(element: ExternalComponent): Record<string, string> {
    const inputs = element.properties.subjectInputs;
    if (inputs === null || typeof inputs !== 'object' || Array.isArray(inputs)) return {};

    const sources: Record<string, string> = {};
    for (const [name, binding] of Object.entries(inputs as Record<string, unknown>)) {
        const source = binding !== null && typeof binding === 'object' ? (binding as Record<string, unknown>).subject ?? (binding as Record<string, unknown>).path : undefined;
        if (typeof source === 'string' && source.startsWith(itemPrefix) && source.length > itemPrefix.length) sources[name] = source.slice(itemPrefix.length);
    }

    return sources;
}

function valueAt(row: Record<string, unknown>, path: string): unknown {
    let current: unknown = row;
    for (const segment of path.split('.')) {
        if (current === null || typeof current !== 'object') return undefined;
        current = (current as Record<string, unknown>)[segment];
    }

    return current;
}
