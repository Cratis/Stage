// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { galleryDialogTemplates } from '@cratis/scene.blueprint.default';
import { ScreenFit, type ExternalComponent, type Form, type Screen, type SceneElement } from '@cratis/scene.model';

/** The screen a slice declares as its own dialog, with what of it the dialog that opens for the slice draws. */
export interface StageSliceDialogScreen {
    /** The screen the slice declares. */
    screen: Screen;

    /** The form the screen declares for the slice's command, when it declares one. */
    form?: Form;

    /** The screen the person goes to once the command succeeded, from the screen's action for the command. */
    navigateToScreen?: string;
}

const blueprintDialogTemplates = new Set(galleryDialogTemplates.map(template => template.name));

/**
 * The screen a State Change slice declares as its own dialog: the screen written inside the slice (its owner) that fits a dialog, or that
 * fills a dialog template. A toolbar button that issues the slice opens this screen - its form, its labels and where its action goes once the
 * command succeeded - rather than a form made from the command's schema (Cratis/Stage#1097).
 *
 * @param screens The screens of the scene.
 * @param slice The slice the dialog was opened for, by its qualified name (`Merchandising.Products.DraftProduct`).
 * @param command The command the dialog sends.
 * @param dialogTemplates The dialog templates the application declares itself, besides the blueprint's.
 * @returns The screen and what of it the dialog draws, or nothing when the slice declares no dialog screen.
 */
export function sliceDialogScreenOf(screens: readonly Screen[], slice: string | undefined, command: string, dialogTemplates: readonly { name: string }[] = []): StageSliceDialogScreen | undefined {
    if (!slice) return undefined;
    const templates = new Set([...blueprintDialogTemplates, ...dialogTemplates.map(template => template.name)]);
    const isDialog = (screen: Screen) => screen.fits?.includes(ScreenFit.Dialog) === true || (screen.screenTemplate !== undefined && templates.has(screen.screenTemplate));
    const owned = screens.filter(screen => screen.owner === slice && isDialog(screen));
    const screen = owned.find(candidate => candidate.forms.some(form => form.forCommand === command)) ?? owned[0];
    if (!screen) return undefined;

    const action = actionsIn(Object.values(screen.slotContent).flat()).find(candidate => candidate.properties.command === command);
    const navigateToScreen = action?.properties.navigateToScreen;
    return {
        screen,
        form: screen.forms.find(form => form.forCommand === command),
        ...(typeof navigateToScreen === 'string' && navigateToScreen !== '' ? { navigateToScreen } : {}),
    };
}

function actionsIn(elements: readonly SceneElement[]): ExternalComponent[] {
    return elements.flatMap(element => {
        const component = element as ExternalComponent;
        if (component.componentName === 'core:action') return [component];
        const children = (element as { children?: SceneElement[] }).children ?? [];
        return actionsIn([...Object.values(component.slots ?? {}).flat(), ...children]);
    });
}
