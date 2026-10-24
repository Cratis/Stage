// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useId, useState, type ReactNode } from 'react';
import { asCommandFormField, type WrappedFieldProps } from '@cratis/arc.react/commands';
import { useStageData, useStageQuery } from './stageData';
import type { SchemaProperty, SchemaReference } from './stageFormSchema';

interface ReferenceInputProps extends WrappedFieldProps<unknown> {
    id?: string;
    reference: SchemaReference;
    title: string;
    disabled?: boolean;
}

interface Choice { key: string; label: string; }

function ReferenceInput(props: ReferenceInputProps) {
    const scope = useId();
    const { routes, routesReady } = useStageData();
    const route = routes?.queries?.[props.reference.query];
    const ready = routesReady && !!route;
    const result = useStageQuery({ scope, name: props.reference.query, route, ready, publish: false });
    const selected = typeof props.value === 'string' ? props.value : '';
    if (!ready || result.error) return <>
        <input id={props.id} aria-label={props.title} aria-invalid={props.invalid} value={selected} onChange={event => props.onChange(event.target.value)} onBlur={props.onBlur} required={props.required} disabled={props.disabled} />
        {result.error && <span role='alert'>{result.error}</span>}
    </>;

    return <FilterableChoices {...props} selected={selected} choices={choicesOf(result.rows, props.reference)} loading={result.loading} />;
}

function choicesOf(rows: Record<string, unknown>[], reference: SchemaReference): Choice[] {
    return rows.flatMap(row => {
        const key = row[reference.value];
        if (typeof key !== 'string') return [];
        return [{ key, label: String(row[reference.label] ?? key) }];
    });
}

function FilterableChoices({ id, onChange, onBlur, invalid, title, required, disabled, selected, choices, loading }: ReferenceInputProps & { selected: string; choices: Choice[]; loading: boolean }) {
    const [filter, setFilter] = useState('');
    const visible = choices.filter(choice => choice.key === selected || choice.label.toLocaleLowerCase().includes(filter.toLocaleLowerCase()));
    const missing = selected !== '' && !choices.some(choice => choice.key === selected);
    return <div className='stage-reference-field'>
        <input type='search' aria-label={`Filter ${title}`} value={filter} onChange={event => setFilter(event.target.value)} disabled={disabled || loading} />
        <select id={id} aria-label={title} aria-invalid={invalid} value={selected} onChange={event => onChange(event.target.value)} onBlur={onBlur} required={required} disabled={disabled || loading}>
            <option value=''>Choose {title}</option>
            {missing && <option value={selected}>{selected}</option>}
            {visible.map(choice => <option key={choice.key} value={choice.key}>{choice.label}</option>)}
        </select>
        <ChoiceStatus loading={loading} empty={choices.length === 0} />
    </div>;
}

function ChoiceStatus({ loading, empty }: { loading: boolean; empty: boolean }) {
    if (loading) return <span role='status'>Loading choices…</span>;
    return empty ? <span>No choices available</span> : null;
}

/** Leaves ordinary fields unchanged; replaces only a field with a modeled reference. */
export function withReferenceField(descriptor: SchemaProperty | undefined, props: { value: (command: unknown) => unknown; fieldName: string; title: string; required: boolean }, fallback: ReactNode): ReactNode {
    return descriptor?.reference ? <StageReferenceField {...props} reference={descriptor.reference} /> : fallback;
}

/** A reference picker bound to Arc's native command field value and validation. */
export const StageReferenceField = asCommandFormField<ReferenceInputProps>(ReferenceInput, { defaultValue: '' });
