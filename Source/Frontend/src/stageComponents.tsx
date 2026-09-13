// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useState } from 'react';
import type { ExternalComponent, SceneElement } from '@cratis/scene.model';
import { coreComponents } from '@cratis/scene.react';
import type { ComponentType, ReactNode } from 'react';

interface RegisteredProps {
    element: ExternalComponent;
    slots: Record<string, ReactNode[]>;
}

/** Fired after a command executed, so every table on screen re-reads its query. */
const DATA_CHANGED = 'cratis.stage.data-changed';

function text(element: ExternalComponent, name: string, fallback = ''): string {
    const value = element.properties[name];
    return typeof value === 'string' ? value : fallback;
}

function useModelData(route: string | undefined): { rows: Record<string, unknown>[]; error: string } {
    const [rows, setRows] = useState<Record<string, unknown>[]>([]);
    const [error, setError] = useState('');

    const read = useCallback(() => {
        if (!route) return;
        fetch(route.replace(/^\//, ''), { headers: { Accept: 'application/json' } })
            .then(async response => {
                if (!response.ok) throw new Error(`The query answered ${response.status}.`);
                return response.json();
            })
            .then(payload => {
                const data = payload?.data ?? payload;
                setRows(Array.isArray(data) ? data : data ? [data] : []);
                setError('');
            })
            .catch(reason => setError(reason instanceof Error ? reason.message : String(reason)));
    }, [route]);

    useEffect(() => {
        read();
        globalThis.addEventListener(DATA_CHANGED, read);
        return () => globalThis.removeEventListener(DATA_CHANGED, read);
    }, [read]);

    return { rows, error };
}

/** Reads a slice's read model through the query the Stage registered for it. */
export function StageTable({ element, slots }: RegisteredProps) {
    const route = text(element, 'route');
    const { rows, error } = useModelData(route || undefined);
    const modeled = (element.slots.columns ?? []).map(column => {
        const properties = (column as ExternalComponent).properties;
        return {
            property: typeof properties.property === 'string' ? properties.property : '',
            label: typeof properties.label === 'string' ? properties.label : '',
        };
    });

    // A projection can put properties on a document the model never names - a `children` block, say. The rows
    // themselves state what is there, and showing what came back beats showing an empty header over real data.
    const discovered = [...new Set(rows.flatMap(row => Object.keys(row)))].map(property => ({ property, label: property }));
    const columns = modeled.length > 0 ? modeled : discovered;

    if (!route) {
        return (
            <section className='stage-table' data-scene-id={element.id}>
                <p className='stage-note'>No query is exposed for {text(element, 'target', 'this read model')} yet.</p>
                {slots.columns}
            </section>
        );
    }

    return (
        <section className='stage-table' data-scene-id={element.id}>
            {error && <p className='stage-note stage-note--error'>{error}</p>}
            <table>
                <thead>
                    <tr>{columns.map(column => <th key={column.property}>{column.label || column.property}</th>)}</tr>
                </thead>
                <tbody>
                    {rows.length === 0 && (
                        <tr><td colSpan={Math.max(columns.length, 1)} className='stage-empty'>Nothing recorded yet</td></tr>
                    )}
                    {rows.map((row, index) => (
                        <tr key={String(row.id ?? index)}>
                            {columns.map(column => <td key={column.property}>{format(row[column.property])}</td>)}
                        </tr>
                    ))}
                </tbody>
            </table>
        </section>
    );
}

function format(value: unknown): string {
    if (value === null || value === undefined) return '';
    if (typeof value === 'object') return JSON.stringify(value);
    return String(value);
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
export function StageAction({ element }: RegisteredProps) {
    const label = text(element, 'label', text(element, 'command'));
    const route = text(element, 'route');
    const properties = schemaProperties(element);
    const [open, setOpen] = useState(false);
    const [values, setValues] = useState<Record<string, string>>({});
    const [messages, setMessages] = useState<string[]>([]);
    const [busy, setBusy] = useState(false);

    if (!route) {
        return <button type='button' data-scene-id={element.id} disabled title='This command is not exposed as an API yet'>{label}</button>;
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

            setOpen(false);
            setValues({});
            globalThis.dispatchEvent(new CustomEvent(DATA_CHANGED));
        } catch (reason) {
            setMessages([reason instanceof Error ? reason.message : String(reason)]);
        } finally {
            setBusy(false);
        }
    };

    return (
        <div className='stage-action' data-scene-id={element.id}>
            <button type='button' onClick={() => setOpen(value => !value)}>{label}</button>
            {open && (
                <form
                    className='stage-form'
                    onSubmit={event => { event.preventDefault(); void execute(); }}>
                    {properties.map(property => (
                        <label key={property.name}>
                            <span>{property.name}</span>
                            <input
                                type={property.type}
                                value={values[property.name] ?? ''}
                                onChange={event => setValues({ ...values, [property.name]: event.target.value })}
                            />
                        </label>
                    ))}
                    {messages.map(message => <p key={message} className='stage-note stage-note--error'>{message}</p>)}
                    <button type='submit' disabled={busy}>{busy ? 'Working…' : `Execute ${label}`}</button>
                </form>
            )}
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
