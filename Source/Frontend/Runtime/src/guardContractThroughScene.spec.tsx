// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { InteractionTriggerKind, type Behavior } from '@cratis/scene.model';
import type { ActionDispatcher } from '@cratis/scene.engine';
import { InteractionScope, SceneElementView, type ComponentBindingOutputs } from '@cratis/scene.react';
import { PrimeReactProvider } from '@primereact/core';
import { stageApplicationRegistry } from './App';
import { StageDataProvider, useStageData } from './stageData';
import { bindDataSources } from './stageDataSources';
import { StageKanbanBoard } from './stageItemChange';
import { element } from './testElements';
import { queryResult, registerTestQueries } from './testing/queries';

const cases: Array<{ name: string; item: Record<string, unknown>; operator: string; right: unknown; allowed: boolean; unevaluable: boolean; subject?: string; logical?: boolean }> = [
    { name: 'array containing a number', item: { value: [1] }, operator: 'Contains', right: 1, allowed: false, unevaluable: true },
    { name: 'array containing text', item: { value: ['open'] }, operator: 'Contains', right: 'open', allowed: false, unevaluable: true },
    { name: 'number field', item: { value: 123 }, operator: 'Contains', right: '2', allowed: false, unevaluable: true },
    { name: 'Boolean field', item: { value: true }, operator: 'Contains', right: 'true', allowed: false, unevaluable: true },
    { name: 'object field', item: { value: {} }, operator: 'Contains', right: 'open', allowed: false, unevaluable: true },
    { name: 'null field', item: { value: null }, operator: 'Contains', right: 'open', allowed: false, unevaluable: true },
    { name: 'absent text field', item: {}, operator: 'Contains', right: 'open', allowed: false, unevaluable: true },
    { name: 'numeric literal', item: { value: '123' }, operator: 'Contains', right: 2, allowed: false, unevaluable: true },
    { name: 'Boolean literal', item: { value: 'true' }, operator: 'Contains', right: true, allowed: false, unevaluable: true },
    { name: 'null literal', item: { value: 'null' }, operator: 'Contains', right: null, allowed: false, unevaluable: true },
    { name: 'array in a true disjunction', item: { value: ['open'] }, operator: 'Contains', right: 'open', allowed: false, unevaluable: true, logical: true },
    { name: 'text containing text', item: { value: 'reopened' }, operator: 'Contains', right: 'open', allowed: true, unevaluable: false },
    { name: 'text not containing text', item: { value: 'closed' }, operator: 'Contains', right: 'open', allowed: false, unevaluable: false },
    { name: 'explicit null', item: { value: null }, operator: 'Equal', right: null, allowed: true, unevaluable: false },
    { name: 'absent field compared to null', item: {}, operator: 'Equal', right: null, allowed: false, unevaluable: false },
    { name: 'absent field not equal to null', item: {}, operator: 'NotEqual', right: null, allowed: false, unevaluable: false },
    { name: 'explicit nested null', item: { nested: { value: null } }, operator: 'Equal', right: null, allowed: true, unevaluable: false, subject: 'item.nested.value' },
    { name: 'absent nested field', item: { nested: {} }, operator: 'Equal', right: null, allowed: false, unevaluable: false, subject: 'item.nested.value' },
    { name: 'absent parent', item: {}, operator: 'Equal', right: null, allowed: false, unevaluable: false, subject: 'item.nested.value' },
];

const routes = { queries: { GuardSubject: '/api/guard-subject' }, commands: {} };
const execute = (command: string) => ({ kind: 'ExecuteCommand', command, arguments: [], onSuccess: [], onFailure: [], onResult: [] });
function guard(operator: string, right: unknown, subject = 'item.value', logical = false): unknown {
    const condition = { kind: 'comparison', left: { subject }, operator, right };
    return logical ? { kind: 'logical', operator: 'Or', left: { kind: 'comparison', left: { subject: 'item.id' }, operator: 'Equal', right: 'subject' }, right: condition } : condition;
}

