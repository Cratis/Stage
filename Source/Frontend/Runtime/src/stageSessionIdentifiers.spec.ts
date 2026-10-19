// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, describe, expect, it } from 'vitest';
import { schemaPropertiesOf, sessionIdentifiers } from './StageCommandForm';

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

    it('should leave the identifier to the screen when it carries it as a parameter', () => {
        expect(sessionIdentifiers(fields as never, schemaPropertiesOf(schema), { basketId: 'b1' })).toEqual({});
    });

    it('should leave an identifier the form shows to the person filling it', () => {
        const shown = [{ name: 'basketId', label: 'Basket' }, ...fields];

        expect(sessionIdentifiers(shown as never, schemaPropertiesOf(schema), {})).toEqual({});
    });

    it('should not supply a value for a property that is not an identifier', () => {
        expect(Object.keys(sessionIdentifiers([] as never, schemaPropertiesOf(schema), {}))).toEqual(['basketId']);
    });
});
