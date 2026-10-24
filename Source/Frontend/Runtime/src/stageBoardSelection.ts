// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ExternalComponent, SceneElement } from '@cratis/scene.model';

const boardComponents = new Set(['kanbanBoard', 'Cratis.Blueprint.Default:kanbanBoard']);

/** Enables the board's native selectedItem output so toolbar actions can act on its original query row. */
export function withBoardSelection(element: SceneElement): SceneElement {
    const component = element as ExternalComponent;
    const slots = Object.fromEntries(Object.entries(component.slots ?? {}).map(([name, children]) => [name, children.map(withBoardSelection)]));
    const properties = boardComponents.has(component.componentName)
        ? { ...component.properties, selectionMode: 'single' }
        : component.properties;
    return { ...component, properties, slots } as SceneElement;
}
