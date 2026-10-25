// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Screen, SceneElement } from '@cratis/scene.model';
import { humanizeScreenName } from './humanize';

const titleComponent = 'core:title';
const bodySlots = ['content', 'main'];

function titleAmong(elements: readonly SceneElement[] | undefined): string | undefined {
    for (const element of elements ?? []) {
        const candidate = element as { componentName?: string; properties?: Record<string, unknown> };
        const text = candidate.properties?.text;
        if (candidate.componentName === titleComponent && typeof text === 'string' && text.trim().length > 0) return text;
    }

    return undefined;
}

/**
 * What a screen is called where the application lists it - in the navigation and the breadcrumb - when the model gave it no label.
 *
 * Only a title at the top of the screen's body names it; a title inside a section or another element names that part, not the screen.
 *
 * @param screen The screen.
 * @returns The text of the first `core:title` among the elements at the top of the screen's body (the `content` or `main` slot first),
 * otherwise the screen's name humanized, without a trailing Screen, Page or Dialog.
 */
export function screenTitle(screen: Pick<Screen, 'name' | 'slotContent'>): string {
    const slots = screen.slotContent ?? {};
    const ordered = [...bodySlots, ...Object.keys(slots).filter(name => !bodySlots.includes(name))];
    for (const name of ordered) {
        const title = titleAmong(slots[name] as SceneElement[] | undefined);
        if (title !== undefined) return title;
    }

    return humanizeScreenName(screen.name);
}
