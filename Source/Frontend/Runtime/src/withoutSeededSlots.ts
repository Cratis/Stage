// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Screen, ScreenTemplate } from '@cratis/scene.model';

/**
 * The screens with the slots they do not fill left empty, so a blueprint's own demo copy does not draw in their place.
 *
 * A blueprint template seeds each of its slots with placeholder content - the demo header, search box and empty states of the blueprint's own
 * "Deals" screen. A screen that fills a slot replaces that slot's seed (Cratis/Stage#506), but a screen written in Screenplay fills only the slots it
 * has something for - a list screen fills `header` and `list` - and the seeds of the others drew beside it ("Search deals", "Nothing here yet").
 * A slot the screen does not fill is an explicitly empty fill here, the way the Studio preview draws the same screen (Cratis/Stage#641).
 * A screen that fills no slot at all shows its template as the blueprint ships it (the template gallery, the blueprint-template vector), so it is left alone.
 *
 * @param screens The screens of the scene.
 * @param templates The screen templates in play: the application's own, its dialog templates and the blueprints'.
 * @returns The screens, each with every slot of the template it names filled or explicitly empty.
 */
export function withoutSeededSlots(screens: readonly Screen[], templates: readonly Pick<ScreenTemplate, 'name' | 'slots'>[]): Screen[] {
    const slotsOf = new Map<string, string[]>();
    for (const template of templates) if (!slotsOf.has(template.name)) slotsOf.set(template.name, template.slots.map(slot => slot.name));
    return screens.map(screen => {
        const slots = screen.screenTemplate === undefined ? undefined : slotsOf.get(screen.screenTemplate);
        if (slots === undefined || !slots.some(slot => screen.slotContent[slot] !== undefined)) return screen;
        const unfilled = slots.filter(slot => screen.slotContent[slot] === undefined);
        return unfilled.length === 0 ? screen : { ...screen, slotContent: { ...screen.slotContent, ...Object.fromEntries(unfilled.map(slot => [slot, []])) } };
    });
}
