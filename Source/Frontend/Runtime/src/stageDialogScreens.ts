// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { externalComponent } from '@cratis/scene.blueprint.default';
import type { ExternalComponent, Form, Screen, SceneElement } from '@cratis/scene.model';

/** The component a screen that names the blueprint's confirmation dialog is drawn with. */
export const stageConfirmDialogComponent = 'Stage:confirmDialog';

/** The name of the confirmation dialog template of the default blueprint. */
const confirmDialogTemplate = 'ConfirmDialog';

/**
 * One choice of a confirmation dialog: an action of the screen with the form of the command it executes.
 */
export interface StageDecision {
    /** The command the decision executes. */
    command: string;

    /** The label the action carries, when the model gave it one. */
    label?: string;

    /** The screen the person goes to once the command succeeded. */
    navigateToScreen?: string;

    /** The type name of the command, when the action carries one. */
    typeName?: string;

    /** The route of the command, when the action carries one. */
    route?: string;

    /** The form the screen declares for the command, when it declares one. */
    form?: Form;
}

/**
 * The screens with those that fill the default blueprint's confirmation dialog turned into one decision dialog.
 *
 * Scene lays a dialog template's slots out as a page, so a screen written against `ConfirmDialog` drew its actions as bare command buttons
 * and its forms below them, each asking for the identifier of the entity the decision is about (Cratis/Stage#672). Such a screen is drawn as
 * one dialog instead: a button for each action, and the form of the chosen one. A screen that fills a template of the application's own
 * with that name, and every other screen, is left as it is.
 *
 * @param screens The screens of the scene.
 * @param ownTemplates The templates the application declares itself, which win over the blueprint's of the same name.
 * @returns The screens, those that are a decision dialog holding one confirm-dialog element - with the application's first other screen as the
 * place to leave to - and no flat forms.
 */
export function asDialogScreens(screens: readonly Screen[], ownTemplates: readonly { name: string }[]): Screen[] {
    const own = new Set(ownTemplates.map(template => template.name));
    return screens.map(screen => {
        if (screen.screenTemplate !== confirmDialogTemplate || own.has(confirmDialogTemplate)) return screen;
        const decisions = decisionsOf(screen);
        const home = screens.find(candidate => candidate.name !== screen.name)?.name;
        const dialog = externalComponent(`confirm-dialog-${screen.name}`, stageConfirmDialogComponent, { title: screen.name, decisions, home });
        return { ...screen, screenTemplate: undefined, slotContent: { content: [dialog] }, forms: [] };
    });
}

function decisionsOf(screen: Screen): StageDecision[] {
    const decisions: StageDecision[] = [];
    for (const action of actionsIn(Object.values(screen.slotContent).flat())) {
        const command = stringOf(action.properties.command);
        if (!command || decisions.some(decision => decision.command === command)) continue;
        decisions.push({
            command,
            label: stringOf(action.properties.label),
            navigateToScreen: stringOf(action.properties.navigateToScreen),
            typeName: stringOf(action.properties.typeName),
            route: stringOf(action.properties.route),
            form: screen.forms.find(form => form.forCommand === command),
        });
    }

    return decisions;
}

function actionsIn(elements: readonly SceneElement[]): ExternalComponent[] {
    return elements.flatMap(element => {
        const component = element as ExternalComponent;
        return component.componentName === 'core:action' ? [component] : actionsIn(Object.values(component.slots ?? {}).flat());
    });
}

function stringOf(value: unknown): string | undefined {
    return typeof value === 'string' && value !== '' ? value : undefined;
}
