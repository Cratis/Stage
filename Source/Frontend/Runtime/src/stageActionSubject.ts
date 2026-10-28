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

/** What a command form reads of its action's subject: the rows it may use, whether it is a row action and the values bound from the row. */
export interface SubjectForm extends ScreenRows {
    rowAction: boolean;
    subjectValues: Record<string, unknown>;
}

const itemSource = /^item\.(.+)$/;
const emptyValues: unknown[] = [undefined, null, ''];

/** The explicit subject an action element names, or nothing when the subject was omitted. */
export function actionSubjectOf(element: ExternalComponent): ActionSubject | undefined {
    const subject = element.properties.actionSubject;
    return subject === 'item' || subject === 'screen' ? subject : undefined;
}

/** The one selected row an `item` subject acts on; no subject, a `screen` subject and two or more selected rows name none. */
export function subjectRowOf(subject: ActionSubject | undefined, selectedRows: Record<string, unknown>[]): Record<string, unknown> | undefined {
    return subject === 'item' && selectedRows.length === 1 ? selectedRows[0] : undefined;
}

/** Whether an action waits, visible and disabled: a `for item` action without its one selected row. */
export function awaitsSubjectRow(subject: ActionSubject | undefined, row: Record<string, unknown> | undefined): boolean {
    return subject === 'item' && !row;
}

/** What a command form reads: a `screen` subject never reads a selection, and values bound from a row arrive with the opened action. */
export function subjectFormOf(element: ExternalComponent, rows: ScreenRows): SubjectForm {
    const subject = actionSubjectOf(element);
    const values = element.properties.subjectValues;
    return {
        ...(subject === 'screen' ? { selectableRows: [], selectedRows: [] } : rows),
        rowAction: element.properties.rowAction === true || subject === 'item',
        subjectValues: isRecord(values) ? values : {},
    };
}

/** The properties a toolbar item hands the action it becomes: its subject and inputs, and a row action unless the screen is the subject. */
export function subjectActionProperties(element: ExternalComponent, subject: ActionSubject | undefined): Record<string, unknown> {
    return {
        ...(subject === 'screen' ? {} : { rowAction: true }),
        ...(subject === undefined ? {} : { actionSubject: subject, subjectInputs: element.properties.subjectInputs }),
    };
}

/** The command inputs an action binds from the selected row (`with <input> from item.<path>`), by input name. Empty values are dropped. */
export function subjectInputValues(element: ExternalComponent, row: Record<string, unknown>): Record<string, unknown> {
    const values = Object.entries(subjectInputsOf(element)).map(([name, path]) => [name, valueAt(row, path)] as const);
    return Object.fromEntries(values.filter(([, value]) => !emptyValues.includes(value)));
}

/**
 * The action an `item` subject opens: a row action whose bound inputs come from the selected row, and whose form asks for neither those inputs
 * nor a required identifier the row carries. A form with no fields of its own starts from every property of the command, so what remains is asked.
 */
export function itemSubjectElement(element: ExternalComponent, schema: SchemaProperty[], row: Record<string, unknown>): ExternalComponent {
    const bound = new Set(Object.keys(subjectInputsOf(element)));
    const carried = schema.filter(property => property.identifier !== undefined && property.required && Object.hasOwn(row, property.name)).map(property => property.name);
    const supplied = new Set([...bound, ...carried]);
    const fields = askedFields(element, schema).filter(field => !supplied.has(fieldProperty(field)));
    const asked = schema.length > 0 || Array.isArray(element.properties.fields) ? { fields } : {};

    return {
        ...element,
        properties: { ...element.properties, rowAction: true, subjectValues: subjectInputValues(element, row), ...asked },
    } as ExternalComponent;
}

function askedFields(element: ExternalComponent, schema: SchemaProperty[]): Record<string, unknown>[] {
    const own = element.properties.fields;
    return Array.isArray(own) ? own as Record<string, unknown>[] : schema.map(property => ({ name: property.name, label: humanize(property.name) }));
}

function fieldProperty(field: Record<string, unknown>): string {
    const source = field.sourceProperty || field.name;
    return typeof source === 'string' ? source : '';
}

/** The `<path>` of each bound input's `item.<path>` source, keyed by input name, read from the `{ subject: 'item.x' }` shape the scene carries. */
function subjectInputsOf(element: ExternalComponent): Record<string, string> {
    const inputs = element.properties.subjectInputs;
    const paths = (isRecord(inputs) ? Object.entries(inputs) : []).map(([name, binding]) => [name, itemPath(binding)] as const);
    return Object.fromEntries(paths.filter((entry): entry is readonly [string, string] => entry[1] !== undefined));
}

function itemPath(binding: unknown): string | undefined {
    const source = isRecord(binding) ? binding.subject : undefined;
    return typeof source === 'string' ? itemSource.exec(source)?.[1] : undefined;
}

function valueAt(row: Record<string, unknown>, path: string): unknown {
    return path.split('.').reduce<unknown>((current, segment) => isRecord(current) ? current[segment] : undefined, row);
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
}
