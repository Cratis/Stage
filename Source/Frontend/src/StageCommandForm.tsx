// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useState } from 'react';
import type { ExternalComponent, FormField } from '@cratis/scene.model';
import { Command } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { CommandForm as NativeCommandForm } from '@cratis/arc.react/commands';
import { CheckboxField, InputTextField, NumberField, CalendarField } from '@cratis/components/CommandForm';
import { useStageData } from './stageData';

interface SchemaProperty {
    name: string;
    type: 'text' | 'number' | 'boolean' | 'date' | 'object';
    required: boolean;
}

interface NativeFormProperties {
    command: string;
    label: string;
    fields: FormField[];
    route?: string;
    mode: 'auto' | 'manual';
    columns: string[][];
}

interface StageCommandFormProps {
    element: ExternalComponent;
}

interface StageCommandContent {
    [key: string]: unknown;
}

type StageCommandConstructor = new () => Command<StageCommandContent, object>;

/** Renders an authored screen form through Cratis Arc's native CommandForm boundary. */
export function StageCommandForm({ element }: StageCommandFormProps) {
    const data = useStageData();
    const form = formProperties(element);
    const schema = schemaProperties(element);
    const [messages, setMessages] = useState<string[]>([]);

    if (!form) return <p className='stage-note'>This form is missing its command metadata.</p>;

    const route = form.route ?? data.routes?.commands[form.command];
    if (!route) return <p className='stage-note'>The modeled command “{form.command}” is not registered by this Stage.</p>;

    const fields = fieldsFor(form, schema);
    const commandType = useMemo(() => commandTypeFor(route, fields, schema), [route, fields, schema]);
    const renderedFields = renderFields(form, fields, schema);

    return (
        <section className='stage-command-form' data-command={form.command}>
            <NativeCommandForm
                command={commandType}
                validateOn='both'
                validateAllFieldsOnChange
                onFieldValidate={(_command, fieldName, _oldValue, newValue) => requiredMessage(fields, schema, fieldName, newValue)}
                onValidationFailure={validation => setMessages(validation.map(_ => _.message))}
                onException={exceptionMessages => setMessages(exceptionMessages)}
                onFailed={result => setMessages(messagesFromResult(result))}
                onSuccess={() => {
                    setMessages([]);
                    data.refreshQuery();
                }}>
                {renderedFields}
                {messages.map(message => <p key={message} role='alert' className='stage-form-error'>{message}</p>)}
                <button type='submit'>Execute {form.label}</button>
            </NativeCommandForm>
        </section>
    );
}

function formProperties(element: ExternalComponent): NativeFormProperties | undefined {
    const command = text(element, 'command');
    if (!command) return undefined;
    const fields = Array.isArray(element.properties.fields) ? element.properties.fields.filter(isRecord) as unknown as FormField[] : [];
    const columns = Array.isArray(element.properties.columns) ? element.properties.columns.map(column => Array.isArray(column) ? column.filter(_ => typeof _ === 'string') : []) : [];
    const mode = element.properties.mode === 'manual' ? 'manual' : 'auto';
    return { command, label: text(element, 'label', command), fields, route: text(element, 'route') || undefined, mode, columns };
}

function fieldsFor(form: NativeFormProperties, schema: SchemaProperty[]): FormField[] {
    if (form.mode === 'manual' || form.fields.length > 0) return form.fields;
    return schema.map(property => ({ name: property.name, label: property.name }));
}

function renderFields(form: NativeFormProperties, fields: FormField[], schema: SchemaProperty[]) {
    const byName = new Map(fields.map(field => [field.name, field]));
    if (form.columns.length === 0) return fields.map(field => renderField(field, schema));

    const rendered = new Set<string>();
    return form.columns.map((column, index) => (
        <NativeCommandForm.Column key={index}>
            {column.map(name => {
                const field = byName.get(name);
                if (!field) return null;
                rendered.add(field.name);
                return renderField(field, schema);
            })}
            {index === form.columns.length - 1 && fields.filter(field => !rendered.has(field.name)).map(field => renderField(field, schema))}
        </NativeCommandForm.Column>
    ));
}

