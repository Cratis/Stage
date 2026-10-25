// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useId, useState, type ReactNode } from 'react';
import { asCommandFormField, type WrappedFieldProps } from '@cratis/arc.react/commands';
import { useStageData, useStageQuery } from './stageData';
import type { SchemaProperty, SchemaReference } from './stageFormSchema';
import { referenceIdentityOf } from './referenceIdentityOf';
import { StageValueField } from './StageValueFields';

interface ReferenceInputProps extends WrappedFieldProps<unknown> {
    id?: string;
    choices: Choice[];
    loading: boolean;
    title: string;
    disabled?: boolean;
}

interface Choice { key: string; value: string | number; label: string; }

interface ReferenceFieldProps {
    reference: SchemaReference;
    fallback: ReactNode;
    value: (command: unknown) => unknown;
    fieldName: string;
    title: string;
    required: boolean;
}

function ReferenceField({ reference, fallback, ...props }: ReferenceFieldProps) {
    const scope = useId();
    const { routes, routesReady } = useStageData();
    const route = routes?.queries?.[reference.query];
    const ready = routesReady && !!route;
    const result = useStageQuery({ scope, name: reference.query, route, ready, publish: false });
    if (!ready || result.error) return <>
        {fallback}
        {result.error && <span role='alert'>{result.error}</span>}
    </>;

    return <StageReferenceField {...props} choices={choicesOf(result.rows, reference)} loading={result.loading} />;
}

function ReferenceInput(props: ReferenceInputProps) {
    const selected = referenceIdentityOf(props.value);
    return <FilterableChoices {...props} selected={selected === undefined ? '' : String(selected)} />;
}

function choicesOf(rows: Record<string, unknown>[], reference: SchemaReference): Choice[] {
    return rows.flatMap(row => {
        const key = row[reference.value];
        if (typeof key !== 'string' && !(typeof key === 'number' && Number.isFinite(key))) return [];
        return [{ key: String(key), value: key, label: String(row[reference.label] ?? key) }];
    });
}

function FilterableChoices({ id, onChange, onBlur, invalid, title, required, disabled, selected, choices, loading }: ReferenceInputProps & { selected: string }) {
    const [filter, setFilter] = useState('');
    const visible = choices.filter(choice => choice.key === selected || choice.label.toLocaleLowerCase().includes(filter.toLocaleLowerCase()));
    const missing = selected !== '' && !choices.some(choice => choice.key === selected);
    return <div className='stage-reference-field'>
        <input type='search' aria-label={`Filter ${title}`} value={filter} onChange={event => setFilter(event.target.value)} disabled={disabled || loading} />
        <select id={id} aria-label={title} aria-invalid={invalid} value={selected} onChange={event => onChange(choices.find(choice => choice.key === event.target.value)?.value ?? event.target.value)} onBlur={onBlur} required={required} disabled={disabled || loading}>
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
    if (!descriptor?.reference) return fallback;
    const manualField = descriptor.type === 'number' ? <StageValueField {...props} descriptor={descriptor} /> : fallback;
    return <ReferenceField {...props} reference={descriptor.reference} fallback={manualField} />;
}

/** A reference picker bound to Arc's native command field value and validation. */
export const StageReferenceField = asCommandFormField<ReferenceInputProps>(ReferenceInput, { defaultValue: '' });
