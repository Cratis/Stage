// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { chai, describe, it } from 'vitest';
import type { ExternalComponent, SceneElement, Screen } from '@cratis/scene.model';
import { composeStageScreen } from './App';
import { element } from './testElements';

chai.should();

function componentsOf(node: SceneElement): ExternalComponent[] {
    const component = node as ExternalComponent;
    return [component, ...Object.values(component.slots ?? {}).flatMap(children => children.flatMap(componentsOf))];
}

describe('composing a Stage board screen', () => {
    it('should opt the blueprint board into single card selection', () => {
        const screen = {
            name: 'PickingQueue', layout: 'AppShell', screenTemplate: 'BoardScreen', forms: [], contributions: [],
            slotContent: { header: [element('heading', 'core:title', { text: 'Picking queue' })] },
        } as unknown as Screen;
        const scene = { layouts: [], screenTemplates: [], screens: [screen] };

        const composed = composeStageScreen(scene, screen, [], 'en');

        const boards = componentsOf(composed).filter(component => component.componentName === 'Cratis.Blueprint.Default:kanbanBoard');
        boards.should.have.lengthOf(1);
        boards[0].properties.should.have.property('selectionMode', 'single');
    });
});
