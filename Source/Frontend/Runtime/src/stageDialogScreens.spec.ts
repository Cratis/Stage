// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import type { ExternalComponent, Screen } from '@cratis/scene.model';
import { asDialogScreens, stageConfirmDialogComponent } from './stageDialogScreens';
import { element } from './testElements';

const action = (command: string) => element(`action-${command}`, 'core:action', { command, navigateToScreen: 'ApprovalInbox' });
const decision = (template?: string): Screen => ({
    name: 'Decision', layout: 'AppShell', screenTemplate: template, contributions: [],
    slotContent: { actions: [action('ApproveOrder'), action('RejectOrder')] },
    forms: [
        { name: 'ApproveOrderForm', forCommand: 'ApproveOrder', fields: [{ name: 'Order' }] },
        { name: 'RejectOrderForm', forCommand: 'RejectOrder', fields: [{ name: 'Order' }, { name: 'Reason' }] },
    ],
} as unknown as Screen);

describe('drawing a screen of the blueprint confirmation dialog as a decision dialog', () => {
    const [dialog] = asDialogScreens([decision('ConfirmDialog')], []);
    const content = dialog.slotContent.content as ExternalComponent[];

    it('puts one confirm dialog element in the screen', () => {
        expect(dialog.screenTemplate).toBeUndefined();
        expect(content.map(_ => _.componentName)).toEqual([stageConfirmDialogComponent]);
    });

    it('keeps an action with the form of its command, and the screen it navigates to', () => {
        const decisions = content[0].properties.decisions as { command: string; navigateToScreen: string; form?: { name: string } }[];
        expect(decisions.map(_ => [_.command, _.navigateToScreen, _.form?.name])).toEqual([
            ['ApproveOrder', 'ApprovalInbox', 'ApproveOrderForm'],
            ['RejectOrder', 'ApprovalInbox', 'RejectOrderForm'],
        ]);
    });

    it('clears the flat forms', () => expect(dialog.forms).toEqual([]));

    it('leaves a screen that fills another template as it is', () => {
        const other = decision('Workspace');
        expect(asDialogScreens([other], [])[0]).toBe(other);
    });

    it('leaves a screen that fills a template of the application with that name as it is', () => {
        const own = decision('ConfirmDialog');
        expect(asDialogScreens([own], [{ name: 'ConfirmDialog' }])[0]).toBe(own);
    });
});
