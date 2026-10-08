// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useMemo, useState } from 'react';
import type React from 'react';
import type { ExternalComponent, SceneElement } from '@cratis/scene.model';
import { coreComponents } from '@cratis/scene.react';
import type { InteractionHandlers } from '@cratis/scene.react';
import type { ComponentType, ReactNode } from 'react';
import { Button } from 'primereact/button';
import { InputText } from 'primereact/inputtext';
import { InputNumber } from 'primereact/inputnumber';
import type { InputNumberRootValueChangeEvent } from 'primereact/inputnumber';
import { PrimeDialog, PrimeMessage } from '@cratis/scene.primereact';
import { dataChanged, useStageData, useStageQuery } from './stageData';

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

function queryArgumentsFor(element: ExternalComponent, data: ReturnType<typeof useStageData>): Record<string, unknown> {
    const value = element.properties.queryArguments;
    if (!isRecord(value)) return {};

    return Object.fromEntries(Object.entries(value).map(([name, argument]) => [name, isBinding(argument) ? data.resolveBinding(argument) : argument]));
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

/** Reads a slice's read model through the query the Stage registered for it. */
export function StageTable({ element, slots }: RegisteredProps) {
    const route = text(element, 'route');
    const queryName = text(element, 'query', text(element, 'typeName', element.id));
    const data = useStageData();
    const queryArguments = useMemo(() => queryArgumentsFor(element, data), [data, element]);
    const { rows, error, loading } = useStageQuery({ scope: element.id, name: queryName, route: route || undefined, arguments: queryArguments });
    const columns = columnsFor(element, rows);
    const dataKey = text(element, 'dataKey', 'id');
    const selected = data.selections[element.id];

    useEffect(() => {
        if (!selected) return;
        const selectedIdentity = rowIdentity(selected, dataKey);
        const rebound = selectedIdentity !== undefined ? rows.find(row => rowIdentity(row, dataKey) === selectedIdentity) : undefined;
        if (rebound && rebound !== selected) {
            data.selectRow(element.id, rebound);
            return;
        }

        if (!rebound && selectedIdentity !== undefined) data.clearSelection(element.id);
    }, [data, dataKey, element.id, rows, selected]);

    if (!route) {
        return (
            <section className='stage-table' data-scene-id={element.id}>
                <p className='stage-note'>No query is exposed for {text(element, 'typeName', 'this read model')} yet.</p>
                {slots.columns}
            </section>
        );
    }

    const select = (row: Record<string, unknown>) => data.selectRow(element.id, row);
    const clear = () => data.clearSelection(element.id);

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

function isBinding(value: unknown): value is { path?: string; kind?: string } {
    return isRecord(value) && (typeof value.path === 'string' || typeof value.kind === 'string');
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
}

interface SchemaProperty {
    name: string;
    type: string;
}

function schemaProperties(element: ExternalComponent): SchemaProperty[] {
    const schema = text(element, 'schema');
    if (!schema) return [];
    try {
        const parsed = JSON.parse(schema) as { properties?: Record<string, { type?: string; format?: string }> };
        return Object.entries(parsed.properties ?? {}).map(([name, definition]) => ({
            name,
            type: definition?.type === 'integer' || definition?.type === 'number' ? 'number' : 'text',
        }));
    } catch {
        return [];
    }
}

/** Executes a modeled command against the route the Stage registered for it. */
export function StageAction({ element, interactions }: RegisteredProps) {
    const label = text(element, 'label', text(element, 'command'));
    const route = text(element, 'route');
    const properties = schemaProperties(element);
    const [values, setValues] = useState<Record<string, string>>({});
    const [messages, setMessages] = useState<string[]>([]);
    const [busy, setBusy] = useState(false);

    if (!route) {
        return <Button type='button' data-scene-id={element.id} disabled title='This command is not exposed as an API yet' {...interactions}>{label}</Button>;
    }

    const execute = async () => {
        setBusy(true);
        setMessages([]);
        try {
            const payload: Record<string, unknown> = {};
            for (const property of properties) {
                const raw = values[property.name];
                if (raw === undefined || raw === '') continue;
                payload[property.name] = property.type === 'number' ? Number(raw) : raw;
            }

            const response = await fetch(route.replace(/^\//, ''), {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload),
            });
            const result = await response.json().catch(() => undefined);
            const validation = (result?.validationResults ?? []) as { message?: string }[];
            const exceptions = (result?.exceptionMessages ?? []) as string[];
            const failures = [...validation.map(entry => entry.message ?? 'Rejected'), ...exceptions];

            if (!response.ok || failures.length > 0) {
                setMessages(failures.length > 0 ? failures : [`The command answered ${response.status}.`]);
                return;
            }

            // PrimeDialog owns its own open/close state once shown (v11's compositional Dialog is driven by
            // its own trigger, not this element's `visible` seed past first render) - a successful command
            // clears the fields so the next open starts fresh, but cannot also close a dialog it does not own.
            setValues({});
            dataChanged();
        } catch (reason) {
            setMessages([reason instanceof Error ? reason.message : String(reason)]);
        } finally {
            setBusy(false);
        }
    };

    const form = (
        <form
            key='form'
            className='stage-form'
            onSubmit={event => { event.preventDefault(); void execute(); }}>
            {properties.map(property => (
                <label key={property.name} className='stage-form-field'>
                    <span>{property.name}</span>
                    {property.type === 'number' ? (
                        <InputNumber.Root
                            value={values[property.name] ? Number(values[property.name]) : undefined}
                            onValueChange={(event: InputNumberRootValueChangeEvent) => setValues({ ...values, [property.name]: event.value === undefined || event.value === null ? '' : String(event.value) })}>
                            <InputNumber.Input />
                        </InputNumber.Root>
                    ) : (
                        <InputText
                            value={values[property.name] ?? ''}
                            onChange={(event: React.ChangeEvent<HTMLInputElement>) => setValues({ ...values, [property.name]: event.target.value })}
                        />
                    )}
                </label>
            ))}
            {messages.map(message => <PrimeMessage key={message} element={syntheticElement(`${element.id}-message`, { severity: 'error', text: message })} slots={{}} />)}
            <Button type='submit' disabled={busy}>{busy ? 'Working…' : `Execute ${label}`}</Button>
        </form>
    );

    return (
        <div className='stage-action' data-scene-id={element.id}>
            <PrimeDialog
                element={syntheticElement(element.id, { visible: open, header: label, triggerLabel: label })}
                slots={{ content: [form] }}
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
};

/** Whether an element tree contains anything the Stage can act on. */
export function isActionable(element: SceneElement): boolean {
    const component = element as ExternalComponent;
    return component.componentName === 'core:action' || component.componentName === 'core:table';
}
