// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useRef, useState } from 'react';
import type { ExternalComponent, FormField } from '@cratis/scene.model';
import { FormGenerationMode } from '@cratis/scene.model';
import { StageCommandForm, selectedIdentifier } from './StageCommandForm';
import { humanize, schemaPropertiesOf } from './stageFormSchema';
import type { SchemaProperty } from './stageFormSchema';
import { useStageData } from './stageData';
import { stageConfirmDialogComponent } from './stageDialogScreens';
import type { StageDecision } from './stageDialogScreens';

interface StageConfirmDialogProps {
    element: ExternalComponent;
}

/**
 * A screen that confirms a decision: its title, a button for each decision and, once one is chosen, only that decision's form. The entity the
 * decision is about is the screen's, so the identifier the command acts on is not asked for - the address, the selected row or the identity the action creates supplies it - and a
 * successful decision takes the person to the screen the action names.
 */
export function StageConfirmDialog({ element }: StageConfirmDialogProps) {
    const { routes, parameters, selections } = useStageData();
    const [chosen, setChosen] = useState<string>();
    const first = useRef<HTMLButtonElement>(null);
    const title = typeof element.properties.title === 'string' ? element.properties.title : '';
    const decisions = Array.isArray(element.properties.decisions) ? element.properties.decisions as StageDecision[] : [];
    const decision = decisions.find(candidate => candidate.command === chosen);
    const selectedRows = Object.values(selections).filter((row): row is Record<string, unknown> => row !== undefined);

    useEffect(() => first.current?.focus(), []);

    return (
        <section className='scene-slice-dialog stage-confirm-dialog' role='dialog' aria-label={humanize(title)} data-stage-component={stageConfirmDialogComponent}>
            <header className='scene-slice-dialog-header'><h2 className='scene-slice-dialog-title'>{humanize(title)}</h2></header>
            <div className='stage-confirm-dialog__decisions'>
                {decisions.map((candidate, index) => (
                    <button key={candidate.command} ref={index === 0 ? first : undefined} type='button' aria-pressed={candidate.command === chosen} onClick={() => setChosen(candidate.command)}>
                        {labelOf(candidate)}
                    </button>
                ))}
            </div>
            {decision && <StageCommandForm key={decision.command} element={formOf(decision, routes?.commandSchemas?.[decision.command] ?? '', parameters, selectedRows)} />}
        </section>
    );
}

function labelOf(decision: StageDecision): string {
    return decision.label ?? humanize(decision.command);
}

/** Whether the context supplies the identifier without asking: the address, the selected row, or the identity the command creates. */
function suppliedByContext(property: SchemaProperty, parameters: Record<string, string>, selectedRows: Record<string, unknown>[]): boolean {
    if (!property.identifier || !property.required) return false;
    return property.identity === true || !!parameters[property.name] || selectedIdentifier(property, selectedRows) !== undefined;
}

/**
 * The fields the decision asks for: the form's own, or the command's when the form lists none, without the identifiers the context supplies, and
 * with every other required identifier asked for - a reference nothing supplies is the person's to give, never invented.
 */
function fieldsOf(decision: StageDecision, properties: SchemaProperty[], parameters: Record<string, string>, selectedRows: Record<string, unknown>[]): FormField[] {
    const authored = decision.form?.fields ?? [];
    const base: FormField[] = authored.length > 0 ? authored : properties.filter(property => !property.unsupportedReason).map(property => ({ name: property.name, label: property.name }));
    const supplied = new Set(properties.filter(property => suppliedByContext(property, parameters, selectedRows)).map(property => property.name));
    const asked = base.filter(field => !supplied.has(field.sourceProperty ?? field.name));
    const present = new Set(asked.map(field => field.sourceProperty ?? field.name));
    const missing = properties.filter(property => property.identifier && property.required && !supplied.has(property.name) && !present.has(property.name));
    return [...asked, ...missing.map(property => ({ name: property.name, label: humanize(property.name) }))];
}

function formOf(decision: StageDecision, schema: string, parameters: Record<string, string>, selectedRows: Record<string, unknown>[]): ExternalComponent {
    const fields = fieldsOf(decision, schemaPropertiesOf(schema), parameters, selectedRows);
    return {
        id: `confirm-${decision.command}`,
        componentName: stageConfirmDialogComponent,
        properties: {
            command: decision.command,
            label: labelOf(decision),
            submitLabel: 'Confirm',
            typeName: decision.typeName,
            route: decision.route,
            navigateToScreen: decision.navigateToScreen,
            schema: schema || undefined,
            generationMode: fields.length === 0 ? FormGenerationMode.Manual : decision.form?.generationMode,
            layout: decision.form?.layout,
            populateSource: decision.form?.populateSource,
            fields,
        },
        slots: {},
    } as unknown as ExternalComponent;
}
