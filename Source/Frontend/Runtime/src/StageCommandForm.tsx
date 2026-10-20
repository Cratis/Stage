// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, useContext, useMemo, useState } from 'react';
import type { CSSProperties, ReactNode } from 'react';
import type { CommandFormLayout, ExternalComponent, FormColumn, FormField, FormFieldPlacement, FormWidth } from '@cratis/scene.model';
import { FormGenerationMode, FormWidthUnit } from '@cratis/scene.model';
import { registeredCommand, commandOutcome } from './commandExecutor';
import { CommandForm as NativeCommandForm } from '@cratis/arc.react/commands';
import { CheckboxField, InputTextField, NumberField, CalendarField, DropdownField } from '@cratis/components/CommandForm';
import { navigateToScreen } from './stageNavigation';
import { sessionIdentifierPrefix, useStageData, useStageQuery } from './stageData';
import type { StageDataState } from './stageData';
import { asCommandValue, humanize, missingMember, schemaPropertiesOf } from './stageFormSchema';
import type { SchemaProperty } from './stageFormSchema';
import { StageValueField } from './StageValueFields';

export { schemaPropertiesOf };
export type { SchemaProperty };

/**
 * What a command form that sits in a dialog calls once its command succeeded, so the dialog it was opened in closes and the screen behind it
 * - the next step of a wizard - is reachable again. A form outside a dialog has nothing to close.
 */
export const StageDialogContext = createContext<(() => void) | undefined>(undefined);

interface NativeFormProperties {
    command: string;
    label: string;
    fields: FormField[];
    generationMode: FormGenerationMode;
    layout?: CommandFormLayout;
}

interface StageCommandFormProps {
    element: ExternalComponent;
}

interface StageCommandContent {
    [key: string]: unknown;
}

/** Renders an authored screen form through Cratis Arc's native CommandForm boundary. */
export function StageCommandForm({ element }: StageCommandFormProps) {
    const data = useStageData();
    const closeDialog = useContext(StageDialogContext);
    const form = useMemo(() => formProperties(element), [element]);
    const schema = useMemo(() => schemaProperties(element), [element]);
    const hasSchemaMetadata = typeof element.properties.schema === 'string';
    const fields = useMemo(() => form ? fieldsFor(form, schema) : [], [form, schema]);
    const commandType = form ? registeredCommand(form.command, text(element, 'typeName'), text(element, 'route')) : undefined;
    const argumentValues = useArgumentValues(element, fields, data.resolveArgument);
    const answered = usePopulatedValues(element, schema, data);

    // The answer is a new object each time the query is read again; only a different answer is a different starting point for the form.
    const populationKey = JSON.stringify(answered ?? null);
    const populated = useMemo(() => answered, [populationKey]);
    const initialValues = useMemo(() => ({ ...populated, ...sessionIdentifiers(fields, schema, data.parameters), ...initialCommandValues(fields, data.parameters) }), [populated, fields, schema, data.parameters]);
    const renderedFields = useMemo(() => form ? renderFields(form, fields, schema) : undefined, [form, fields, schema]);
    const unsupportedSchemaMessages = useMemo(() => form ? unsupportedSchemaMessagesFor(fields, schema) : [], [form, fields, schema]);
    const unsupportedGeometryMessages = useMemo(() => form ? unsupportedGeometryMessagesFor(form.layout, fields) : [], [form, fields]);
    const [messages, setMessages] = useState<string[]>([]);

    if (!form) return <p className='stage-note'>This form is missing its command metadata.</p>;
    if (!commandType && !data.routesReady) return <p className='stage-note'>The modeled command “{form.command}” is waiting for Stage routes.</p>;
    if (!commandType) return <p className='stage-note'>The modeled command “{form.command}” is not registered by this Stage.</p>;
    if (fields.length === 0 && !hasSchemaMetadata && element.properties.parameterless !== true) {
        return <UnsupportedForm form={form} messages={[`The modeled command “${form.command}” has no form fields or schema metadata, so Stage will not submit an empty command body.`]} />;
    }

    if (unsupportedSchemaMessages.length > 0) return <UnsupportedForm form={form} messages={unsupportedSchemaMessages} />;

    return (
        <section className='stage-command-form' data-command={form.command} data-generation-mode={form.generationMode}>
            <NativeCommandForm
                key={populationKey}
                command={commandType}
                initialValues={initialValues}
                currentValues={argumentValues}
                validateOn='both'
                validateAllFieldsOnChange
                onFieldValidate={(_command, fieldName, _oldValue, newValue) => requiredMessage(fields, schema, fieldName, newValue)}
                onValidationFailure={validation => setMessages(validation.map(_ => _.message))}
                onException={exceptionMessages => setMessages(exceptionMessages)}
                onFailed={result => setMessages(commandOutcome(form.command, result).messages)}
                onSuccess={() => {
                    setMessages([]);
                    data.refreshAfterCommand();

                    // The action's `navigate to` is where the shopper goes once the command succeeded - the basket after adding to it.
                    const destination = text(element, 'navigateToScreen');
                    if (destination) navigateToScreen(destination, {});
                    closeDialog?.();
                }}>
                {unsupportedGeometryMessages.map(message => <p key={message} role='alert' className='stage-form-error'>{message}</p>)}
                {renderedFields}
                {messages.map(message => <p key={message} role='alert' className='stage-form-error'>{message}</p>)}
                <button type='submit'>Execute {form.label}</button>
            </NativeCommandForm>
        </section>
    );
}

