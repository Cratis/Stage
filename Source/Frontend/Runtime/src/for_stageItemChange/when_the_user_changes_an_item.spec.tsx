// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { ActionDispatcher } from '@cratis/scene.engine';
import { InteractionTriggerKind, type Behavior } from '@cratis/scene.model';
import type { ComponentBindingOutputs } from '@cratis/scene.react';
import { InteractionScope } from '@cratis/scene.react';
import { PrimeReactProvider } from '@primereact/core';
import { StageDataProvider } from '../stageData';
import { StageKanbanBoard, changedItem, itemChangeOf, resolveChange } from '../stageItemChange';
import { element } from '../testElements';

const talks = [
    { talkId: 'a', title: 'First talk', status: 'submitted' },
    { talkId: 'b', title: 'Second talk', status: 'submitted' },
];
const columns = [{ id: 'submitted', label: 'Submitted' }, { id: 'accepted', label: 'Accepted' }, { id: 'withdrawn', label: 'Withdrawn' }];
const isStatus = (status: string) => ({ kind: 'comparison', left: { subject: 'item.status' }, operator: 'Equal', right: status });
const execute = (command: string, ...inputs: [string, string][]) => ({
    kind: 'ExecuteCommand', command, arguments: inputs.map(([name, path]) => ({ name, value: { path } })), onSuccess: [], onFailure: [], onResult: [],
});
const notify = { kind: 'Notify', level: 'warning', message: { text: 'A talk cannot move back to Submitted' } };

const behaviors = [{
    bindings: [{
        trigger: { kind: InteractionTriggerKind.Change },
        alternatives: [
            { condition: isStatus('accepted'), actions: [execute('AcceptTalk', ['talkId', 'item.talkId'])] },
            { condition: isStatus('withdrawn'), actions: [execute('WithdrawTalk', ['talkId', 'item.talkId'])] },
        ],
        otherwise: [notify],
    }],
}] as unknown as Behavior[];

const board = (extra: Partial<{ behaviors: Behavior[] }> = {}) => ({
    ...element('talkBoard', 'Cratis.Blueprint.Default.kanbanBoard', {
        columns, cards: talks, cardId: 'talkId', cardTitle: 'title', cardColumn: 'status', editPermission: 'enabled',
    }),
    ...extra,
});

function renderBoard(dispatcher: Partial<ActionDispatcher>, element = board({ behaviors })) {
    const outputs = new Map<string, unknown>();
    const bindingOutputs: ComponentBindingOutputs = { setOutput: (name: string, value: unknown) => { outputs.set(name, value); } } as ComponentBindingOutputs;
    render(
        <PrimeReactProvider>
            <StageDataProvider routes={{ commands: {}, queries: {} }} locale='en' locales={['en']} screen='ReviewBoard'>
                <InteractionScope dispatcher={dispatcher as ActionDispatcher} context={{ resolve: () => undefined }} attachments={[]}>
                    <StageKanbanBoard element={element} slots={{}} bindingOutputs={bindingOutputs} />
                </InteractionScope>
            </StageDataProvider>
        </PrimeReactProvider>,
    );
    return outputs;
}

const moveTo = (title: string, column: string) =>
    fireEvent.change(screen.getByLabelText(new RegExp(title)), { target: { value: column } });

