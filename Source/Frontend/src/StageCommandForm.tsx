// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useState } from 'react';
import type { CSSProperties, ReactNode } from 'react';
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

enum FormFieldGenerationMode {
    Auto = 'auto',
    Manual = 'manual',
}

enum FormLayoutMode {
    Auto = 'auto',
    Manual = 'manual',
}

interface FormFieldPlacement {
    name: string;
    width?: string;
    minWidth?: string;
    maxWidth?: string;
    grow?: number;
}

interface FormColumnGeometry {
    fields: FormFieldPlacement[];
    width?: string;
    minWidth?: string;
    maxWidth?: string;
    grow?: number;
    resizable: boolean;
}

interface FormLayoutGeometry {
    mode: FormLayoutMode;
    columns: FormColumnGeometry[];
}

interface NativeFormProperties {
    command: string;
    label: string;
    fields: FormField[];
    route?: string;
    fieldGenerationMode: FormFieldGenerationMode;
    layout: FormLayoutGeometry;
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
    const form = useMemo(() => formProperties(element), [element]);
    const schema = useMemo(() => schemaProperties(element), [element]);
    const fields = useMemo(() => form ? fieldsFor(form, schema) : [], [form, schema]);
    const route = form?.route ?? (form ? data.routes?.commands[form.command] : undefined);
    const commandType = useMemo(() => route ? commandTypeFor(route, fields, schema) : undefined, [route, fields, schema]);
    const renderedFields = useMemo(() => form ? renderFields(form, fields, schema) : undefined, [form, fields, schema]);
    const [messages, setMessages] = useState<string[]>([]);

    if (!form) return <p className='stage-note'>This form is missing its command metadata.</p>;
    if (!route && !data.routesReady) return <p className='stage-note'>The modeled command “{form.command}” is waiting for Stage routes.</p>;
    if (!route || !commandType) return <p className='stage-note'>The modeled command “{form.command}” is not registered by this Stage.</p>;

