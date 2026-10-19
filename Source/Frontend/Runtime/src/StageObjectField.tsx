// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { asCommandFormField } from '@cratis/arc.react/commands';
import type { SchemaProperty } from './StageCommandForm';

interface ObjectFieldComponentProps {
    value: Record<string, unknown>;
    id?: string;
    onChange: (value: unknown) => void;
    onBlur?: () => void;
    invalid: boolean;
    required: boolean;
    errors: string[];
    title?: string;
    members: SchemaProperty[];
}

/** Words for a member name: `postalCode` is "Postal code". */
export function memberLabel(name: string): string {
    return name.replace(/([a-z0-9])([A-Z])/g, '$1 $2').replace(/^./, first => first.toUpperCase());
}

/**
 * A command property that is a composite value - a postal address - as a group of fields, one for each member the schema declares. The value
 * the command carries is the object they make up, so a member the person leaves empty is left out of it.
 */
export const StageObjectField = asCommandFormField<ObjectFieldComponentProps>(({ value, onChange, onBlur, invalid, title, members, id }) => {
    const current = value ?? {};
    const edit = (member: SchemaProperty, text: string) => {
        const next = { ...current };
        if (text === '') delete next[member.name];
        else next[member.name] = member.type === 'number' ? Number(text) : text;
        onChange(next);
    };

    return (
        <fieldset className='stage-object-field' aria-invalid={invalid || undefined} id={id}>
            {title && <legend>{title}</legend>}
            {members.map(member => (
                <label key={member.name} className='stage-object-field__member' data-member={member.name}>
                    <span>{memberLabel(member.name)}{member.required ? ' *' : ''}</span>
                    {member.options
                        ? (
                            <select value={String(current[member.name] ?? '')} onChange={event => edit(member, event.currentTarget.value)} onBlur={onBlur}>
                                <option value=''>-</option>
                                {member.options.map(option => <option key={option} value={option}>{memberLabel(option)}</option>)}
                            </select>
                        )
                        : (
                            <input
                                type={member.type === 'number' ? 'number' : 'text'}
                                value={String(current[member.name] ?? '')}
                                onChange={event => edit(member, event.currentTarget.value)}
                                onBlur={onBlur} />
                        )}
                </label>
            ))}
        </fieldset>
    );
}, { defaultValue: {} });
