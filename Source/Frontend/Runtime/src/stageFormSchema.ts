// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * What a command's presentation schema says about one property, in the terms a form needs: its scalar type, the
 * values an enumeration declares, the members of a record and the element of a collection.
 */
export interface SchemaProperty {
    name: string;

    /** The identifier concept of a uuid property (`x-concept`) - `BasketId`; what a session holds one value of. */
    identifier?: string;

    /**
     * Whether this is the identifier the command acts on (`x-identity`) - the basket an add to basket appends to - rather than a reference to
     * another entity, such as the product it adds. Only the schema a task dialog is built from says so.
     */
    identity?: boolean;
    type?: 'text' | 'number' | 'boolean' | 'date';
    required: boolean;
    unsupportedReason?: string;

    /** The values an enumeration declares, in declaration order; the property is presented as a choice among them. */
    options?: string[];

    /** The members of a record (`PostalAddress`); the property is presented as a group of fields. */
    members?: SchemaProperty[];

    /** The element of a collection (`OrderLine[]`); the property is presented as a list of them. */
    item?: SchemaProperty;

    /** Whether a date also carries a time of day. */
    withTime?: boolean;
}

interface JsonSchemaDefinition {
    type?: unknown;
    format?: unknown;
    enum?: unknown;
    items?: unknown;
    properties?: unknown;
    required?: unknown;
    'x-concept'?: unknown;
    'x-identity'?: unknown;
}

const maximumDepth = 8;

/** Whether the command creates an entity whose identity the runtime allocates, rather than acting on a selected row. */
export function createsEntity(schema: string): boolean {
    if (!schema) return false;
    try {
        const parsed: unknown = JSON.parse(schema);
        return isRecord(parsed) && parsed['x-creates'] === true;
    } catch {
        return false;
    }
}

/**
 * The properties a command's JSON schema declares, with the type the native form gives each and whether it is required. A toolbar
 * slice's task dialog is built from the same reading, so a command is the same fields wherever it is presented.
 */
export function schemaPropertiesOf(schema: string): SchemaProperty[] {
    if (!schema) return [];
    try {
        return membersOf(JSON.parse(schema) as JsonSchemaDefinition, 0);
    } catch {
        return [];
    }
}

function membersOf(parsed: JsonSchemaDefinition, depth: number): SchemaProperty[] {
    const hasRequired = Object.prototype.hasOwnProperty.call(parsed, 'required');
    const required = Array.isArray(parsed.required) ? new Set(parsed.required.filter((name): name is string => typeof name === 'string')) : new Set<string>();
    const properties = isRecord(parsed.properties) ? parsed.properties : {};
    return Object.entries(properties).map(([name, definition]) => describe(name, definition as JsonSchemaDefinition | undefined, hasRequired ? required.has(name) : true, depth));
}

function describe(name: string, definition: JsonSchemaDefinition | undefined, required: boolean, depth: number): SchemaProperty {
    return {
        name,
        identifier: definition?.format === 'uuid' && typeof definition['x-concept'] === 'string' ? definition['x-concept'] : undefined,
        ...(definition?.['x-identity'] === true ? { identity: true } : {}),
        required,
        ...propertyType(definition, depth),
    };
}

function propertyType(definition: JsonSchemaDefinition | undefined, depth: number): Partial<SchemaProperty> {
    const schemaType = definition?.type;
    if (Array.isArray(schemaType)) {
        const nonNullTypes = schemaType.filter(type => type !== 'null');
        if (nonNullTypes.length === 1) return propertyType({ ...definition, type: nonNullTypes[0] }, depth);
        return { unsupportedReason: `union type ${JSON.stringify(schemaType)}` };
    }

    if (schemaType === 'integer' || schemaType === 'number') return { type: 'number' };
    if (schemaType === 'boolean') return { type: 'boolean' };
    if (definition?.format === 'date' || definition?.format === 'date-time') return { type: 'date', withTime: definition.format === 'date-time' };
    if (schemaType === 'array') return collectionType(definition, depth);
    if (schemaType === 'object') return recordType(definition, depth);
    if (schemaType === undefined || schemaType === 'string') {
        const options = Array.isArray(definition?.enum) ? definition.enum.filter((value): value is string => typeof value === 'string') : [];
        return options.length > 0 ? { type: 'text', options } : { type: 'text' };
    }

    return { unsupportedReason: `type ${String(schemaType)}` };
}

