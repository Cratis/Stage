// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { registeredQuery } from './queryExecutor';
import { useCallback, useEffect, useMemo, useState } from 'react';
import type { ExternalComponent, SceneElement } from '@cratis/scene.model';
import { CoreSliceDialog, CoreToolbarItem, SliceDialogView, coreComponents, sliceDialogParameters, useOptionalSceneNavigation } from '@cratis/scene.react';
import type { InteractionHandlers } from '@cratis/scene.react';
import type { ComponentType, ReactNode } from 'react';
import { Button } from 'primereact/button';
import { PrimeDialog, PrimeMessage } from '@cratis/scene.primereact';
import { useStageData, useStageQuery } from './stageData';
import { StageCommandForm, StageDialogContext, carriedBy, identifiersAwaitingSelection, selectableRowsOf, selectedIdentifier, selectedRowsOf, suppliedWithoutAsking } from './StageCommandForm';
import { humanize, schemaPropertiesOf, type SchemaProperty } from './stageFormSchema';
import { registeredCommand } from './commandExecutor';
import { navigateToScreen } from './stageNavigation';
import { isGuardedAction, StageGuardedAction } from './StageGuardedAction';
import { useRowInteractions } from './stageRowInteractions';
import { StageConfirmDialog } from './StageConfirmDialog';
import { stageConfirmDialogComponent } from './stageDialogScreens';

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
    return typeof member === 'string' && options?.includes(member) ? humanize(member) : String(value ?? '');
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
            {(loading || awaitingCommand) && <PrimeMessage element={syntheticElement(`${element.id}-loading`, { severity: 'info', text: 'Loading…' })} slots={{}} />}
            {error && <PrimeMessage element={syntheticElement(`${element.id}-error`, { severity: 'error', text: error })} slots={{}} />}
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
                    {rows.length === 0 && !loading && !awaitingCommand && (
                        <tr><td colSpan={Math.max(columns.length, 1)}>No records found</td></tr>
                    )}
                </tbody>
            </table>
        </section>
    );
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

function StageCommandAction({ element, interactions }: RegisteredProps) {
    const label = text(element, 'label', text(element, 'command'));
    const command = registeredCommand(text(element, 'command'), text(element, 'typeName'), text(element, 'route'));

    if (!command) {
        return <Button type='button' data-scene-id={element.id} disabled title='This command is not exposed as an API yet' {...interactions}>{label}</Button>;
    }

    return <StageCommandDialog element={element} label={label} />;
}

/** The dialog a command's form opens in. The dialog keeps its own visibility, so it is closed by starting it over once the command succeeded. */
function StageCommandDialog({ element, label }: { element: ExternalComponent; label: string }) {
    const [generation, setGeneration] = useState(0);
    const close = useCallback(() => setGeneration(current => current + 1), []);
    const content = <StageDialogContext.Provider value={close}><StageCommandForm element={element} /></StageDialogContext.Provider>;

    return (
        <div className='stage-action' data-scene-id={element.id}>
            <PrimeDialog
                key={generation}
                element={syntheticElement(element.id, { visible: false, header: label, triggerLabel: label })}
                slots={{ content: [content] }}
            />
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
    const { routes, parameters, queries, selections } = useStageData();
    const opened = sliceDialogParameters(navigation?.currentDialogParameters);
    const command = text(element, 'command') || opened?.command || text(element, 'slice') || '';
    const title = text(element, 'title') || opened?.title || command;
    const schema = routes?.commandSchemas?.[command];
    const properties = useMemo(() => schemaPropertiesOf(schema ?? ''), [schema]);
    const selectableRows = useMemo(() => selectableRowsOf(queries), [queries]);
    const selectedRows = useMemo(() => selectedRowsOf(selections, queries), [selections, queries]);
    const close = useCallback(() => navigation?.closeDialog(), [navigation]);

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
            <StageDialogContext.Provider value={close}><StageCommandForm element={form} /></StageDialogContext.Provider>
            <footer className='scene-slice-dialog-footer'><button type='button' className='scene-slice-dialog-cancel' onClick={close}>Cancel</button></footer>
        </section>
    );
}

