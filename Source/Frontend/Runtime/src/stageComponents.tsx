// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { registeredQuery } from './queryExecutor';
import { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react';
import type { ExternalComponent, SceneElement } from '@cratis/scene.model';
import { CoreOutletItems, CoreSliceDialog, OutletView, SliceDialogView, coreComponents, outletOfElement, sliceDialogParameters, useOptionalSceneNavigation, useSceneOutlets } from '@cratis/scene.react';
import { OutletKind, type OutletButton, type OutletItem } from '@cratis/scene.model';
import type { InteractionHandlers } from '@cratis/scene.react';
import type { ComponentType, ReactNode } from 'react';
import { Button } from '@cratis/components/Common';
import { Dialog } from '@cratis/components/Dialogs';
import { Message } from '@cratis/components/Display';
import { useStageData, useStageQuery } from './stageData';
import { DialogSlice } from './stageStructure';
import { StageCommandForm, StageDialogContext, StageDialogFooterContext, carriedBy, identifiersAwaitingSelection, selectedIdentifier, suppliedWithoutAsking } from './StageCommandForm';
import { useScreenRows } from './stageScreenTables';
import { type ActionSubject, actionCreatesEntity, actionSubjectOf, awaitsSubjectRow, itemSubjectElement, subjectActionProperties, subjectRowOf } from './stageActionSubject';
import { actionVariant } from './humanize';
import { readableValue } from './readableValue';
import { createsEntity, humanize, schemaPropertiesOf, type SchemaProperty } from './stageFormSchema';
import { registeredCommand } from './commandExecutor';
import { navigateToScreen } from './stageNavigation';
import { isGuardedAction, StageSceneGuardedAction as StageGuardedAction } from './stageData';
import { useRowInteractions } from './stageRowInteractions';
import { StageConfirmDialog } from './StageConfirmDialog';
import { stageConfirmDialogComponent } from './stageDialogScreens';
import { StageKanbanBoard, kanbanBoardKey } from './stageItemChange';

export const stageCommandFormComponent = 'Stage:commandForm';

/**
 * Builds the minimal `ExternalComponent` PrimeReact's v11 adapters need - they read configuration off
 * `element.properties` and children off `element.slots`, so a real query result or a form's fields have
 * to arrive wearing that shape to reach them. Stage keeps owning the fetch/execute logic; this is only
 * the seam between raw data and the polished widget rendering it.
 */
function syntheticElement(id: string, properties: Record<string, unknown>, slots: Record<string, SceneElement[]> = {}): ExternalComponent {
    return { id, componentName: '', properties, slots } as ExternalComponent;
}

interface RegisteredProps {
    element: ExternalComponent;
    slots: Record<string, ReactNode[]>;

    // What the document attached to this element, to put on the node it belongs on. Absent when the component
    // is rendered directly rather than through the renderer.
    interactions?: InteractionHandlers;
}

function text(element: ExternalComponent, name: string, fallback = ''): string {
    const value = element.properties[name];
    return typeof value === 'string' ? value : fallback;
}

interface TableColumn {
    property: string;
    label: string;
    options?: readonly string[];

    // The number each option carries, parallel to options. Absent for an implicitly numbered enumeration,
    // whose members are numbered by position.
    optionValues?: readonly number[];
}

function tableValue(value: unknown, options?: readonly string[], optionValues?: readonly number[]): string {
    const member = typeof value === 'number' && Number.isInteger(value)
        ? (optionValues && optionValues.length === options?.length ? options?.[optionValues.indexOf(value)] : options?.[value])
        : value;
    return typeof member === 'string' && options?.includes(member) ? humanize(member) : readableValue(value) ?? String(value ?? '');
}

interface QueryArguments {
    values: Record<string, unknown>;
    ready: boolean;
}

function queryArgumentsFor(element: ExternalComponent, data: ReturnType<typeof useStageData>): QueryArguments {
    const value = element.properties.queryArguments;
    if (!isRecord(value)) return { values: {}, ready: true };

    const values: Record<string, unknown> = {};
    let ready = true;
    for (const [name, argument] of Object.entries(value)) {
        const resolved = isBinding(argument) ? data.resolveBinding(argument) : argument;
        values[name] = resolved;
        if (resolved === undefined || resolved === null || resolved === '') ready = false;
    }

    return { values, ready };
}

function columnsFor(element: ExternalComponent, rows: Record<string, unknown>[]): TableColumn[] {
    const modeled = (element.slots.columns ?? []).map(column => {
        const properties = (column as ExternalComponent).properties;
        return {
            property: typeof properties.property === 'string' ? properties.property : '',
            label: typeof properties.label === 'string' ? properties.label : '',
            options: Array.isArray(properties.options) && properties.options.every(option => typeof option === 'string') ? properties.options : undefined,
            optionValues: Array.isArray(properties.optionValues) && properties.optionValues.every(option => typeof option === 'number') ? properties.optionValues : undefined,
        };
    }).filter(column => column.property.length > 0);

    // A projection can put properties on a document the model never names - a `children` block, say. The rows
    // themselves state what is there, and showing what came back beats showing an empty header over real data.
    const discovered = [...new Set(rows.flatMap(row => Object.keys(row)))].map(property => ({ property, label: property }));
    return modeled.length > 0 ? modeled : discovered;
}

function rowIdentity(row: Record<string, unknown>, dataKey: string): unknown {
    return row[dataKey] ?? row.id ?? row.key;
}

/**
 * Reads a slice's read model through the query the Stage registered for it.
 *
 * A table the model says navigates on a row click (`navigate to WorkItemDetails by workItemId`) carries that
 * parameter in the address: clicking a row on another screen navigates there with the row's identity, clicking a
 * row on the target screen itself updates the parameter in place, and opening an address that already carries it
 * selects the row it names. The parameter is then what every query and command on the screen is scoped by.
 */
export function StageTable({ element, slots }: RegisteredProps) {
    const data = useStageData();
    const queryName = text(element, 'query', text(element, 'typeName', element.id));
    const route = (text(element, 'query') ? data.routes?.queries?.[text(element, 'query')] : undefined) ?? text(element, 'route');
    const queryArguments = useMemo(() => queryArgumentsFor(element, data), [data, element]);
    const { rows, error, loading, awaitingCommand } = useStageQuery({ scope: element.id, name: queryName, route: route || undefined, arguments: queryArguments.values, ready: queryArguments.ready });
    const columns = columnsFor(element, rows);
    const navigateTo = text(element, 'navigateOnRowClickToScreen');
    const navigateBy = text(element, 'navigateOnRowClickByParameter');
    const dataKey = text(element, 'dataKey') || navigateBy || 'id';
    const selected = data.selections[element.id];
    const linkedIdentity = navigateBy ? data.parameters[navigateBy] : undefined;
    const rowInteractions = useRowInteractions(element);

    useEffect(() => {
        if (!selected) return;
        const selectedIdentity = rowIdentity(selected, dataKey);
        const rebound = selectedIdentity !== undefined ? rows.find(row => rowIdentity(row, dataKey) === selectedIdentity) : undefined;
        if (rebound && rebound !== selected) {
            data.selectRow(element.id, rebound);
            return;
        }

        if (!rebound && selectedIdentity !== undefined && !loading) data.clearSelection(element.id);
    }, [data, dataKey, element.id, loading, rows, selected]);

    // The address is the source of truth for a navigating table's selection: it is what a reload, a shared link
    // and the back button all restore.
    useEffect(() => {
        if (!navigateBy) return;
        if (linkedIdentity === undefined) {
            if (selected) data.clearSelection(element.id);
            return;
        }

        if (selected && String(rowIdentity(selected, dataKey)) === linkedIdentity) return;
        const linkedRow = rows.find(row => String(rowIdentity(row, dataKey)) === linkedIdentity);
        if (linkedRow) data.selectRow(element.id, linkedRow);
    }, [data, dataKey, element.id, linkedIdentity, navigateBy, rows, selected]);

    const select = (row: Record<string, unknown>) => {
        const identity = rowIdentity(row, dataKey);
        if (navigateBy && identity !== undefined && identity !== null) {
            const parameters = { ...(navigateTo && navigateTo !== data.screen ? {} : data.parameters), [navigateBy]: String(identity) };
            if (!navigateTo || navigateTo === data.screen) data.selectRow(element.id, row);
            navigateToScreen(navigateTo || data.screen, parameters);
            return;
        }

        data.selectRow(element.id, row);
    };
    const clear = () => {
        data.clearSelection(element.id);
        if (!navigateBy || data.parameters[navigateBy] === undefined) return;
        const { [navigateBy]: _, ...remaining } = data.parameters;
        navigateToScreen(data.screen, remaining);
    };

    if (!text(element, 'query') && !route && !registeredQuery(queryName)) {
        return (
            <section className='stage-table' data-scene-id={element.id}>
                <p className='stage-note'>No query is exposed for {text(element, 'typeName', 'this read model')} yet.</p>
                {slots.columns}
            </section>
        );
    }

    return (
        <section className='stage-table' data-scene-id={element.id}>
            {(loading || awaitingCommand) && <Message severity='info' text='Loading…' />}
            {error && <Message severity='error' text={error} />}
            {selected && <Button type='button' size='small' onClick={clear}>Clear selection</Button>}
            <table aria-label={text(element, 'label', text(element, 'typeName', 'Results'))}>
                <thead>
                    <tr>{columns.map(column => <th key={column.property} scope='col'>{column.label}</th>)}</tr>
                </thead>
                <tbody>
                    {rows.map((row, index) => {
                        const isSelected = selected === row;
                        return (
                            <tr
                                key={String(rowIdentity(row, dataKey) ?? index)}
                                aria-selected={isSelected}
                                tabIndex={0}
                                onClick={() => rowInteractions.click(row, () => select(row))}
                                onDoubleClick={rowInteractions.doubleClick ? () => rowInteractions.doubleClick?.(row) : undefined}
                                onKeyDown={event => {
                                    if (event.key !== 'Enter' && event.key !== ' ') return;
                                    event.preventDefault();
                                    select(row);
                                }}>
                                {columns.map(column => <td key={column.property}>{tableValue(row[column.property], column.options, column.optionValues)}</td>)}
                            </tr>
                        );
                    })}
                    <EmptyTableRows rowCount={rows.length} columnCount={columns.length} loading={loading} awaitingCommand={awaitingCommand} />
                </tbody>
            </table>
        </section>
    );
}

function EmptyTableRows({ rowCount, columnCount, loading, awaitingCommand }: { rowCount: number; columnCount: number; loading: boolean; awaitingCommand: boolean }) {
    if (rowCount !== 0) return null;
    if (loading || awaitingCommand) return null;
    return <tr><td colSpan={Math.max(columnCount, 1)}>No records found</td></tr>;
}

function isBinding(value: unknown): value is Parameters<ReturnType<typeof useStageData>['resolveBinding']>[0] {
    return isRecord(value) && (typeof value.path === 'string' || typeof value.kind === 'string');
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
}

/** Renders an authored screen form bound to a modeled command. */
export { StageCommandForm };

/** Executes a modeled command against the route the Stage registered for it. */
export function StageAction({ element, interactions, slots }: RegisteredProps) {
    return isGuardedAction(element)
        ? <StageGuardedAction element={element} interactions={interactions} />
        : <StageCommandAction element={element} interactions={interactions} slots={slots} />;
}

/**
 * A command action. An action on a list screen - a screen toolbar's Dispatch next to the shipments table - acts on the row the person selects:
 * while the command's key is one the screen's rows carry, the form does not ask for it and no row is selected, the action waits, visible
 * and disabled, instead of opening a form that would act on an entity nobody chose.
 */
function useCommandAction(input: ExternalComponent) {
    const { routes, parameters } = useStageData();
    const schema = text(input, 'schema') || routes?.commandSchemas?.[text(input, 'command')];
    const properties = useMemo(() => schemaPropertiesOf(schema ?? ''), [schema]);
    const subject = actionSubjectOf(input);
    const creates = actionCreatesEntity(input, schema);
    const { selectableRows, selectedRows } = useScreenRows();
    const auto = text(input, 'generationMode').toLowerCase() !== 'manual';
    const rowAction = infersRowAction(input, subject, creates, auto);
    const fields = auto ? autoActionFields(input, schema, properties, parameters, subject, creates ? [] : selectableRows) : {};
    const element = { ...input, properties: { ...input.properties, ...fields, ...(schema ? { schema } : {}), createsEntity: creates, rowAction } } as ExternalComponent;
    return { element, subject, properties, selectedRows };
}

function infersRowAction(element: ExternalComponent, subject: ActionSubject | undefined, creates: boolean, auto: boolean): boolean {
    if (creates) return false;
    if (subject !== undefined) return false;
    return auto || element.properties.rowAction === true;
}

/** Keep authored auto-field presentation; only row-carried identifiers become hidden bindings. With no rows there is nothing to filter. */
function autoActionFields(input: ExternalComponent, schema: string | undefined, properties: SchemaProperty[], parameters: Record<string, string>, subject: ActionSubject | undefined, rows: Record<string, unknown>[]) {
    const ownFields = input.properties.fields;
    if (!Array.isArray(ownFields) || ownFields.length === 0) return toolbarCommandFields(schema, properties, parameters, subject, rows);
    if (rows.length === 0 || subject === 'screen') return {};
    // Multiple identifiers do not prove a row target: a new comment and its parent must remain declared inputs.
    if (subject === undefined && input.properties.rowAction !== true && properties.filter(property => property.identifier).length > 1) return {};
    const carried = new Set(properties.filter(property => carriedByRows(property, subject, rows)).map(property => property.name));
    // Manual even when nothing is left to ask, so the form does not regenerate the identifiers the row supplies.
    return { fields: ownFields.filter(field => !carried.has(askedProperty(field))), generationMode: 'manual' };
}

function StageCommandAction({ element: input, interactions }: RegisteredProps) {
    const { element, subject, properties, selectedRows } = useCommandAction(input);
    const label = text(element, 'label') || humanize(text(element, 'command'));
    const command = registeredCommand(text(element, 'command'), text(element, 'typeName'), text(element, 'route'));
    const awaiting = useIdentifiersAwaitingSelection(element, subject);
    const row = subjectRowOf(subject, selectedRows);

    if (!command) {
        return <Button type='button' data-scene-id={element.id} disabled title='This command is not exposed as an API yet' {...interactions}>{label}</Button>;
    }

    // An explicit `for item` acts on exactly one selected row, even when the screen has no table to select from.
    if (awaiting.length > 0 || awaitsSubjectRow(subject, row)) {
        return <Button type='button' data-scene-id={element.id} disabled title='Select a row first' {...interactions}>{label}</Button>;
    }

    return <StageCommandDialog element={row ? itemSubjectElement(element, properties, row) : element} label={label} />;
}

/**
 * The keys an action's command acts on that the action's form does not ask for, that a row of the screen carries and that no selected row or
 * address supplies yet. A form that asks for every property of the schema - one with no fields of its own that is not manual - asks for its keys too.
 */
function useIdentifiersAwaitingSelection(element: ExternalComponent, subject?: ActionSubject): string[] {
    const { parameters } = useStageData();
    const schema = text(element, 'schema');
    const fields = element.properties.fields;
    const properties = useMemo(() => schemaPropertiesOf(schema), [schema]);
    const { selectableRows, selectedRows } = useScreenRows();
    const formFields: unknown[] = Array.isArray(fields) ? fields : [];
    const asked = hasExplicitActionFields(element, formFields)
        ? new Set(formFields.map(askedProperty).filter(name => name !== ''))
        : undefined;
    if (!asked || subject !== undefined) return [];
    if (actionCreatesEntity(element, schema)) return [];

    return identifiersAwaitingSelection(properties.filter(property => !asked.has(property.name)), parameters, selectedRows, selectableRows);
}

function hasExplicitActionFields(element: ExternalComponent, fields: unknown[]): boolean {
    if (fields.length > 0) return true;
    if (element.properties.rowAction === true) return true;
    return text(element, 'generationMode').toLowerCase() === 'manual';
}

/** The command property a form field asks for: its source property when it names one, as the form binds it, else its name. */
function askedProperty(field: unknown): string {
    if (!isRecord(field)) return '';
    if (typeof field.sourceProperty === 'string' && field.sourceProperty !== '') return field.sourceProperty;
    return typeof field.name === 'string' ? field.name : '';
}

/** Whether an action stands after another one - directly, or as a toolbar item whose wrapper stands after another action's wrapper. */
function followsEarlierAction(host: HTMLElement | null): boolean {
    const anchor = host?.parentElement?.classList.contains('stage-toolbar-action') ? host.parentElement : host;
    const earlier = anchor?.previousElementSibling;
    return earlier?.classList.contains('stage-action') === true || earlier?.classList.contains('stage-toolbar-action') === true;
}

/** The dialog a command's form opens in. The dialog keeps its own visibility, so it is closed by starting it over once the command succeeded. */
function StageCommandDialog({ element, label }: { element: ExternalComponent; label: string }) {
    const [generation, setGeneration] = useState(0);
    const [visible, setVisible] = useState(false);
    const [followsAction, setFollowsAction] = useState(false);
    const host = useRef<HTMLDivElement>(null);
    useLayoutEffect(() => setFollowsAction(followsEarlierAction(host.current)));
    const close = useCallback(() => {
        setVisible(false);
        setGeneration(current => current + 1);
    }, []);
    const content = <StageDialogContext.Provider value={close}><StageCommandForm key={generation} element={element} cancellable /></StageDialogContext.Provider>;

    return (
        <div className='stage-action' data-scene-id={element.id} ref={host}>
            <Button type='button' variant={actionVariant(text(element, 'emphasis'), followsAction)} onClick={() => setVisible(true)}>{label}</Button>
            <Dialog title={label} visible={visible} onClose={() => setVisible(false)} buttons={null} dismissable dismissOnOutsideClick={false}>{content}</Dialog>
        </div>
    );
}

/**
 * The task dialog of a toolbar slice button. Scene's own dialog asks for a text, a number, a switch or a date for each property of
 * the command; a command that takes an enumeration, a record or a collection is asked through the command's own form instead - a choice, a
 * group of fields, a list. The identifier the command acts on - the shopper's basket - is not asked for: the session supplies it, or creates it on
 * submit. A reference to another entity - the product an add to basket is for - is the person's to choose, and is asked for unless the screen's
 * address or the session already supplies it; the form then submits what was not asked by the same rule.
 */
export function StageSliceDialog(props: Partial<RegisteredProps>) {
    const element = props.element ?? syntheticElement('slice-dialog', {});
    const navigation = useOptionalSceneNavigation();
    const opened = sliceDialogParameters(navigation?.currentDialogParameters);
    return <DialogSlice slice={text(element, 'slice') || opened?.slice || text(element, 'command') || opened?.command}><StageSliceDialogContent {...props} /></DialogSlice>;
}

function StageSliceDialogContent(props: Partial<RegisteredProps>) {
    const element = props.element ?? syntheticElement('slice-dialog', {});
    const navigation = useOptionalSceneNavigation();
    const { routes, parameters } = useStageData();
    const opened = sliceDialogParameters(navigation?.currentDialogParameters);
    const command = text(element, 'command') || opened?.command || text(element, 'slice') || '';
    const title = text(element, 'title') || opened?.title || humanize(command);
    const schema = routes?.commandSchemas?.[command];
    const properties = useMemo(() => schemaPropertiesOf(schema ?? ''), [schema]);
    const { selectableRows, selectedRows } = useScreenRows();
    const close = useCallback(() => navigation?.closeDialog(), [navigation]);
    const [footer, setFooter] = useState<HTMLElement | null>(null);

    // A key the row the person selected carries is not asked for: the form fills it from that row (the command acts on it).
    const fromSelectedRow = (property: SchemaProperty) => property.identifier && property.required && carriedBy(property, selectableRows) && selectedIdentifier(property, selectedRows) !== undefined;
    if (!schema || !(properties.some(property => property.options || property.members || property.item) || properties.some(fromSelectedRow))) return props.element ? <CoreSliceDialog element={props.element} slots={props.slots ?? {}} /> : <SliceDialogView />;

    const asked = properties.filter(property => !suppliedWithoutAsking(property, parameters) && !fromSelectedRow(property));
    const form = syntheticElement(`slice-dialog-${command}`, {
        command,
        label: title,
        schema,
        fields: asked.map(property => ({ name: property.name, label: humanize(property.name) })),
        generationMode: 'manual',
    });

    return (
        <section className='scene-slice-dialog' data-scene-slice-dialog={text(element, 'slice') || opened?.slice} data-scene-command={command}>
            <header className='scene-slice-dialog-header'><h2 className='scene-slice-dialog-title'>{title}</h2></header>
            <StageDialogContext.Provider value={close}><StageDialogFooterContext.Provider value={footer}><StageCommandForm element={form} /></StageDialogFooterContext.Provider></StageDialogContext.Provider>
            <footer ref={setFooter} className='scene-slice-dialog-footer'><button type='button' className='scene-slice-dialog-cancel' onClick={close}>Cancel</button></footer>
        </section>
    );
}

/** The component the Stage draws an outlet button that names a slice or a command with. */
export const stageToolbarItemComponent = 'core:toolbarItem';

/** The properties a Stage toolbar item reads, taken from an outlet button. */
interface ToolbarItemProps extends RegisteredProps {
    /** What Scene draws for the button when the Stage has nothing of its own to add; the button alone when not given. */
    fallback?: ReactNode;
}

/** The outlet button a Stage toolbar item stands for, as Scene draws it on its own. */
function SceneButtonOf({ element }: { element: ExternalComponent }) {
    const button: OutletButton = {
        kind: 'button',
        id: text(element, 'name', element.id),
        title: text(element, 'label', text(element, 'name', element.id)),
        ...(text(element, 'slice') ? { slice: text(element, 'slice') } : {}),
        ...(text(element, 'icon') ? { icon: text(element, 'icon') } : {}),
    } as OutletButton;
    const outlet = outletOfElement(element.id, { name: element.id, kind: OutletKind.Actions, items: [button] }, undefined);
    return outlet ? <OutletView outlet={outlet} elementId={element.id} /> : null;
}

/**
 * An outlet a screen places (`core:outletItems`, what a Screenplay toolbar became in Scene 5). Scene resolves the outlet and draws
 * every kind but actions; of an outlet of actions, Scene draws the buttons that open a destination or run a built-in, and the Stage
 * draws the ones that name a slice or a command, so they reach this running application: a slice button that waits for a row, and a
 * command button that opens the command's form and posts it to the route the Stage registered. A group is Scene's.
 */
function StageOutletItems(props: RegisteredProps) {
    const { element } = props;
    const context = useSceneOutlets();
    const outlet = useMemo(() => outletOfElement(element.id, element.properties, context), [context, element.id, element.properties]);
    if (!outlet || outlet.kind !== OutletKind.Actions || !outlet.items.some(entry => toolbarItemOf(element.id, entry.item))) return <CoreOutletItems {...props} />;

    return (
        <div className='stage-toolbar' role='toolbar' aria-label={outlet.name} data-scene-id={element.id} data-scene-outlet={outlet.name}>
            {segmentsOf(outlet.items, entry => toolbarItemOf(element.id, entry.item)).map(segment => {
                // A run of buttons Scene draws is one unnamed group, so the outlet stays the one toolbar a person reaches by its name.
                const scene = (entries: typeof outlet.items) =>
                    <OutletView key={entries[0].item.id} outlet={{ ...outlet, name: '', items: entries }} item={element.properties.item} elementId={`${element.id}.${entries[0].item.id}`} />;
                return segment.stage
                    ? <StageToolbarItem key={segment.entries[0].item.id} element={segment.stage} slots={{}} interactions={props.interactions} fallback={scene(segment.entries)} />
                    : scene(segment.entries);
            })}
        </div>
    );
}

/** Splits the items of an outlet into the runs Scene draws and the single items the Stage draws, in order. */
function segmentsOf<TEntry>(entries: TEntry[], stageItemOf: (entry: TEntry) => ExternalComponent | undefined): { entries: TEntry[]; stage?: ExternalComponent }[] {
    const segments: { entries: TEntry[]; stage?: ExternalComponent }[] = [];
    for (const entry of entries) {
        const stage = stageItemOf(entry);
        const last = segments.at(-1);
        if (stage) segments.push({ entries: [entry], stage });
        else if (last && !last.stage) last.entries.push(entry);
        else segments.push({ entries: [entry] });
    }
    return segments;
}

/** The toolbar item the Stage draws for an outlet button that names a slice or a command, or undefined for one Scene draws. */
function toolbarItemOf(owner: string, item: OutletItem): ExternalComponent | undefined {
    if (item.kind !== 'button') return undefined;
    const button = item as OutletButton;
    if (button.navigateTo || button.builtIn !== undefined || (!button.slice && !button.command)) return undefined;
    return {
        id: `${owner}.${button.id}`,
        componentName: stageToolbarItemComponent,
        properties: {
            name: button.id,
            kind: 'Action',
            label: button.title,
            ...(button.slice ? { slice: button.slice } : {}),
            ...(button.command ? { target: button.command, command: button.command } : {}),
            ...(button.emphasis ? { emphasis: button.emphasis } : {}),
            ...(button.icon ? { icon: button.icon } : {}),
            ...(button.confirm ? { confirm: button.confirm } : {}),
            ...(button.subject ? { actionSubject: button.subject } : {}),
        },
        slots: {},
    } as unknown as ExternalComponent;
}

/**
 * A toolbar item that names a slice opens the slice's task dialog, which submits through the Stage's slice invoker to the route the
 * Stage registered for its command. An item that executes a command by name is the Stage's command action, so it opens the command's
 * form and posts it to that route, instead of only announcing that the command is wanted.
 */
function StageToolbarItem(props: ToolbarItemProps) {
    const { element } = props;
    if (text(element, 'slice') !== '') return <StageToolbarSliceButton {...props} />;
    return <StageToolbarCommand {...props} />;
}

/**
 * A toolbar button that opens the task dialog of a slice. On a screen whose rows carry the key the slice's command acts on - a button a feature
 * adds to the toolbar of its list screen, such as Approve product - the button waits, visible and disabled, until a row is selected, and the
 * dialog then acts on that row instead of asking for a key nobody can know. A command whose key no row of the screen carries is not held back.
 * The command is the one the button names, or the last segment of its slice when it does not name one.
 */
function StageToolbarSliceButton(props: ToolbarItemProps) {
    const { element } = props;
    const { routes, parameters } = useStageData();
    const slice = text(element, 'slice');
    const command = text(element, 'command') || slice.split('.').at(-1) || '';
    const schema = routes?.commandSchemas?.[command];
    const properties = useMemo(() => schemaPropertiesOf(schema ?? ''), [schema]);
    const { selectableRows, selectedRows } = useScreenRows();
    const awaiting = createsEntity(schema ?? '') ? [] : identifiersAwaitingSelection(properties, parameters, selectedRows, selectableRows);
    if (awaiting.length === 0) return <>{props.fallback ?? <SceneButtonOf element={element} />}</>;

    const label = text(element, 'label', text(element, 'name', element.id));
    return (
        <span className='stage-toolbar-action' data-scene-toolbar-item={text(element, 'name', element.id)}>
            <Button type='button' data-scene-id={element.id} disabled title='Select a row first' {...props.interactions}>{label}</Button>
        </span>
    );
}

/** Whether the screen's rows supply a required identifier, so the form does not ask for it; a `screen` subject never reads a row. */
function carriedByRows(property: SchemaProperty, subject: ActionSubject | undefined, selectableRows: Record<string, unknown>[]): boolean {
    return subject !== 'screen' && Boolean(property.identifier) && property.required && carriedBy(property, selectableRows);
}

/**
 * A toolbar item that executes a command. The command's own schema, when the Stage carries one, makes the form: what a selectable row supplies
 * is not asked for, and an action on a list waits - visible, disabled - until a row is selected, instead of acting on an entity nobody chose.
 * Without an explicit subject, a command that creates an entity (`x-creates`) is about no row: it neither waits for selection nor takes a selected row's identifiers.
 */
function StageToolbarCommand(props: RegisteredProps) {
    const { element } = props;
    const { routes, parameters } = useStageData();
    const name = text(element, 'name', element.id);
    const command = text(element, 'target');
    const label = text(element, 'label', name);
    const schema = routes?.commandSchemas?.[command];
    const properties = useMemo(() => schemaPropertiesOf(schema ?? ''), [schema]);
    const subject = actionSubjectOf(element);
    const creates = useMemo(() => subject === undefined && createsEntity(schema ?? ''), [schema, subject]);
    const { selectableRows: screenRows, selectedRows } = useScreenRows();
    const selectableRows = creates ? [] : screenRows;

    // `for item` waits for exactly one selected row; `for screen` never reads a row. Only an omitted subject uses creation inference.
    const awaiting = subject === undefined && !creates ? identifiersAwaitingSelection(properties, parameters, selectedRows, selectableRows) : [];
    const askedFields = toolbarCommandFields(schema, properties, parameters, subject, selectableRows);
    const intent = creates ? { createsEntity: true } : subjectActionProperties(element, subject);
    const action = { ...element, componentName: 'core:action', properties: { command, label, emphasis: text(element, 'emphasis'), ...askedFields, ...intent } } as ExternalComponent;

    return (
        <span className='stage-toolbar-action' data-scene-toolbar-item={name}>
            {awaiting.length > 0
                ? <Button type='button' data-scene-id={element.id} disabled title='Select a row first' {...props.interactions}>{label}</Button>
                : <StageAction element={action} interactions={props.interactions} slots={props.slots} />}
        </span>
    );
}

function toolbarCommandFields(schema: string | undefined, properties: SchemaProperty[], parameters: Record<string, string>, subject: ActionSubject | undefined, selectableRows: Record<string, unknown>[]) {
    if (!schema) return {};
    const asked = properties.filter(property => !suppliedWithoutAsking(property, parameters) && !carriedByRows(property, subject, selectableRows));
    return { schema, fields: asked.map(property => ({ name: property.name, label: humanize(property.name) })), generationMode: 'manual' };
}

/**
 * The components the Stage renders a scene with: Scene's own core vocabulary, with the two that have to reach
 * this running application - the table that reads a modeled query, and the action that executes a modeled
 * command - replaced by versions that do.
 */
export const stageComponents: Record<string, ComponentType<RegisteredProps>> = {
    ...(coreComponents as Record<string, ComponentType<RegisteredProps>>),
    'core:table': StageTable,
    'core:action': StageAction,
    'core:guarded-action': StageGuardedAction,
    'core:outletItems': StageOutletItems,
    [stageToolbarItemComponent]: StageToolbarItem as ComponentType<RegisteredProps>,
    'core:sliceDialog': StageSliceDialog as ComponentType<RegisteredProps>,
    [stageCommandFormComponent]: StageCommandForm,
    [stageConfirmDialogComponent]: StageConfirmDialog,
    [kanbanBoardKey]: StageKanbanBoard,
};

/** Whether an element tree contains anything the Stage can act on. */
export function isActionable(element: SceneElement): boolean {
    const component = element as ExternalComponent;
    return component.componentName === 'core:action' || component.componentName === 'core:table';
}
