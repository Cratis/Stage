// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { HorizontalAlignment, VerticalAlignment, Visibility } from '@cratis/scene.model';
import type { ExternalComponent, SceneElement } from '@cratis/scene.model';

/** Builds the Scene element a Stage serves, with the visual defaults every element carries. */
export function element(
    id: string,
    componentName: string,
    properties: Record<string, unknown> = {},
    slots: Record<string, SceneElement[]> = {},
): ExternalComponent {
    return {
        id,
        name: id,
        componentName,
        properties,
        slots,
        visibility: Visibility.Visible,
        isEnabled: true,
        opacity: 1,
        size: {},
        zIndex: 0,
        minimumSize: {},
        maximumSize: {},
        margin: { left: 0, top: 0, right: 0, bottom: 0 },
        horizontalAlignment: HorizontalAlignment.Stretch,
        verticalAlignment: VerticalAlignment.Stretch,
        borderThickness: { left: 0, top: 0, right: 0, bottom: 0 },
        padding: { left: 0, top: 0, right: 0, bottom: 0 },
        tabIndex: 0,
    } as ExternalComponent;
}