    return (
        <section className='stage-command-form' data-command={form.command} data-layout-mode={form.layout.mode}>
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
    const fieldGenerationMode = element.properties.mode === FormFieldGenerationMode.Manual ? FormFieldGenerationMode.Manual : FormFieldGenerationMode.Auto;
    const layout = layoutGeometryFor(element.properties, fields);

    return {
        command,
        label: text(element, 'label', command),
        fields,
        route: text(element, 'route') || undefined,
        fieldGenerationMode,
        layout,
    };
}

function fieldsFor(form: NativeFormProperties, schema: SchemaProperty[]): FormField[] {
    if (form.fieldGenerationMode === FormFieldGenerationMode.Manual || form.fields.length > 0) return form.fields;
    return schema.map(property => ({ name: property.name, label: property.name }));
}

function renderFields(form: NativeFormProperties, fields: FormField[], schema: SchemaProperty[]): ReactNode {
    if (form.layout.columns.length === 0) return fields.map(field => renderField(field, schema));

    const byName = new Map(fields.map(field => [field.name, field]));
    const rendered = new Set<string>();
    const columns = form.layout.columns;
    const columnsStyle: CSSProperties = {
        gridTemplateColumns: columns.map(columnWidth).join(' '),
    };

    return (
        <div className='stage-command-form__layout' data-layout-mode={form.layout.mode} style={columnsStyle}>
            {columns.map((column, index) => (
                <div key={index} className='stage-command-form__column' data-resizable={column.resizable ? 'true' : undefined} style={columnStyle(column)}>
                    {column.fields.map(placement => {
                        const field = byName.get(placement.name);
                        if (!field) return null;
                        rendered.add(field.name);
                        return renderField(field, schema, placement);
                    })}
                    {index === columns.length - 1 && fields.filter(field => !rendered.has(field.name)).map(field => renderField(field, schema))}
                </div>
            ))}
        </div>
    );
}

function renderField(field: FormField, schema: SchemaProperty[], placement?: FormFieldPlacement) {
    const property = field.sourceProperty ?? field.name;
    const descriptor = schema.find(_ => _.name === property);
    const title = field.label ?? field.name;
    const accessor = (command: unknown) => (command as Record<string, unknown>)[property];
    const required = descriptor?.required ?? true;
    const fieldElement = (() => {
        switch (descriptor?.type ?? 'text') {
            case 'number': return <NumberField value={accessor} fieldName={property} title={title} required={required} />;
            case 'boolean': return <CheckboxField value={accessor} fieldName={property} label={title} required={required} />;
            case 'date': return <CalendarField value={accessor} fieldName={property} title={title} required={required} showIcon />;
            default: return <InputTextField value={accessor} fieldName={property} title={title} required={required} />;
        }
    })();

    const style = placementStyle(placement ?? placementForField(field));
    if (!style) return <span key={field.name} className='stage-command-form__field'>{fieldElement}</span>;

    return <span key={field.name} className='stage-command-form__field' data-field={field.name} style={style}>{fieldElement}</span>;
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

function layoutGeometryFor(properties: Record<string, unknown>, fields: FormField[]): FormLayoutGeometry {
    const configured = geometryValue(properties.geometry) ?? geometryValue(properties.layout) ?? geometryValue(properties.formLayout);
    const rawColumns = configured?.columns ?? properties.columns;
    const columns = parseColumns(rawColumns);
    if (columns.length > 0) return { mode: configured?.mode ?? FormLayoutMode.Manual, columns };

    const fieldColumns = columnsFromFieldMetadata(fields);
    if (fieldColumns.length > 0) return { mode: configured?.mode ?? FormLayoutMode.Manual, columns: fieldColumns };

    return { mode: configured?.mode ?? FormLayoutMode.Auto, columns: [] };
}

function geometryValue(value: unknown): { mode?: FormLayoutMode; columns?: unknown } | undefined {
    if (!isRecord(value)) return undefined;
    return {
        mode: value.mode === FormLayoutMode.Manual ? FormLayoutMode.Manual : value.mode === FormLayoutMode.Auto ? FormLayoutMode.Auto : undefined,
        columns: value.columns,
    };
}

function parseColumns(value: unknown): FormColumnGeometry[] {
    if (!Array.isArray(value)) return [];
    return value.map(parseColumn).filter((column): column is FormColumnGeometry => column.fields.length > 0);
}

function parseColumn(value: unknown): FormColumnGeometry {
    if (Array.isArray(value)) {
        return { fields: value.map(parsePlacement).filter(isPlacement), resizable: false };
    }

    if (!isRecord(value)) return { fields: [], resizable: false };
    const fields = Array.isArray(value.fields) ? value.fields.map(parsePlacement).filter(isPlacement) : [];
    return {
        fields,
        width: stringValue(value.width),
        minWidth: stringValue(value.minWidth),
        maxWidth: stringValue(value.maxWidth),
        grow: numberValue(value.grow),
        resizable: value.resizable === true,
    };
}

function parsePlacement(value: unknown): FormFieldPlacement | undefined {
    if (typeof value === 'string') return { name: value };
    if (!isRecord(value)) return undefined;
    const name = stringValue(value.name) ?? stringValue(value.field) ?? stringValue(value.fieldName);
    if (!name) return undefined;

    return {
        name,
        width: stringValue(value.width),
        minWidth: stringValue(value.minWidth),
        maxWidth: stringValue(value.maxWidth),
        grow: numberValue(value.grow),
    };
}

function columnsFromFieldMetadata(fields: FormField[]): FormColumnGeometry[] {
    const columns = new Map<string, FormFieldPlacement[]>();
    for (const field of fields) {
        const metadata = field as FormField & Record<string, unknown>;
        const column = metadata.column ?? metadata.columnIndex;
        if (column === undefined || column === null) continue;
        const columnKey = String(column);
        columns.set(columnKey, [...(columns.get(columnKey) ?? []), placementForField(field)]);
    }

    return [...columns.entries()]
        .sort(([left], [right]) => Number(left) - Number(right))
        .map(([, placements]) => ({ fields: placements, resizable: false }));
}

function placementForField(field: FormField): FormFieldPlacement {
    const metadata = field as FormField & Record<string, unknown>;
    return {
        name: field.name,
        width: stringValue(metadata.width),
        minWidth: stringValue(metadata.minWidth),
        maxWidth: stringValue(metadata.maxWidth),
        grow: numberValue(metadata.grow),
    };
}

function columnWidth(column: FormColumnGeometry): string {
    if (column.width) return column.width;
    if (column.minWidth || column.maxWidth) return `minmax(${column.minWidth ?? '0'}, ${column.maxWidth ?? `${column.grow ?? 1}fr`})`;
    return `minmax(0, ${column.grow ?? 1}fr)`;
}

function columnStyle(column: FormColumnGeometry): CSSProperties {
    return {
        minWidth: column.minWidth,
        maxWidth: column.maxWidth,
        resize: column.resizable ? 'horizontal' : undefined,
        overflow: column.resizable ? 'auto' : undefined,
    };
}

function placementStyle(placement: FormFieldPlacement | undefined): CSSProperties | undefined {
    if (!placement?.width && !placement?.minWidth && !placement?.maxWidth && placement?.grow === undefined) return undefined;
    return {
        width: placement.width,
        minWidth: placement.minWidth,
        maxWidth: placement.maxWidth,
        flexGrow: placement.grow,
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

function stringValue(value: unknown): string | undefined {
    return typeof value === 'string' && value.length > 0 ? value : undefined;
}

function numberValue(value: unknown): number | undefined {
    return typeof value === 'number' && Number.isFinite(value) ? value : undefined;
}

function text(element: ExternalComponent, name: string, fallback = ''): string {
    const value = element.properties[name];
    return typeof value === 'string' ? value : fallback;
}

function isPlacement(value: FormFieldPlacement | undefined): value is FormFieldPlacement {
    return !!value;
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
}
