// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useMemo } from 'react';
import type React from 'react';
import type { ExternalComponent, SceneElement } from '@cratis/scene.model';
import { coreComponents } from '@cratis/scene.react';
import type { InteractionHandlers } from '@cratis/scene.react';
import type { ComponentType, ReactNode } from 'react';
import { Button } from 'primereact/button';
import { PrimeDialog, PrimeMessage } from '@cratis/scene.primereact';
import { useStageData, useStageQuery } from './stageData';
import { StageCommandForm } from './StageCommandForm';
import { navigateToScreen } from './stageNavigation';

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
    const route = (text(element, 'query') ? data.routes?.queries[text(element, 'query')] : undefined) ?? text(element, 'route');
    const queryArguments = useMemo(() => queryArgumentsFor(element, data), [data, element]);
    const { rows, error, loading } = useStageQuery({ scope: element.id, name: queryName, route: route || undefined, arguments: queryArguments.values, ready: queryArguments.ready });
    const columns = columnsFor(element, rows);
    const navigateTo = text(element, 'navigateOnRowClickToScreen');
    const navigateBy = text(element, 'navigateOnRowClickByParameter');
    const dataKey = text(element, 'dataKey') || navigateBy || 'id';
    const selected = data.selections[element.id];
    const linkedIdentity = navigateBy ? data.parameters[navigateBy] : undefined;

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

    if (!route) {
        return (
            <section className='stage-table' data-scene-id={element.id}>
                <p className='stage-note'>No query is exposed for {text(element, 'typeName', 'this read model')} yet.</p>
                {slots.columns}
            </section>
        );
    }

    return (
        <section className='stage-table' data-scene-id={element.id}>
            {loading && <PrimeMessage element={syntheticElement(`${element.id}-loading`, { severity: 'info', text: 'Loading…' })} slots={{}} />}
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
                                onClick={() => select(row)}
                                onKeyDown={event => {
                                    if (event.key !== 'Enter' && event.key !== ' ') return;
                                    event.preventDefault();
                                    select(row);
                                }}>
                                {columns.map(column => <td key={column.property}>{String(row[column.property] ?? '')}</td>)}
                            </tr>
                        );
                    })}
                    {rows.length === 0 && !loading && (
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
export function StageAction({ element, interactions }: RegisteredProps) {
    const label = text(element, 'label', text(element, 'command'));
    const route = text(element, 'route');

    if (!route) {
        return <Button type='button' data-scene-id={element.id} disabled title='This command is not exposed as an API yet' {...interactions}>{label}</Button>;
    }

    const content = <StageCommandForm element={element} />;

    return (
        <div className='stage-action' data-scene-id={element.id}>
            <PrimeDialog
                element={syntheticElement(element.id, { visible: false, header: label, triggerLabel: label })}
                slots={{ content: [content] }}
            />
        </div>
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
    [stageCommandFormComponent]: StageCommandForm,
};

/** Whether an element tree contains anything the Stage can act on. */
export function isActionable(element: SceneElement): boolean {
    const component = element as ExternalComponent;
    return component.componentName === 'core:action' || component.componentName === 'core:table';
}
