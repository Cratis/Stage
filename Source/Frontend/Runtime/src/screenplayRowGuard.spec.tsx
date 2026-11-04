// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { ActionDispatcher } from '@cratis/scene.engine';
import { InteractionScope } from '@cratis/scene.react';
import { InteractionTriggerKind, type Behavior, type ExternalComponent } from '@cratis/scene.model';
import { useRowInteractions } from './stageRowInteractions';
import { element } from './testElements';

const cases = [
    { name: 'array containing text', item: { value: ['open'] }, operator: 'Contains', right: 'open', result: 'refused' },
    { name: 'number field', item: { value: 123 }, operator: 'Contains', right: '2', result: 'refused' },
    { name: 'Boolean field', item: { value: true }, operator: 'Contains', right: 'true', result: 'refused' },
    { name: 'object field', item: { value: {} }, operator: 'Contains', right: 'open', result: 'refused' },
    { name: 'null field', item: { value: null }, operator: 'Contains', right: 'open', result: 'refused' },
    { name: 'absent field', item: {}, operator: 'Contains', right: 'open', result: 'refused' },
    { name: 'matching string', item: { value: 'reopened' }, operator: 'Contains', right: 'open', result: 'first' },
    { name: 'nonmatching string', item: { value: 'closed' }, operator: 'Contains', right: 'open', result: 'otherwise' },
    { name: 'explicit null equality', item: { value: null }, operator: 'Equal', right: null, result: 'first' },
    { name: 'absent equality', item: {}, operator: 'Equal', right: null, result: 'otherwise' },
    { name: 'absent inequality', item: {}, operator: 'NotEqual', right: null, result: 'otherwise' },
    { name: 'nested explicit null', item: { nested: { value: null } }, operator: 'Equal', right: null, result: 'first', subject: 'item.nested.value' },
    { name: 'nested absent equality', item: { nested: {} }, operator: 'Equal', right: null, result: 'otherwise', subject: 'item.nested.value' },
    { name: 'nested absent inequality', item: { nested: {} }, operator: 'NotEqual', right: null, result: 'otherwise', subject: 'item.nested.value' },
];
const execute = (command: string) => ({ kind: 'ExecuteCommand', command, arguments: [], onSuccess: [], onFailure: [], onResult: [] });

function Row({ table, item }: { table: ExternalComponent; item: Record<string, unknown> }) {
    const handlers = useRowInteractions(table);
    return <button onDoubleClick={() => handlers.doubleClick?.(item)}>Row</button>;
}

describe('Screenplay guards on the Stage-owned row double-click path', () => {
    it.each(cases)('preserves $name', async ({ item, operator, right, result, subject }) => {
        const condition = { kind: 'comparison', left: { subject: subject ?? 'item.value' }, operator, right };
        const behaviors = [{ bindings: [{ trigger: { kind: InteractionTriggerKind.DoubleClick }, alternatives: [
            { condition, actions: [execute('First')] },
            { condition, actions: [execute('Second')] },
        ], otherwise: [execute('Otherwise')] }] }] as unknown as Behavior[];
        const table = { ...element('table', 'core:table'), behaviors };
        const dispatcher = { executeCommand: vi.fn().mockResolvedValue({ isSuccess: true }) };
        const findings = vi.fn();
        render(<InteractionScope dispatcher={dispatcher as unknown as ActionDispatcher} attachments={[]} context={{ resolve: () => undefined }} onFindings={findings}>
            <Row table={table} item={item} />
        </InteractionScope>);

        fireEvent.doubleClick(screen.getByRole('button', { name: 'Row' }));

        if (result === 'refused') {
            await waitFor(() => expect(findings).toHaveBeenCalledWith(expect.arrayContaining([expect.objectContaining({ kind: 'unevaluableCondition' })])));
            expect(dispatcher.executeCommand).not.toHaveBeenCalled();
        } else {
            await waitFor(() => expect(dispatcher.executeCommand).toHaveBeenCalledTimes(1));
            expect(dispatcher.executeCommand.mock.calls[0][0]).toBe(result === 'first' ? 'First' : 'Otherwise');
            expect(findings).not.toHaveBeenCalled();
        }
    });
});
