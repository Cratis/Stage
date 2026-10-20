// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, describe, expect, it } from 'vitest';
import { schemaPropertiesOf, selectedIdentifier, sessionIdentifiers } from './StageCommandForm';

const schema = JSON.stringify({
    type: 'object',
    properties: {
        basketId: { type: 'string', format: 'uuid', 'x-concept': 'BasketId' },
        sku: { type: 'string', 'x-concept': 'Sku' },
        quantity: { type: 'integer' },
    },
    required: ['basketId', 'sku', 'quantity'],
});

describe('when a form does not show the identifier its command is about', () => {
    afterEach(() => globalThis.localStorage?.clear());

    const fields = [{ name: 'sku', label: 'Product' }, { name: 'quantity', label: 'How many?' }];

    it('should supply one identity for the identifier concept', () => {
        const values = sessionIdentifiers(fields as never, schemaPropertiesOf(schema), {});

        expect(values.basketId).toMatch(/^[0-9a-f-]{36}$/);
    });

    it('should supply the same identity every time in the same session', () => {
        const first = sessionIdentifiers(fields as never, schemaPropertiesOf(schema), {}).basketId;
        const second = sessionIdentifiers(fields as never, schemaPropertiesOf(schema), {}).basketId;

        expect(second).toBe(first);
    });

    it('should take the identifier the screen carries as a parameter', () => {
        expect(sessionIdentifiers(fields as never, schemaPropertiesOf(schema), { basketId: 'b1' })).toEqual({ basketId: 'b1' });
    });

    it('should leave an identifier the form shows to the person filling it', () => {
        const shown = [{ name: 'basketId', label: 'Basket' }, ...fields];

        expect(sessionIdentifiers(shown as never, schemaPropertiesOf(schema), {})).toEqual({});
    });

    it('should not supply a value for a property that is not an identifier', () => {
        expect(Object.keys(sessionIdentifiers([] as never, schemaPropertiesOf(schema), {}))).toEqual(['basketId']);
    });
});

describe('when a row is selected on the screen', () => {
    afterEach(() => globalThis.localStorage?.clear());

    const fields = [{ name: 'sku', label: 'Product' }, { name: 'quantity', label: 'How many?' }];

    it('should act on the selected row instead of an invented identity', () => {
        expect(sessionIdentifiers(fields as never, schemaPropertiesOf(schema), {}, [{ basketId: 'row-1', sku: 'A' }])).toEqual({ basketId: 'row-1' });
    });

    it('should prefer the address parameter to the selected row', () => {
        expect(sessionIdentifiers(fields as never, schemaPropertiesOf(schema), { basketId: 'b1' }, [{ basketId: 'row-1' }])).toEqual({ basketId: 'b1' });
    });

    it('should not choose between selected rows that disagree', () => {
        const property = schemaPropertiesOf(schema).find(item => item.name === 'basketId')!;

        expect(selectedIdentifier(property, [{ basketId: 'row-1' }, { basketId: 'row-2' }])).toBeUndefined();
    });

    it('should ignore a selected row that has no value for the identifier', () => {
        const property = schemaPropertiesOf(schema).find(item => item.name === 'basketId')!;

        expect(selectedIdentifier(property, [{ sku: 'A' }])).toBeUndefined();
    });
});

describe('when a row the person could select carries the identifier the form does not show', () => {
    afterEach(() => globalThis.localStorage?.clear());

    const fields = [{ name: 'sku', label: 'Product' }, { name: 'quantity', label: 'How many?' }];
    const selectable = [{ basketId: 'b1' }, { basketId: 'b2' }];

    it('should not invent the identifier while no row is selected', () => {
        expect(sessionIdentifiers(fields as never, schemaPropertiesOf(schema), {}, [], selectable)).toEqual({});
    });

    it('should take the selected row\'s identifier', () => {
        expect(sessionIdentifiers(fields as never, schemaPropertiesOf(schema), {}, [selectable[1]], selectable)).toEqual({ basketId: 'b2' });
    });

    it('should still create the identifier of a new entity no row carries', () => {
        expect(sessionIdentifiers(fields as never, schemaPropertiesOf(schema), {}, [], [{ orderId: 'o1' }]).basketId).toMatch(/^[0-9a-f-]{36}$/);
    });

    it('should take the selected row instead of the identity the session already holds', () => {
        const held = sessionIdentifiers(fields as never, schemaPropertiesOf(schema), {}).basketId;
        expect(held).toMatch(/^[0-9a-f-]{36}$/);
        expect(sessionIdentifiers(fields as never, schemaPropertiesOf(schema), {}, [selectable[1]], selectable)).toEqual({ basketId: 'b2' });
    });
});

describe('when a task dialog acts on an identity that a row on the screen also carries', () => {
    afterEach(() => globalThis.localStorage?.clear());

    const identitySchema = JSON.stringify({
        type: 'object',
        properties: {
            basketId: { type: 'string', format: 'uuid', 'x-concept': 'BasketId', 'x-identity': true },
            sku: { type: 'string', 'x-concept': 'Sku' },
        },
        required: ['basketId', 'sku'],
    });
    const fields = [{ name: 'sku', label: 'Product' }];

    it('should still create the identity in a fresh session', () => {
        expect(sessionIdentifiers(fields as never, schemaPropertiesOf(identitySchema), {}, [], [{ basketId: 'b1' }]).basketId).toMatch(/^[0-9a-f-]{36}$/);
    });
});

describe('when an action on a list acts on an identity its rows carry', () => {
    afterEach(() => globalThis.localStorage?.clear());

    const identitySchema = JSON.stringify({
        type: 'object',
        properties: {
            shipmentId: { type: 'string', format: 'uuid', 'x-concept': 'ShipmentId', 'x-identity': true },
            carrier: { type: 'string' },
        },
        required: ['shipmentId', 'carrier'],
    });
    const fields = [{ name: 'carrier', label: 'Carrier' }];
    const selectable = [{ shipmentId: 's1' }, { shipmentId: 's2' }];

    it('should neither create nor take the held identity while no row is selected', () => {
        sessionIdentifiers(fields as never, schemaPropertiesOf(identitySchema), {});
        expect(sessionIdentifiers(fields as never, schemaPropertiesOf(identitySchema), {}, [], selectable, true)).toEqual({});
    });

    it('should take the selected row\'s identity', () => {
        sessionIdentifiers(fields as never, schemaPropertiesOf(identitySchema), {});
        expect(sessionIdentifiers(fields as never, schemaPropertiesOf(identitySchema), {}, [selectable[1]], selectable, true)).toEqual({ shipmentId: 's2' });
    });
});