function UnsupportedForm({ form, messages }: { form: NativeFormProperties; messages: string[] }) {
    return (
        <section className='stage-command-form' data-command={form.command} data-generation-mode={form.generationMode}>
            {messages.map(message => <p key={message} role='alert' className='stage-form-error'>{message}</p>)}
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
        generationMode: generationModeFor(element.properties.generationMode),
        layout: layoutFor(element.properties.layout),
    };
}

function fieldsFor(form: NativeFormProperties, schema: SchemaProperty[]): FormField[] {
    if (form.generationMode === FormGenerationMode.Manual || form.fields.length > 0) return form.fields;
    return schema.filter(property => !property.unsupportedReason).map(property => ({ name: property.name, label: property.name }));
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
        if (descriptor?.options) {
            const options = descriptor.options.map(option => ({ value: option, label: humanize(option) }));
            return <DropdownField value={accessor} fieldName={property} title={title} required={required} options={options} optionValue='value' optionLabel='label' />;
        }

        if (descriptor?.members || descriptor?.item) return <StageValueField value={accessor} fieldName={property} title={title} required={required} descriptor={descriptor} />;

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
            data-width-unit={placement?.width?.unit}
            style={style}>
            {fieldElement}
        </span>
    );
}

/**
 * The values a command starts with from the screen it is executed on.
 *
 * A screen navigated to `by workItemId` is about that work item, so a command on it that takes a `workItemId`
 * is about the same one: the parameter is carried in by exact name. Nothing else is guessed - a property the
 * screen has no parameter for stays empty and its required validation still applies, so a command is never
 * submitted for an entity the user did not pick.
 */
function initialCommandValues(fields: FormField[], parameters: Record<string, string>): StageCommandContent {
    const values: StageCommandContent = {};
    for (const field of fields) {
        const property = field.sourceProperty ?? field.name;
        const parameter = parameters[property];
        if (parameter !== undefined && parameter !== '') values[property] = parameter;
    }

    return values;
}

/**
 * The identity a session acts as - the shopper's basket - for the identifier properties a form does not show.
 *
 * A command like AddToBasket is about the shopper's own basket, which the shopper never types: the form lists the product and the quantity.
 * A required uuid property that the form does not list is that kind of identity - the screen's own parameter when it carries one, otherwise one value per
 * identifier concept for as long as this browser keeps the session - the same basket for every add, change and checkout, a different one for
 * another browser - and a command is never submitted for an identity nobody chose.
 */
