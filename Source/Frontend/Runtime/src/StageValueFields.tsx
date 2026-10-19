// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { asCommandFormField, type WrappedFieldProps } from '@cratis/arc.react/commands';
import { humanize, type SchemaProperty } from './stageFormSchema';

interface ValueFieldProps extends WrappedFieldProps<unknown> {
    /** What the command's schema says about the member the field is bound to. */
    descriptor: SchemaProperty;
    title?: string;
}

interface ValueEditorProps {
    descriptor: SchemaProperty;
    label: string;
    value: unknown;
    onChange: (value: unknown) => void;
    onBlur?: () => void;
    invalid?: boolean;
}

/**
 * A command member that is a record or a collection, presented as the fields it holds: a record is a group of its members, a
 * collection a list of its elements the shopper adds to and removes from. One Arc form field binds the whole value, so the command
 * is submitted with the record and the list exactly as the schema declares them.
 */
export const StageValueField = asCommandFormField<ValueFieldProps>(props => (
    <ValueEditor
        descriptor={props.descriptor}
        label={props.title ?? humanize(props.descriptor.name)}
        value={props.value}
        onChange={props.onChange}
        onBlur={props.onBlur}
        invalid={props.invalid}
    />
), { defaultValue: undefined });

function ValueEditor({ descriptor, label, value, onChange, onBlur, invalid }: ValueEditorProps) {
    if (descriptor.members) return <RecordEditor descriptor={descriptor} label={label} value={value} onChange={onChange} onBlur={onBlur} invalid={invalid} />;
    if (descriptor.item) return <ListEditor descriptor={descriptor} label={label} value={value} onChange={onChange} onBlur={onBlur} invalid={invalid} />;
    return <ScalarEditor descriptor={descriptor} label={label} value={value} onChange={onChange} onBlur={onBlur} invalid={invalid} />;
}

function RecordEditor({ descriptor, label, value, onChange, onBlur, invalid }: ValueEditorProps) {
    const record = isRecord(value) ? value : {};
    return (
        <fieldset className='stage-record-field' data-invalid={invalid ? 'true' : undefined}>
            <legend>{label}</legend>
            {(descriptor.members ?? []).map(member => (
                <ValueEditor
                    key={member.name}
                    descriptor={member}
                    label={humanize(member.name)}
                    value={record[member.name]}
                    onBlur={onBlur}
                    onChange={memberValue => onChange({ ...record, [member.name]: memberValue })}
                />
            ))}
        </fieldset>
    );
}

function ListEditor({ descriptor, label, value, onChange, onBlur, invalid }: ValueEditorProps) {
    const item = descriptor.item as SchemaProperty;
    const elements = Array.isArray(value) ? value : [];
    const change = (next: unknown[]) => onChange(next);
    return (
        <fieldset className='stage-list-field' data-invalid={invalid ? 'true' : undefined}>
            <legend>{label}</legend>
            {elements.length === 0 && <p className='stage-note'>Nothing added yet.</p>}
            {elements.map((element, index) => (
                // The position is the identity of a row: a row has nothing else to tell it from its neighbours.
                <div className='stage-list-field__row' key={index}>
                    <ValueEditor
                        descriptor={item}
                        label={`${label} ${index + 1}`}
                        value={element}
                        onBlur={onBlur}
                        onChange={changed => change(elements.map((existing, position) => position === index ? changed : existing))}
                    />
                    <button type='button' aria-label={`Remove ${label} ${index + 1}`} onClick={() => change(elements.filter((_existing, position) => position !== index))}>Remove</button>
                </div>
            ))}
            <button type='button' aria-label={`Add ${label}`} onClick={() => change([...elements, emptyValueOf(item)])}>Add</button>
        </fieldset>
    );
}

function ScalarEditor({ descriptor, label, value, onChange, onBlur, invalid }: ValueEditorProps) {
    const common = { 'aria-invalid': invalid ? true : undefined, onBlur };
    if (descriptor.options) {
        return (
            <label className='stage-value-field'>
                <span>{label}</span>
                <select {...common} value={typeof value === 'string' ? value : ''} onChange={event => onChange(event.target.value === '' ? undefined : event.target.value)}>
                    <option value=''>{''}</option>
                    {descriptor.options.map(option => <option key={option} value={option}>{humanize(option)}</option>)}
                </select>
            </label>
        );
    }

    switch (descriptor.type) {
        case 'boolean':
            return (
                <label className='stage-value-field stage-value-field--check'>
                    <input {...common} type='checkbox' checked={value === true} onChange={event => onChange(event.target.checked)} />
                    <span>{label}</span>
                </label>
            );
        case 'number':
            return (
                <label className='stage-value-field'>
                    <span>{label}</span>
                    <input {...common} type='number' step='any' value={typeof value === 'number' ? value : ''} onChange={event => onChange(event.target.value === '' ? undefined : Number(event.target.value))} />
                </label>
            );
        case 'date':
            return (
                <label className='stage-value-field'>
                    <span>{label}</span>
                    <input {...common} type={descriptor.withTime ? 'datetime-local' : 'date'} value={typeof value === 'string' ? value.slice(0, descriptor.withTime ? 16 : 10) : ''} onChange={event => onChange(event.target.value === '' ? undefined : dateValue(event.target.value, descriptor.withTime === true))} />
                </label>
            );
        default:
            return (
                <label className='stage-value-field'>
                    <span>{label}</span>
                    <input {...common} type='text' value={typeof value === 'string' ? value : ''} onChange={event => onChange(event.target.value === '' ? undefined : event.target.value)} />
                </label>
            );
    }
}

/** What a new element of a collection starts as: nothing for a scalar, an empty record for a record. */
function emptyValueOf(descriptor: SchemaProperty): unknown {
    return descriptor.members ? {} : undefined;
}

function dateValue(value: string, withTime: boolean): string {
    if (!withTime) return value;
    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? value : parsed.toISOString();
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
}
