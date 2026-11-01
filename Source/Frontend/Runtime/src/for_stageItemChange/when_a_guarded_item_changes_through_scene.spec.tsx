// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { InteractionTriggerKind, type Behavior } from '@cratis/scene.model';
import type { ActionDispatcher } from '@cratis/scene.engine';
import { InteractionScope, type ComponentBindingOutputs } from '@cratis/scene.react';
import { PrimeReactProvider } from '@primereact/core';
import { StageDataProvider } from '../stageData';
import { StageKanbanBoard } from '../stageItemChange';
import { element } from '../testElements';

const execute = (command: string) => ({
    kind: 'ExecuteCommand', command, arguments: [{ name: 'talkId', value: { path: 'item.talkId' } }], onSuccess: [], onFailure: [], onResult: [],
});

const cases = [
    { name: 'allowed', operator: 'Equal', target: 'accepted', applied: true, notified: false },
    { name: 'denied', operator: 'Equal', target: 'withdrawn', applied: false, notified: true },
    { name: 'unknown', operator: 'Unknown', target: 'accepted', applied: false, notified: false },
];

describe('a guarded item change through native Scene alternatives', () => {
    it.each(cases)('preserves the $name decision and card position', async ({ operator, target, applied, notified }) => {
        const dispatcher = {
            executeCommand: vi.fn().mockResolvedValue({ isSuccess: true, validationErrors: [] }),
            notify: vi.fn(),
        };
        const condition = { kind: 'comparison', left: { subject: 'item.status' }, operator, right: 'accepted' };
        const behaviors = [{ bindings: [{
            trigger: { kind: InteractionTriggerKind.Change },
            alternatives: [
                { condition, actions: [execute('AcceptTalk')] },
                { condition, actions: [execute('MustNotRun')] },
            ],
            otherwise: [{ kind: 'Notify', level: 'warning', message: { text: 'Not allowed' } }],
        }] }] as unknown as Behavior[];
        const board = {
            ...element('talkBoard', 'Cratis.Blueprint.Default.kanbanBoard', {
                columns: ['submitted', 'accepted', 'withdrawn'].map(id => ({ id, label: id })),
                cards: [{ talkId: 'a', title: 'First talk', status: 'submitted' }],
                cardId: 'talkId', cardTitle: 'title', cardColumn: 'status', editPermission: 'enabled',
            }),
            behaviors,
        };
        const bindingOutputs = { setOutput: vi.fn() } as unknown as ComponentBindingOutputs;
        render(
            <PrimeReactProvider>
                <StageDataProvider routes={{ commands: {}, queries: {} }} locale='en' locales={['en']} screen='ReviewBoard'>
                    <InteractionScope dispatcher={dispatcher as unknown as ActionDispatcher} context={{ resolve: () => undefined, subject: () => ({ status: 'withdrawn' }) }} attachments={[]}>
                        <StageKanbanBoard element={board} slots={{}} bindingOutputs={bindingOutputs} />
                    </InteractionScope>
                </StageDataProvider>
            </PrimeReactProvider>,
        );

        fireEvent.change(screen.getByLabelText(/First talk/), { target: { value: target } });

        await waitFor(() => expect(dispatcher.executeCommand).toHaveBeenCalledTimes(applied ? 1 : 0));
        if (applied) expect(dispatcher.executeCommand).toHaveBeenCalledWith('AcceptTalk', { talkId: 'a' });
        await waitFor(() => expect((screen.getByLabelText(/First talk/) as HTMLSelectElement).value).toBe(applied ? 'accepted' : 'submitted'));
        expect(dispatcher.notify).toHaveBeenCalledTimes(notified ? 1 : 0);
    });
});
