// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { schemaPropertiesOf } from './StageCommandForm';

const schema = JSON.stringify({
    type: 'object',
    properties: {
        method: { type: 'string', enum: ['standard', 'express'], 'x-concept': 'DeliveryMethod' },
        shipTo: {
            type: 'object',
            properties: { street: { type: 'string' }, postalCode: { type: 'string' }, floor: { type: 'integer' } },
            required: ['street', 'postalCode'],
        },
        lines: { type: 'array', items: { type: 'object' } },
        blob: { type: 'object' },
    },
    required: ['method', 'shipTo', 'lines', 'blob'],
});

describe('when a command schema has an enumeration, a composite and a collection', () => {
    const properties = schemaPropertiesOf(schema);
    const named = (name: string) => properties.find(property => property.name === name)!;

    it('should offer the values of the enumeration', () => {
        expect(named('method').options).toEqual(['standard', 'express']);
        expect(named('method').unsupportedReason).toBeUndefined();
    });

    it('should carry the members of the composite with what each requires', () => {
        expect(named('shipTo').members?.map(member => [member.name, member.required, member.type])).toEqual([['street', true, 'text'], ['postalCode', true, 'text'], ['floor', false, 'number']]);
    });

    it('should leave a collection to the screen that supplies it', () => {
        expect(named('lines').unsupportedReason).toContain('array');
        expect(named('lines').members).toBeUndefined();
    });

    it('should leave a composite that declares no members unsupported', () => {
        expect(named('blob').unsupportedReason).toContain('object');
        expect(named('blob').members).toBeUndefined();
    });
});
