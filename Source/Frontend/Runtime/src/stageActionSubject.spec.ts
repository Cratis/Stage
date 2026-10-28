// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { actionSubjectOf, itemSubjectElement, rowsForSubject, subjectInputValues, subjectRow } from './stageActionSubject';
import { element } from './testElements';
import { schemaPropertiesOf } from './stageFormSchema';

const schema = schemaPropertiesOf(JSON.stringify({
    type: 'object',
    required: ['shipmentId', 'carrier'],
    properties: { shipmentId: { type: 'string', format: 'uuid', 'x-concept': 'ShipmentId' }, carrier: { type: 'string' }, note: { type: 'string' } },
}));
const action = (properties: Record<string, unknown> = {}) => element('a', 'core:action', { command: 'Dispatch', ...properties });

describe('the subject an action names', () => {
    it('is read from the action, and is nothing when omitted', () => {
        expect(actionSubjectOf(action({ actionSubject: 'item' }))).toBe('item');
        expect(actionSubjectOf(action({ actionSubject: 'screen' }))).toBe('screen');
        expect(actionSubjectOf(action())).toBeUndefined();
    });
});

describe('the row an item subject acts on', () => {
    it('is the only selected row', () => expect(subjectRow([{ id: 1 }])).toEqual({ id: 1 }));
    it('is nothing with no selected row', () => expect(subjectRow([])).toBeUndefined());
    it('is nothing with two selected rows', () => expect(subjectRow([{ id: 1 }, { id: 2 }])).toBeUndefined());
});

describe('the rows a subject reads', () => {
    const rows = { selectableRows: [{ id: 1 }], selectedRows: [{ id: 1 }] };
    it('are none for a screen subject', () => expect(rowsForSubject('screen', rows)).toEqual({ selectableRows: [], selectedRows: [] }));
    it('are the screen\'s rows for an item subject', () => expect(rowsForSubject('item', rows)).toBe(rows));
    it('are the screen\'s rows for an omitted subject', () => expect(rowsForSubject(undefined, rows)).toBe(rows));
});

describe('the inputs bound from the selected row', () => {
    const inputs = { subjectInputs: { id: { subject: 'item.rowId' }, city: { subject: 'item.address.city' }, gone: { subject: 'item.address.zip' }, blank: { subject: 'item.empty' } } };
    const row = { rowId: 'r1', address: { city: 'Oslo' }, empty: '' };

    it('are read by path, including a nested one', () => expect(subjectInputValues(action(inputs), row)).toEqual({ id: 'r1', city: 'Oslo' }));
    it('are nothing without bound inputs', () => expect(subjectInputValues(action(), row)).toEqual({}));
});

describe('the action an item subject opens', () => {
    it('does not ask for a bound input or for the identifier the row carries', () => {
        const opened = itemSubjectElement(action({ actionSubject: 'item', subjectInputs: { note: { subject: 'item.text' } } }), schema, { shipmentId: 's1', text: 'hi' });

        expect(opened.properties.rowAction).toBe(true);
        expect(opened.properties.subjectValues).toEqual({ note: 'hi' });
        expect((opened.properties.fields as { name: string }[]).map(field => field.name)).toEqual(['carrier']);
    });

    it('keeps asking for an identifier the row does not carry', () => {
        const opened = itemSubjectElement(action({ actionSubject: 'item' }), schema, { other: 1 });

        expect((opened.properties.fields as { name: string }[]).map(field => field.name)).toEqual(['shipmentId', 'carrier', 'note']);
    });

    it('narrows the fields the form already has', () => {
        const fields = [{ name: 'shipmentId', label: 'Shipment' }, { name: 'carrier', label: 'Carrier' }];
        const opened = itemSubjectElement(action({ actionSubject: 'item', fields }), schema, { shipmentId: 's1' });

        expect((opened.properties.fields as { name: string }[]).map(field => field.name)).toEqual(['carrier']);
    });
});