function GuardedAction({ condition }: { condition: unknown }) {
    const data = useStageData();
    const tree = bindDataSources(element('screen', 'core:section', {}, { content: [
        element('subject', 'core:data', { typeName: 'Subject', query: 'GuardSubject', isCollection: false }),
        element('process', 'core:guarded-action', {
            label: 'Process', alternatives: [{ command: 'ApplyChange', condition, arguments: {} }],
            otherwise: { outcome: 'Execute', command: 'Fallback', arguments: {} },
        }),
    ] }));
    return <SceneElementView element={tree} registry={stageApplicationRegistry()} resolveBinding={data.resolveBinding} />;
}

describe('the Screenplay guard contract through native Scene', () => {
    afterEach(() => { vi.unstubAllGlobals(); vi.restoreAllMocks(); });

    it.each(cases)('guarded actions preserve $name', async ({ item, operator, right, allowed, unevaluable, subject, logical }) => {
        registerTestQueries(routes.queries);
        vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => queryResult({ data: { id: 'subject', ...item } }) }));
        const dispatcher = { executeCommand: vi.fn<(command: string) => Promise<{ isSuccess: boolean }>>().mockResolvedValue({ isSuccess: true }) };
        const condition = guard(operator, right, subject, logical);
        render(<PrimeReactProvider><StageDataProvider routes={routes} locale='en' locales={['en']} screen='Subject'>
            <InteractionScope dispatcher={dispatcher as unknown as ActionDispatcher} attachments={[]}>
                <GuardedAction condition={condition} />
            </InteractionScope>
        </StageDataProvider></PrimeReactProvider>);

        const button = await screen.findByRole('button', { name: 'Process' });
        expect((button as HTMLButtonElement).disabled).toBe(unevaluable);
        fireEvent.click(button);
        if (unevaluable) {
            expect(screen.getByRole('alert').textContent).toContain('SCENE-GUARD-001');
            expect(dispatcher.executeCommand).not.toHaveBeenCalled();
        } else {
            await waitFor(() => expect(dispatcher.executeCommand).toHaveBeenCalledTimes(1));
            expect(dispatcher.executeCommand.mock.calls[0][0]).toBe(allowed ? 'ApplyChange' : 'Fallback');
        }
    });

    it.each(cases)('item-change preserves $name', async ({ item, operator, right, allowed, unevaluable, subject, logical }) => {
        const dispatcher = { executeCommand: vi.fn().mockResolvedValue({ isSuccess: true }), notify: vi.fn() };
        const findings = vi.fn();
        const condition = guard(operator, right, subject, logical);
        const behaviors = [{ bindings: [{ trigger: { kind: InteractionTriggerKind.Change }, alternatives: [
            { condition, actions: [execute('ApplyChange')] },
        ], otherwise: [{ kind: 'Notify', level: 'warning', message: { text: 'Not allowed' } }] }] }] as unknown as Behavior[];
        const board = { ...element('board', 'Cratis.Blueprint.Default.kanbanBoard', {
            columns: ['submitted', 'accepted'].map(id => ({ id, label: id })),
            cards: [{ id: 'subject', title: 'First item', status: 'submitted', ...item }],
            cardId: 'id', cardTitle: 'title', cardColumn: 'status', editPermission: 'enabled',
        }), behaviors };
        const outputs = { setOutput: vi.fn() } as unknown as ComponentBindingOutputs;
        render(<PrimeReactProvider><StageDataProvider routes={{ commands: {}, queries: {} }} locale='en' locales={['en']} screen='Board'>
            <InteractionScope dispatcher={dispatcher as unknown as ActionDispatcher} context={{ resolve: () => undefined }} attachments={[]} onFindings={findings}>
                <StageKanbanBoard element={board} slots={{}} bindingOutputs={outputs} />
            </InteractionScope>
        </StageDataProvider></PrimeReactProvider>);

        fireEvent.change(screen.getByLabelText(/First item/), { target: { value: 'accepted' } });

        await waitFor(() => expect((screen.getByLabelText(/First item/) as HTMLSelectElement).value).toBe(allowed ? 'accepted' : 'submitted'));
        expect(dispatcher.executeCommand).toHaveBeenCalledTimes(allowed ? 1 : 0);
        expect(dispatcher.notify).toHaveBeenCalledTimes(!allowed && !unevaluable ? 1 : 0);
        if (unevaluable) expect(findings).toHaveBeenCalledWith(expect.arrayContaining([expect.objectContaining({ kind: 'unevaluableCondition' })]));
    });
});
