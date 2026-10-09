// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useState } from 'react';
import type { CSSProperties, ReactNode } from 'react';
import type { CommandFormLayout, ExternalComponent, FormColumn, FormField, FormFieldPlacement, FormWidth } from '@cratis/scene.model';
import { FormGenerationMode, FormWidthUnit } from '@cratis/scene.model';
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
    generationMode: FormGenerationMode;
    layout?: CommandFormLayout;
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
        <section className='stage-command-form' data-command={form.command} data-generation-mode={form.generationMode}>
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
    return {
        command,
        label: text(element, 'label', command),
        fields,
        route: text(element, 'route') || undefined,
        generationMode: generationModeFor(element.properties.generationMode),
        layout: layoutFor(element.properties.layout),
    };
}

function fieldsFor(form: NativeFormProperties, schema: SchemaProperty[]): FormField[] {
    if (form.generationMode === FormGenerationMode.Manual || form.fields.length > 0) return form.fields;
    return schema.map(property => ({ name: property.name, label: property.name }));
}

function renderFields(form: NativeFormProperties, fields: FormField[], schema: SchemaProperty[]): ReactNode {
    const layout = form.layout;
    const placements = layout ? placementsFor(layout, fields) : [];
    if (!layout || (layout.columns.length === 0 && placements.length === 0)) return fields.map(field => renderField(field, schema));

    const byName = new Map(fields.map(field => [field.name, field]));
    const columnCount = gridColumnCount(layout, placements);
    const layoutStyle: CSSProperties = {
        gridTemplateColumns: Array.from({ length: columnCount }, (_value, index) => columnTrack(index + 1, layout.columns)).join(' '),
        columnGap: widthToCss(layout.columnGap),
        rowGap: widthToCss(layout.rowGap),
    };
    const placedFields = new Set<string>();

    return (
        <div className='stage-command-form__layout' style={layoutStyle}>
            {placements.map(placement => {
                const field = byName.get(placement.field);
                if (!field) return null;
                placedFields.add(field.name);
                return renderField(field, schema, placement);
            })}
            {fields.filter(field => !placedFields.has(field.name)).map(field => renderField(field, schema))}
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

    const style = placementStyle(placement);
    return (
        <span
            key={field.name}
            className='stage-command-form__field'
            data-field={field.name}
            data-row={placement?.row}
            data-column={placement?.column}
            style={style}>
            {fieldElement}
        </span>
    );
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

function generationModeFor(value: unknown): FormGenerationMode {
    return value === FormGenerationMode.Manual ? FormGenerationMode.Manual : FormGenerationMode.Auto;
}

function layoutFor(value: unknown): CommandFormLayout | undefined {
    if (!isRecord(value)) return undefined;
    const layout = value as Partial<CommandFormLayout>;
    return {
        columns: Array.isArray(layout.columns) ? layout.columns.filter(isFormColumn) : [],
        placements: Array.isArray(layout.placements) ? layout.placements.filter(isFormFieldPlacement) : [],
        columnGap: isFormWidth(layout.columnGap) ? layout.columnGap : undefined,
        rowGap: isFormWidth(layout.rowGap) ? layout.rowGap : undefined,
    };
}

function placementsFor(layout: CommandFormLayout, fields: FormField[]): FormFieldPlacement[] {
    const byField = new Map(layout.placements.map(placement => [placement.field, placement]));
    for (const field of fields) {
        if (byField.has(field.name) || !isFormFieldPlacement(field.placement)) continue;
        byField.set(field.name, field.placement);
    }

    return [...byField.values()].sort((left, right) => left.row - right.row || left.column - right.column || left.field.localeCompare(right.field));
}

function gridColumnCount(layout: CommandFormLayout, placements: FormFieldPlacement[]): number {
    return Math.max(
        1,
        ...layout.columns.map(column => column.index),
        ...placements.map(placement => placement.column + (placement.columnSpan ?? 1) - 1),
    );
}

function columnTrack(index: number, columns: FormColumn[]): string {
    const column = columns.find(candidate => candidate.index === index);
    const width = widthToCss(column?.width);
    const minWidth = widthToCss(column?.minWidth);
    const maxWidth = widthToCss(column?.maxWidth);
    if (minWidth || maxWidth) return `minmax(${minWidth ?? '0'}, ${maxWidth ?? width ?? '1fr'})`;
    return width ?? '1fr';
}

function placementStyle(placement: FormFieldPlacement | undefined): CSSProperties | undefined {
    if (!placement) return undefined;
    return {
        gridColumn: `${placement.column} / span ${placement.columnSpan ?? 1}`,
        gridRow: `${placement.row} / span ${placement.rowSpan ?? 1}`,
        width: widthToCss(placement.width),
    };
}

function widthToCss(width: FormWidth | undefined): string | undefined {
    if (!width) return undefined;
    switch (width.unit) {
        case FormWidthUnit.Fraction: return `${width.value ?? 1}fr`;
        case FormWidthUnit.Pixels: return `${width.value ?? 0}px`;
        case FormWidthUnit.Percent: return `${width.value ?? 100}%`;
        case FormWidthUnit.Auto: return 'auto';
    }
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

function isFormColumn(value: unknown): value is FormColumn {
    return isRecord(value) && typeof value.index === 'number' && Number.isInteger(value.index) && value.index > 0;
}

function isFormFieldPlacement(value: unknown): value is FormFieldPlacement {
    return isRecord(value) && typeof value.field === 'string' && typeof value.row === 'number' && typeof value.column === 'number' && value.row > 0 && value.column > 0;
}

function isFormWidth(value: unknown): value is FormWidth {
    return isRecord(value) && Object.values(FormWidthUnit).includes(value.unit as FormWidthUnit);
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
}
