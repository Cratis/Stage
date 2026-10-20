// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import type { ExternalComponent, FormField } from '@cratis/scene.model';
import { FormGenerationMode } from '@cratis/scene.model';
import { StageCommandForm } from './StageCommandForm';
import { humanize, schemaPropertiesOf } from './stageFormSchema';
import { useStageData } from './stageData';
import { stageConfirmDialogComponent } from './stageDialogScreens';
import type { StageDecision } from './stageDialogScreens';

interface StageConfirmDialogProps {
    element: ExternalComponent;
}

/**
 * A screen that confirms a decision: its title, a button for each decision and, once one is chosen, only that decision's form. The entity the
 * decision is about is the screen's, so the identifier the command acts on is not asked for - the address or the session supplies it - and a
 * successful decision takes the person to the screen the action names.
 */
export function StageConfirmDialog({ element }: StageConfirmDialogProps) {
    const { routes } = useStageData();
    const [chosen, setChosen] = useState<string>();
    const title = typeof element.properties.title === 'string' ? element.properties.title : '';
    const decisions = Array.isArray(element.properties.decisions) ? element.properties.decisions as StageDecision[] : [];
    const decision = decisions.find(candidate => candidate.command === chosen);

    return (
        <section className='scene-slice-dialog stage-confirm-dialog' role='dialog' aria-label={humanize(title)} data-stage-component={stageConfirmDialogComponent}>
            <header className='scene-slice-dialog-header'><h2 className='scene-slice-dialog-title'>{humanize(title)}</h2></header>
            <div className='stage-confirm-dialog__decisions'>
                {decisions.map(candidate => (
                    <button key={candidate.command} type='button' aria-pressed={candidate.command === chosen} onClick={() => setChosen(candidate.command)}>
                        {labelOf(candidate)}
                    </button>
                ))}
            </div>
            {decision && <StageCommandForm key={decision.command} element={formOf(decision, routes?.commandSchemas?.[decision.command] ?? '')} />}
        </section>
    );
}

function labelOf(decision: StageDecision): string {
    return decision.label ?? humanize(decision.command);
}

function formOf(decision: StageDecision, schema: string): ExternalComponent {
    const identifiers = new Set(schemaPropertiesOf(schema).filter(property => property.identifier && property.required).map(property => property.name));
    const fields = (decision.form?.fields ?? []).filter((field: FormField) => !identifiers.has(field.sourceProperty ?? field.name));
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
            generationMode: FormGenerationMode.Manual,
            layout: decision.form?.layout,
            fields,
        },
        slots: {},
    } as unknown as ExternalComponent;
}