function renderField(field: FormField, schema: SchemaProperty[]) {
    const property = field.sourceProperty ?? field.name;
    const descriptor = schema.find(_ => _.name === property);
    const title = field.label ?? field.name;
    const accessor = (command: unknown) => (command as Record<string, unknown>)[property];

    switch (descriptor?.type ?? 'text') {
        case 'number': return <NumberField key={field.name} value={accessor} fieldName={property} title={title} required={descriptor?.required ?? true} />;
        case 'boolean': return <CheckboxField key={field.name} value={accessor} fieldName={property} label={title} required={descriptor?.required ?? true} />;
        case 'date': return <CalendarField key={field.name} value={accessor} fieldName={property} title={title} required={descriptor?.required ?? true} showIcon />;
        default: return <InputTextField key={field.name} value={accessor} fieldName={property} title={title} required={descriptor?.required ?? true} />;
    }
}

function commandTypeFor(route: string, fields: FormField[], schema: SchemaProperty[]): StageCommandConstructor {
    const descriptors = fields.map(field => {
        const property = field.sourceProperty ?? field.name;
        const schemaProperty = schema.find(_ => _.name === property);
        return new PropertyDescriptor(property, constructorFor(schemaProperty?.type), !(schemaProperty?.required ?? true));
    });

    return class StageNativeCommand extends Command<StageCommandContent, object> {
        readonly route = route.replace(/^\//, '');
        readonly propertyDescriptors = descriptors;
        get requestParameters(): string[] { return []; }

        constructor() {
            super(Object, false);
            for (const descriptor of descriptors) {
                Object.defineProperty(this, descriptor.name, {
                    configurable: true,
                    enumerable: true,
                    get: () => (this as unknown as { _values: StageCommandContent })._values?.[descriptor.name],
                    set: value => {
                        const target = this as unknown as { _values: StageCommandContent };
                        target._values = { ...(target._values ?? {}), [descriptor.name]: value };
                        this.propertyChanged(descriptor.name);
                    },
                });
            }
        }
    };
}

function constructorFor(type: SchemaProperty['type'] | undefined) {
    switch (type) {
        case 'number': return Number;
        case 'boolean': return Boolean;
        case 'date': return Date;
        case 'object': return Object;
        default: return String;
    }
}

function requiredMessage(fields: FormField[], schema: SchemaProperty[], fieldName: string, value: unknown): string | undefined {
    const field = fields.find(_ => (_.sourceProperty ?? _.name) === fieldName);
    const property = schema.find(_ => _.name === fieldName);
    if (!(property?.required ?? true)) return undefined;
    if (value === undefined || value === null || value === '') return `${field?.label ?? fieldName} is required`;
    return undefined;
}

function messagesFromResult(result: { validationResults: { message: string }[]; exceptionMessages: string[] }): string[] {
    return [
        ...result.validationResults.map(_ => _.message),
        ...result.exceptionMessages,
    ];
}

function schemaProperties(element: ExternalComponent): SchemaProperty[] {
    const schema = text(element, 'schema');
    if (!schema) return [];
    try {
        const parsed = JSON.parse(schema) as { required?: string[]; properties?: Record<string, { type?: string; format?: string }> };
        const required = new Set(parsed.required ?? []);
        return Object.entries(parsed.properties ?? {}).map(([name, definition]) => ({
            name,
            type: propertyType(definition),
            required: required.size === 0 || required.has(name),
        }));
    } catch {
        return [];
    }
}

function propertyType(definition: { type?: string; format?: string } | undefined): SchemaProperty['type'] {
    if (definition?.type === 'integer' || definition?.type === 'number') return 'number';
    if (definition?.type === 'boolean') return 'boolean';
    if (definition?.format === 'date' || definition?.format === 'date-time') return 'date';
    if (definition?.type === 'object') return 'object';
    return 'text';
}

function text(element: ExternalComponent, name: string, fallback = ''): string {
    const value = element.properties[name];
    return typeof value === 'string' ? value : fallback;
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
}