export function sessionIdentifiers(fields: FormField[], schema: SchemaProperty[], parameters: Record<string, string>): StageCommandContent {
    const shown = new Set(fields.map(field => field.sourceProperty ?? field.name));
    const values: StageCommandContent = {};
    for (const property of schema) {
        if (!property.identifier || !property.required || shown.has(property.name)) continue;
        values[property.name] = parameters[property.name] || sessionIdentifier(property.identifier);
    }

    return values;
}

function sessionIdentifier(concept: string): string {
    const key = `${sessionIdentifierPrefix}${concept}`;
    try {
        const existing = globalThis.localStorage?.getItem(key);
        if (existing) return existing;
        const created = globalThis.crypto.randomUUID();
        globalThis.localStorage?.setItem(key, created);
        return created;
    } catch {
        return globalThis.crypto.randomUUID();
    }
}

/** What a form populates from: the query that answers the values the form starts with, or the item the screen is about. */
interface PopulateSource {
    queryName?: string;
    parameterBindings?: Record<string, { path?: string }>;
    itemBinding?: { path?: string };
}

function populateSourceOf(element: ExternalComponent): PopulateSource | undefined {
    const source = element.properties.populateSource;
    return isRecord(source) ? source as PopulateSource : undefined;
}

/**
 * The values a form starts with from what the application already knows - `populate via query GetCheckoutDraft by basketId`, or `populate from item`.
 *
 * The query is read for the screen's own entity (the address's parameter, or the session's identity for a concept the form does not show),
 * and every member of the command that the answer carries is where the form starts, the members the form does not list included: a review step
 * submits what the earlier steps gathered. An enumeration the read model stored by position is brought to the name the command takes.
 */
function usePopulatedValues(element: ExternalComponent, schema: SchemaProperty[], data: StageDataState): StageCommandContent | undefined {
    const source = populateSourceOf(element);
    const queryName = source?.queryName;
    const bindings = Object.entries(source?.parameterBindings ?? {});
    const resolved = bindings.map(([parameter, binding]) => [parameter, populationParameter(parameter, binding?.path ?? parameter, schema, data)] as const);
    const queryArguments = Object.fromEntries(resolved.filter(([, value]) => value !== undefined && value !== ''));
    const ready = !!queryName && data.routesReady && resolved.every(([, value]) => value !== undefined && value !== '');
    const query = useStageQuery({
        scope: `populate:${text(element, 'command')}`,
        name: queryName,
        route: queryName ? data.routes?.queries[queryName] : undefined,
        arguments: queryArguments,
        ready,
        publish: false,
    });
    const item = source?.itemBinding ? (data.resolveBinding(source.itemBinding as never) ?? data.selected) : undefined;
    const row = queryName ? query.rows[0] : (isRecord(item) ? item : undefined);

    return useMemo(() => {
        if (!row) return undefined;

        const values: StageCommandContent = {};
        for (const property of schema) {
            const value = row[property.name] ?? row[property.name.charAt(0).toLowerCase() + property.name.slice(1)];
            if (value !== undefined && value !== null) values[property.name] = asCommandValue(property, value);
        }

        return Object.keys(values).length > 0 ? values : undefined;
    }, [row, schema]);
}

function populationParameter(parameter: string, path: string, schema: SchemaProperty[], data: StageDataState): unknown {
    const fromAddress = data.parameters[path] ?? data.parameters[parameter];
    if (fromAddress) return fromAddress;

    const identity = schema.find(property => property.name === path || property.name === parameter)?.identifier;
    if (identity) return sessionIdentifier(identity);

    const bound = data.resolveBinding({ path });
    return typeof bound === 'string' || typeof bound === 'number' ? bound : undefined;
}

interface ArgumentMapping {
    name: string;
    source: string;
}

/**
 * The values an element's argument mapping supplies, resolved through Scene's argument sources. An authored
 * mapping names where each argument comes from - an input's published `value`, a table's selected item or a
 * data-context path. They are the form's current values rather than its initial ones: when a source changes -
 * another row is selected, the input is edited - the field follows it, and edits in other fields stay. A source
 * that resolves to nothing supplies nothing, so the route parameter, or an empty field, stays.
 */