function collectionType(definition: JsonSchemaDefinition | undefined, depth: number): Partial<SchemaProperty> {
    const unsupported = { unsupportedReason: 'array fields are not supported by the native Stage command form' };
    if (depth >= maximumDepth || definition?.items === undefined) return unsupported;

    const item = describe('', definition.items as JsonSchemaDefinition, true, depth + 1);
    if (item.unsupportedReason || item.item) return unsupported;
    return { item };
}

function recordType(definition: JsonSchemaDefinition | undefined, depth: number): Partial<SchemaProperty> {
    const unsupported = { unsupportedReason: 'object fields are not supported by the native Stage command form' };
    if (depth >= maximumDepth || !isRecord(definition?.properties) || Object.keys(definition.properties).length === 0) return unsupported;

    const members = membersOf(definition, depth + 1);
    const failed = members.find(member => member.unsupportedReason);
    if (failed) return { unsupportedReason: `its member “${failed.name}” is not supported: ${failed.unsupportedReason}` };
    return { members };
}

/** Humanizes a member name the way a field label reads: `postalCode` is “Postal code”. */
export function humanize(name: string): string {
    const words = name.replace(/([a-z0-9])([A-Z])/g, '$1 $2').toLowerCase();
    return words.charAt(0).toUpperCase() + words.slice(1);
}

/**
 * What is missing from a record or collection a form is about to submit: a member the schema requires that holds nothing.
 *
 * @param property The property the value is for.
 * @param value What the form holds for it.
 * @param label How the property reads on the form.
 * @returns The first message, or nothing when the value is complete.
 */
export function missingMember(property: SchemaProperty, value: unknown, label: string): string | undefined {
    if (value === undefined || value === null) return undefined;

    if (property.members && isRecord(value)) {
        for (const member of property.members) {
            const memberValue = value[member.name];
            const memberLabel = humanize(member.name);
            if (isBlank(memberValue)) {
                if (member.required) return `${label}: ${memberLabel} is required`;
                continue;
            }

            const nested = missingMember(member, memberValue, `${label} ${memberLabel.toLowerCase()}`);
            if (nested) return nested;
        }
    }

    if (property.item && Array.isArray(value)) {
        for (const [index, element] of value.entries()) {
            const message = isBlank(element) ? `${label} ${index + 1} is required` : missingMember(property.item, element, `${label} ${index + 1}`);
            if (message) return message;
        }
    }

    return undefined;
}

function isBlank(value: unknown): boolean {
    return value === undefined || value === null || value === '';
}

/**
 * Brings a value read from the application to the form a command takes it in: an enumeration is the name of its member, however the read
 * model stored it (by name, by case, or by its position), and a record or a collection is the same all the way down.
 *
 * @param property The property the value is for.
 * @param value What a query answered for it.
 * @returns The value as the command takes it.
 */
export function asCommandValue(property: SchemaProperty, value: unknown): unknown {
    if (value === undefined || value === null) return value;

    if (property.options) {
        if (typeof value === 'number') return property.options[value] ?? value;
        if (typeof value === 'string') return property.options.find(option => option.toLowerCase() === value.toLowerCase()) ?? value;
        return value;
    }

    if (property.members && isRecord(value)) {
        return Object.fromEntries(Object.entries(value).map(([name, member]) => {
            const described = property.members?.find(candidate => candidate.name === name);
            return [name, described ? asCommandValue(described, member) : member];
        }));
    }

    if (property.item && Array.isArray(value)) return value.map(element => asCommandValue(property.item as SchemaProperty, element));

    return value;
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
}