describe('a kanban board with an on change', () => {
    it('runs the command of the column the card was moved to, bound to the card', async () => {
        const dispatcher = { executeCommand: vi.fn().mockResolvedValue({ isSuccess: true, validationErrors: [] }), notify: vi.fn() };
        renderBoard(dispatcher);

        moveTo('First talk', 'accepted');

        await waitFor(() => expect(dispatcher.executeCommand).toHaveBeenCalledWith('AcceptTalk', { talkId: 'a' }));
        expect(dispatcher.notify).not.toHaveBeenCalled();
    });

    it('runs a different command for another column', async () => {
        const dispatcher = { executeCommand: vi.fn().mockResolvedValue({ isSuccess: true, validationErrors: [] }), notify: vi.fn() };
        renderBoard(dispatcher);

        moveTo('Second talk', 'withdrawn');

        await waitFor(() => expect(dispatcher.executeCommand).toHaveBeenCalledWith('WithdrawTalk', { talkId: 'b' }));
        expect(dispatcher.executeCommand).toHaveBeenCalledTimes(1);
    });

    it('takes the otherwise branch when no alternative holds for the column', async () => {
        const dispatcher = { executeCommand: vi.fn().mockResolvedValue({ isSuccess: true, validationErrors: [] }), notify: vi.fn() };
        renderBoard(dispatcher, board({ behaviors }));

        moveTo('First talk', 'accepted');
        await waitFor(() => expect(dispatcher.executeCommand).toHaveBeenCalledTimes(1));
        dispatcher.executeCommand.mockClear();
        moveTo('First talk', 'submitted');

        await waitFor(() => expect(dispatcher.notify).toHaveBeenCalledWith('warning', 'A talk cannot move back to Submitted'));
        expect(dispatcher.executeCommand).not.toHaveBeenCalled();
    });

    it('puts the card back when the branch ran no command', async () => {
        const dispatcher = { executeCommand: vi.fn().mockResolvedValue({ isSuccess: true, validationErrors: [] }), notify: vi.fn() };
        renderBoard(dispatcher, board({ behaviors: [{ bindings: [{ trigger: { kind: InteractionTriggerKind.Change }, actions: [notify] }] }] as unknown as Behavior[] }));

        moveTo('First talk', 'accepted');

        await waitFor(() => expect(dispatcher.notify).toHaveBeenCalled());
        await waitFor(() => expect((screen.getByLabelText(/First talk/) as HTMLSelectElement).value).toBe('submitted'));
    });

    it('puts the card back when the command was refused', async () => {
        const dispatcher = { executeCommand: vi.fn().mockResolvedValue({ isSuccess: false, validationErrors: [] }), notify: vi.fn() };
        renderBoard(dispatcher);

        moveTo('First talk', 'accepted');

        await waitFor(() => expect(dispatcher.executeCommand).toHaveBeenCalled());
        await waitFor(() => expect((screen.getByLabelText(/First talk/) as HTMLSelectElement).value).toBe('submitted'));
    });

    it('leaves the card where it was dropped when the command was applied', async () => {
        const dispatcher = { executeCommand: vi.fn().mockResolvedValue({ isSuccess: true, validationErrors: [] }), notify: vi.fn() };
        renderBoard(dispatcher);

        moveTo('First talk', 'accepted');

        await waitFor(() => expect(dispatcher.executeCommand).toHaveBeenCalled());
        await new Promise(resolve => setTimeout(resolve, 50));
        expect((screen.getByLabelText(/First talk/) as HTMLSelectElement).value).toBe('accepted');
    });

    it('does not run anything for data that arrives without the user moving a card', () => {
        const dispatcher = { executeCommand: vi.fn(), notify: vi.fn() };
        const outputs = renderBoard(dispatcher);

        expect(outputs.get('value')).toEqual([{ id: 'a', column: 'submitted' }, { id: 'b', column: 'submitted' }]);
        expect(dispatcher.executeCommand).not.toHaveBeenCalled();
    });

    it('is the stock board when the document declares no on change', () => {
        const dispatcher = { executeCommand: vi.fn(), notify: vi.fn() };
        renderBoard(dispatcher, board());

        moveTo('First talk', 'accepted');

        expect(dispatcher.executeCommand).not.toHaveBeenCalled();
    });
});

describe('the change a board reports', () => {
    const before = [{ id: 'a', column: 'submitted' }, { id: 'b', column: 'submitted' }];
    const after = [{ id: 'a', column: 'submitted' }, { id: 'b', column: 'accepted' }];

    it('is the one card whose column differs', () => {
        expect(changedItem(before, after)).toEqual({ item: { id: 'b', column: 'accepted' }, previous: 'submitted' });
    });

    it('is nothing when no card moved', () => expect(changedItem(before, before)).toBeUndefined());

    it('is nothing when the report is not a list', () => expect(changedItem(undefined, after)).toBeUndefined());

    it('is the card as its row with the new column in place', () => {
        const change = itemChangeOf(talks, { item: { id: 'b', column: 'accepted' }, previous: 'submitted' }, 'talkId', 'status')!;
        expect(change.item).toEqual({ talkId: 'b', title: 'Second talk', status: 'accepted' });
        expect(change.value).toBe('accepted');
        expect(change.previous).toBe('submitted');
    });

    it('is nothing for a card the board was given no row for', () => {
        expect(itemChangeOf(talks, { item: { id: 'z', column: 'accepted' }, previous: 'submitted' }, 'talkId', 'status')).toBeUndefined();
    });

    it('binds item fields, the new value and the previous value', () => {
        const change = itemChangeOf(talks, { item: { id: 'b', column: 'accepted' }, previous: 'submitted' }, 'talkId', 'status')!;
        const resolve = resolveChange(change, () => 'fallback');

        expect(resolve({ path: 'item.talkId' } as never)).toBe('b');
        expect(resolve({ path: 'value' } as never)).toBe('accepted');
        expect(resolve({ path: 'previous' } as never)).toBe('submitted');
        expect(resolve({ path: 'something.else' } as never)).toBe('fallback');
    });
});