function useArgumentValues(element: ExternalComponent, fields: FormField[], resolveArgument: (source: string | undefined) => unknown): StageCommandContent | undefined {
    const mapping = useMemo(() => argumentMapping(element, fields), [element, fields]);
    const resolved = mapping.map(argument => resolveArgument(argument.source));

    // `resolved` is a new array on every render; its serialized form says whether a value actually changed.
    const key = JSON.stringify(resolved);
    return useMemo(() => {
        const values = Object.fromEntries(mapping
            .map((argument, index) => [argument.name, resolved[index]] as const)
            .filter(([, value]) => value !== undefined && value !== null && value !== ''));
        return Object.keys(values).length > 0 ? values : undefined;
    }, [mapping, key]);
}

function argumentMapping(element: ExternalComponent, fields: FormField[]): ArgumentMapping[] {
    const mapping = element.properties.arguments;
    if (!Array.isArray(mapping)) return [];

    // Only the command's own members can be seeded; an argument for anything else would never be submitted.
    const members = new Set(fields.map(field => field.sourceProperty ?? field.name));
    return mapping.filter((argument): argument is ArgumentMapping =>
        typeof argument === 'object' && argument !== null &&
        typeof (argument as ArgumentMapping).name === 'string' &&
        typeof (argument as ArgumentMapping).source === 'string' &&
        members.has((argument as ArgumentMapping).name));
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
    if (column?.width?.unit === FormWidthUnit.Fraction && maxWidth) return `minmax(${minWidth ?? '0'}, ${width ?? '1fr'})`;
    if (minWidth || maxWidth) return `minmax(${minWidth ?? '0'}, ${maxWidth && width ? `min(${width}, ${maxWidth})` : maxWidth ?? width ?? '1fr'})`;
    return width ?? '1fr';
}

function placementStyle(placement: FormFieldPlacement | undefined): CSSProperties | undefined {
    if (!placement) return undefined;
    return {
        gridColumn: `${placement.column} / span ${placement.columnSpan ?? 1}`,
        gridRow: `${placement.row} / span ${placement.rowSpan ?? 1}`,
        width: placement.width?.unit === FormWidthUnit.Fraction ? undefined : widthToCss(placement.width),
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

function requiredMessage(fields: FormField[], schema: SchemaProperty[], fieldName: string, value: unknown): string | undefined {
    const field = fields.find(_ => (_.sourceProperty ?? _.name) === fieldName);
    const property = schema.find(_ => _.name === fieldName);
    if (!(property?.required ?? true)) return undefined;
    if (value === undefined || value === null || value === '') return `${field?.label ?? fieldName} is required`;
    return property ? missingMember(property, value, field?.label ?? fieldName) : undefined;
}

function unsupportedSchemaMessagesFor(fields: FormField[], schema: SchemaProperty[]): string[] {
    const schemaByName = new Map(schema.map(property => [property.name, property]));
    return fields.flatMap(field => {
        const property = schemaByName.get(field.sourceProperty ?? field.name);
        return property?.unsupportedReason ? [`The field “${field.label ?? field.name}” uses unsupported schema metadata: ${property.unsupportedReason}.`] : [];
    });
}

function unsupportedGeometryMessagesFor(layout: CommandFormLayout | undefined, fields: FormField[]): string[] {
    if (!layout) return [];

    const placementMessages = placementsFor(layout, fields)
        .filter(placement => placement.width?.unit === FormWidthUnit.Fraction)
        .map(placement => `The field “${placement.field}” uses a fractional field width, which cannot be applied to an individual form control.`);
    const columnMessages = layout.columns
        .filter(column => column.width?.unit === FormWidthUnit.Fraction && !!column.maxWidth)
        .map(column => `Column ${column.index} combines a fractional width with maxWidth; Stage keeps the fractional track and ignores maxWidth for that column.`);

    return [...placementMessages, ...columnMessages];
}

function schemaProperties(element: ExternalComponent): SchemaProperty[] {
    return schemaPropertiesOf(text(element, 'schema'));
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
