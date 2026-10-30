// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { subjectFormOf, type ScreenRows } from './stageActionSubject';
import { createContext, useContext, useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import type { ComponentProps, CSSProperties, ReactNode } from 'react';
import type { CommandFormLayout, ExternalComponent, FormColumn, FormField, FormFieldPlacement, FormWidth } from '@cratis/scene.model';
import { FormGenerationMode, FormWidthUnit } from '@cratis/scene.model';
import { ArcContext } from '@cratis/arc.react';
import { DateOnly } from '@cratis/fundamentals';
import { registeredCommand, commandOutcome, withHostedBasePath } from './commandExecutor';
import { CommandForm as NativeCommandForm, withCommandFormFieldBinding } from '@cratis/arc.react/commands';
import { CheckboxField, InputTextField, NumberField, CalendarField, DropdownField } from '@cratis/components/CommandForm';
import { navigateToScreen } from './stageNavigation';
import { useStageData, useStageQuery } from './stageData';
import { heldSessionIdentity, sessionIdentity } from './stageSession';
import type { StageDataState } from './stageData';
import { asCommandValue, humanize, missingMember, schemaPropertiesOf } from './stageFormSchema';
import type { SchemaProperty } from './stageFormSchema';
import { StageValueField } from './StageValueFields';
import { useScreenRows } from './stageScreenTables';

export { schemaPropertiesOf };
export type { SchemaProperty };

/**
 * What a command form that sits in a dialog calls once its command succeeded, so the dialog it was opened in closes and the screen behind it
 * - the next step of a wizard - is reachable again. A form outside a dialog has nothing to close.
 */
export const StageDialogContext = createContext<(() => void) | undefined>(undefined);

/** The footer of the dialog a form sits in, where its submit button belongs beside Cancel; absent outside a dialog. */
export const StageDialogFooterContext = createContext<HTMLElement | null>(null);

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

// Creating an entity is about no row, even when a list on the screen carries one of the command's identifiers.
function commandFormSubject(element: ExternalComponent, rows: ScreenRows) {
    const subjectRows = element.properties.createsEntity === true ? { selectableRows: [], selectedRows: [] } : rows;
    return subjectFormOf(element, subjectRows);
}

/** Renders an authored screen form through Cratis Arc's native CommandForm boundary. */
export function StageCommandForm({ element }: StageCommandFormProps) {
    const data = useStageData();
    const arc = useContext(ArcContext);
    const closeDialog = useContext(StageDialogContext);
    const footer = useContext(StageDialogFooterContext);
    const section = useRef<HTMLElement>(null);
    const form = useMemo(() => formProperties(element), [element]);
    const schema = useMemo(() => schemaProperties(element), [element]);
    const hasSchemaMetadata = typeof element.properties.schema === 'string';
    const fields = useMemo(() => form ? fieldsFor(form, schema) : [], [form, schema]);
    const commandType = form ? registeredCommand(form.command, text(element, 'typeName'), text(element, 'route')) : undefined;
    const submitLabel = text(element, 'submitLabel') || 'Submit';
    const argumentValues = useArgumentValues(element, fields, data.resolveArgument);
    const answered = usePopulatedValues(element, schema, data);

    // The answer is a new object each time the query is read again; only a different answer is a different starting point for the form.
    const populationKey = JSON.stringify(answered ?? null);
    const populated = useMemo(() => answered, [populationKey]);
    const rows = useScreenRows();
    const { selectableRows, selectedRows, rowAction, subjectValues } = commandFormSubject(element, rows);

    // What the action binds from its subject row wins over the session, which only holds what nobody chose.
    const initialValues = useMemo(() => ({ ...populated, ...sessionIdentifiers(fields, schema, data.parameters, selectedRows, selectableRows, rowAction), ...subjectValues, ...initialCommandValues(fields, data.parameters) }), [populated, fields, schema, data.parameters, selectedRows, selectableRows, rowAction, subjectValues]);
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
        <section ref={section} className='stage-command-form' data-command={form.command} data-generation-mode={form.generationMode}>
            <NativeCommandForm
                key={populationKey}
                command={commandType}
                initialValues={initialValues}
                currentValues={argumentValues}
                validateOn='both'
                validateAllFieldsOnChange
                onBeforeExecute={command => withHostedBasePath(command, arc.apiBasePath)}
                onFieldValidate={(_command, fieldName, _oldValue, newValue) => requiredMessage(fields, schema, fieldName, newValue)}
                onValidationFailure={validation => setMessages(unboundValidationMessages(fields, validation))}
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
                {footer
                    // In a dialog the submit sits in the footer beside Cancel; it submits the form it belongs to.
                    ? createPortal(<button type='button' className='scene-slice-dialog-submit' onClick={() => section.current?.querySelector('form')?.requestSubmit()}>{submitLabel}</button>, footer)
                    : <button type='submit' className='stage-command-form__submit'>{submitLabel}</button>}
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
    return schema.filter(property => !property.unsupportedReason).map(property => ({ name: property.name, label: humanize(property.name) }));
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
            case 'date': return <StageCalendarField value={accessor} fieldName={property} title={title} required={required} showIcon isNonModal showTime={descriptor?.withTime} />;
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

// Arc binds currentValue directly from the command; the accessor only identifies the property.
const StageCalendarField = withCommandFormFieldBinding((props: ComponentProps<typeof CalendarField>) =>
    <CalendarField {...props}
        currentValue={props.showTime ? props.currentValue : calendarDate(props.currentValue)}
        onValueChange={value => props.onValueChange?.(!props.showTime && value instanceof Date && !Number.isNaN(value.getTime()) ? DateOnly.fromDate(value) : value)} />);

function calendarDate(value: unknown): unknown {
    if (value instanceof DateOnly) return value.toDate();
    if (typeof value !== 'string') return value;
    try {
        return DateOnly.parse(value).toDate();
    } catch {
        return null;
    }
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
 *
 * A schema that marks the identifier the command acts on (`x-identity`, a task dialog's) says which one that is: only it is created, and a
 * reference to another entity gets only what the address or the session supplies. A schema that marks none - an authored form's - leaves
 * every identifier it does not show to the session, as it always has.
 *
 * The address wins, then the selected row, then the identity the session already holds: a row the person selected is the entity the action is
 * about, even when the session holds another one. An identifier that a row the person could select carries (`selectableRows`) names an entity
 * that already exists: with no row selected the session does not invent it, so the value stays missing until a row is chosen. The identifier
 * a task dialog acts on (`x-identity`) is still created - the basket a shopper starts - even when a row on the screen shows the same name.
 *
 * An action on a list (`rowAction`) acts on the row the person selected, whatever the schema marks: an identifier a row carries comes from the
 * address or the selected row only - never from the session, and is never created.
 */
export function sessionIdentifiers(fields: FormField[], schema: SchemaProperty[], parameters: Record<string, string>, selectedRows: Record<string, unknown>[] = [], selectableRows: Record<string, unknown>[] = [], rowAction = false): StageCommandContent {
    const shown = new Set(fields.map(field => field.sourceProperty ?? field.name));
    const marked = schema.some(property => property.identity);
    const values: StageCommandContent = {};
    for (const property of schema) {
        if (!property.identifier || !property.required || shown.has(property.name)) continue;
        if (rowAction && carriedBy(property, selectableRows)) {
            const rowValue = parameters[property.name] || selectedIdentifier(property, selectedRows);
            if (rowValue) values[property.name] = rowValue;
            continue;
        }

        const value = parameters[property.name] || selectedIdentifier(property, selectedRows) || heldSessionIdentity(property.identifier)
            || (property.identity || (!marked && !carriedBy(property, selectableRows)) ? sessionIdentity(property.identifier) : undefined);
        if (value !== undefined) values[property.name] = value;
    }

    return values;
}

/**
 * Whether a task dialog leaves an identifier out of what it asks: the identifier the command acts on (`x-identity`) - the shopper's basket,
 * which the session supplies or creates - or one the address or the session already supplies. Every other identifier is a reference to
 * another entity - the product an add to basket is for - and is the person's to choose.
 *
 * @param property The property of the command.
 * @param parameters The screen's address parameters.
 * @returns Whether the dialog does not ask for it.
 */
export function suppliedWithoutAsking(property: SchemaProperty, parameters: Record<string, string>): boolean {
    if (!property.identifier || !property.required) return false;
    return property.identity === true || suppliedIdentifier(property, parameters) !== undefined;
}

/**
 * The value an identifier property already has without anyone typing it: the screen's parameter of the same name, or the identity the session
 * already holds for its concept. Nothing is created here, so a reference to another entity - the product an add to basket is for - that
 * nothing supplies stays a value the person filling the form is asked for.
 *
 * @param property The property of the command.
 * @param parameters The screen's address parameters.
 * @returns The value, or nothing when the property is not an identifier or nothing supplies it.
 */
export function suppliedIdentifier(property: SchemaProperty, parameters: Record<string, string>): string | undefined {
    if (!property.identifier) return undefined;
    return parameters[property.name] || heldSessionIdentity(property.identifier);
}

/**
 * The identifier of the row the person selected on the screen: an action on a list acts on the selected item, so the identifier property of the same
 * name takes that row's value and the session never invents the key of an entity that already exists. Rows that disagree about it supply nothing.
 *
 * @param property The property of the command.
 * @param selectedRows The rows currently selected on the screen.
 * @returns The selected row's value, or nothing when no selected row (or more than one, differently) carries one.
 */
export function selectedIdentifier(property: SchemaProperty, selectedRows: Record<string, unknown>[]): string | undefined {
    if (!property.identifier) return undefined;
    const values = new Set(selectedRows.map(row => row[property.name]).filter((value): value is string => typeof value === 'string' && value !== ''));
    return values.size === 1 ? [...values][0] : undefined;
}

/** Whether a row the person could select carries a property of this name - the key of an entity that already exists on the screen. */
export function carriedBy(property: SchemaProperty, rows: Record<string, unknown>[]): boolean {
    return rows.some(row => Object.hasOwn(row, property.name));
}

/**
 * A read model's rows are keyed `id`, while a command acts on that entity by `<entity>Id` - `productId` for the rows of `ProductReadiness`. The
 * row carries that key too, under the name of every word of the read model's name (`product`, `readiness`), so a command's identifier finds
 * the row that is its entity. A row that already carries a property of that name keeps it.
 */
function withEntityKeys(row: Record<string, unknown>, query: StageDataState['queries'][string]): Record<string, unknown> {
    const key = row.id;
    if (typeof key !== 'string' || key === '') return row;
    const words = `${query.alias ?? ''} ${query.name ?? ''}`.match(/[A-Z][a-z0-9]+/g) ?? [];
    const aliases = Object.fromEntries(words.map(word => `${word.charAt(0).toLowerCase()}${word.slice(1)}Id`).filter(name => !Object.hasOwn(row, name)).map(name => [name, key]));
    return Object.keys(aliases).length === 0 ? row : { ...aliases, ...row };
}

function entityRowsOf(queries: StageDataState['queries']): [Record<string, unknown>, Record<string, unknown>][] {
    return Object.values(queries).flatMap(query => query.rows.map((row): [Record<string, unknown>, Record<string, unknown>] => [row, withEntityKeys(row, query)]));
}

/** The rows of every query the screen has read: the rows a table shows and the person could select, each also carrying its entity's key. */
export function selectableRowsOf(queries: StageDataState['queries']): Record<string, unknown>[] {
    return entityRowsOf(queries).map(([, row]) => row);
}

/** The rows the person selected on the screen, each also carrying its entity's key as the rows they came from do. */
export function selectedRowsOf(selections: StageDataState['selections'], queries: StageDataState['queries']): Record<string, unknown>[] {
    const keyed = new Map(entityRowsOf(queries));
    return Object.values(selections).filter((row): row is Record<string, unknown> => row !== undefined).map(row => keyed.get(row) ?? row);
}

/**
 * The required identifiers a command acts on that the form does not show and that a selectable row supplies, and for which no row has been
 * selected yet and nothing else (the screen's address) supplies them. A toolbar action with any of these waits for a selected row - also when
 * the schema marks it as the identifier the command acts on (`x-identity`), which the host does for every modeled command.
 *
 * @param schema The command's properties.
 * @param parameters The screen's address parameters.
 * @param selectedRows The rows currently selected on the screen.
 * @param selectableRows The rows the person could select.
 * @returns The names of the identifiers still waiting for a selection.
 */
export function identifiersAwaitingSelection(schema: SchemaProperty[], parameters: Record<string, string>, selectedRows: Record<string, unknown>[], selectableRows: Record<string, unknown>[]): string[] {
    return schema
        .filter(property => property.identifier && property.required && carriedBy(property, selectableRows))
        .filter(property => !parameters[property.name] && selectedIdentifier(property, selectedRows) === undefined)
        .map(property => property.name);
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
        route: queryName ? data.routes?.queries?.[queryName] : undefined,
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
    if (identity) return sessionIdentity(identity);

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

/**
 * The validation messages Arc's command form does not already show next to a field. Arc shows one message per field - the first result
 * whose member is the field or a path under it - so exactly those are left out; any further message about the same field, and every
 * message about no field of this form, is listed under the fields.
 */
export function unboundValidationMessages(fields: FormField[], validation: { message: string; members?: string[] }[]): string[] {
    const matches = (result: { members?: string[] }, name: string) => !!result.members?.some(member => member === name || member.startsWith(`${name}.`));
    const shown = new Set(fields
        .map(field => validation.find(result => matches(result, field.sourceProperty ?? field.name)))
        .filter(result => result !== undefined));
    return [...new Set(validation.filter(result => !shown.has(result)).map(result => result.message))];
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
