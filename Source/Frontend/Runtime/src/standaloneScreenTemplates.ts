// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ScreenTemplate } from '@cratis/scene.model';

/**
 * The blueprint templates with every template that fits a slot of another template placed in the shell's content instead.
 *
 * A blueprint template may declare that it sits inside another one - the default blueprint's `MasterDetailScreen` fits
 * `WorkspaceScreen.moduleContent` - and the shared composition then wraps the screen in that outer template with the outer
 * template's own seeded content. A screen written in Screenplay fills only the slots of the template it names, so in the Stage
 * the outer template only ever draws its seed: the workspace's module navigation, bound to a `ModuleSections` query no
 * application has, drew "Nothing here yet" beside the master/detail and squeezed it into three quarters of the page.
 * Placing the inner template in the content slot composes only the template the screen names, at the full content width.
 *
 * @param templates The blueprint templates in play.
 * @returns The same templates, with a template qualified to another template's slot fitting the shell's content slot.
 */
export function standaloneScreenTemplates(templates: readonly ScreenTemplate[]): ScreenTemplate[] {
    const names = new Set(templates.map(template => template.name));
    return templates.map(template => {
        const fits = template.fitsSlot;
        const qualifier = fits?.includes('.') ? fits.slice(0, fits.lastIndexOf('.')) : undefined;
        return qualifier !== undefined && qualifier !== template.name && names.has(qualifier) ? { ...template, fitsSlot: 'content' } : template;
    });
}