/**
 * A toolbar item. An item that opens a destination is Scene's own - the Scene navigation host executes it - and so is one that names
 * a slice: it opens the slice's task dialog, which submits through the Stage's slice invoker to the route the Stage registered for
 * its command. An item that executes a command by name is the Stage's command action, so it opens the command's form and posts it
 * to that route, instead of only announcing that the command is wanted.
 */
function StageToolbarItem(props: RegisteredProps) {
    const { element } = props;
    if (text(element, 'slice') !== '') return <StageToolbarSliceButton {...props} />;
    if (text(element, 'kind').toLowerCase() !== 'action') return <CoreToolbarItem {...props} />;
    return <StageToolbarCommand {...props} />;
}

/**
 * A toolbar button that opens the task dialog of a slice. On a screen whose rows carry the key the slice's command acts on - a button a feature
 * adds to the toolbar of its list screen, such as Approve product - the button waits, visible and disabled, until a row is selected, and the
 * dialog then acts on that row instead of asking for a key nobody can know. A command whose key no row of the screen carries is not held back.
 * The command is the one the button names, or the last segment of its slice when it does not name one.
 */
function StageToolbarSliceButton(props: RegisteredProps) {
    const { element } = props;
    const { routes, parameters, queries, selections } = useStageData();
    const slice = text(element, 'slice');
    const command = text(element, 'command') || slice.split('.').at(-1) || '';
    const schema = routes?.commandSchemas?.[command];
    const properties = useMemo(() => schemaPropertiesOf(schema ?? ''), [schema]);
    const selectableRows = useMemo(() => selectableRowsOf(queries), [queries]);
    const selectedRows = useMemo(() => selectedRowsOf(selections, queries), [selections, queries]);
    const awaiting = identifiersAwaitingSelection(properties, parameters, selectedRows, selectableRows);
    if (awaiting.length === 0) return <CoreToolbarItem {...props} />;

    const label = text(element, 'label', text(element, 'name', element.id));
    return (
        <span className='stage-toolbar-action' data-scene-toolbar-item={text(element, 'name', element.id)}>
            <Button type='button' data-scene-id={element.id} disabled title='Select a row first' {...props.interactions}>{label}</Button>
        </span>
    );
}

/**
 * A toolbar item that executes a command. The command's own schema, when the Stage carries one, makes the form: what a selectable row supplies
 * is not asked for, and an action on a list waits - visible, disabled - until a row is selected, instead of acting on an entity nobody chose.
 */
function StageToolbarCommand(props: RegisteredProps) {
    const { element } = props;
    const { routes, parameters, queries, selections } = useStageData();
    const name = text(element, 'name', element.id);
    const command = text(element, 'target');
    const label = text(element, 'label', name);
    const schema = routes?.commandSchemas?.[command];
    const properties = useMemo(() => schemaPropertiesOf(schema ?? ''), [schema]);
    const selectableRows = useMemo(() => selectableRowsOf(queries), [queries]);
    const selectedRows = useMemo(() => selectedRowsOf(selections, queries), [selections, queries]);
    const awaiting = identifiersAwaitingSelection(properties, parameters, selectedRows, selectableRows);
    const asked = properties.filter(property => !suppliedWithoutAsking(property, parameters) && !(property.identifier && property.required && carriedBy(property, selectableRows)));
    const askedFields = schema ? { schema, fields: asked.map(property => ({ name: property.name, label: humanize(property.name) })), generationMode: 'manual' } : {};
    const action = { ...element, componentName: 'core:action', properties: { command, label, rowAction: true, ...askedFields } } as ExternalComponent;

    return (
        <span className='stage-toolbar-action' data-scene-toolbar-item={name}>
            {awaiting.length > 0
                ? <Button type='button' data-scene-id={element.id} disabled title='Select a row first' {...props.interactions}>{label}</Button>
                : <StageAction element={action} interactions={props.interactions} slots={props.slots} />}
        </span>
    );
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
    'core:toolbarItem': StageToolbarItem,
    'core:sliceDialog': StageSliceDialog as ComponentType<RegisteredProps>,
    [stageCommandFormComponent]: StageCommandForm,
    [stageConfirmDialogComponent]: StageConfirmDialog,
};

/** Whether an element tree contains anything the Stage can act on. */
export function isActionable(element: SceneElement): boolean {
    const component = element as ExternalComponent;
    return component.componentName === 'core:action' || component.componentName === 'core:table';
}
