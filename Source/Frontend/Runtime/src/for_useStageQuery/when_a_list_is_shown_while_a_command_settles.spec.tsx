// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, render, screen } from '@testing-library/react';
import { useEffect } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { QueryFor, QueryResult } from '@cratis/arc/queries';
import { registerTestQueries } from '../testing/queries';
import { commandSettleDelays, StageDataProvider, useStageData, useStageQuery } from '../stageData';

// A list opened right after a command adds to it reads before the command's projection has run. It says it is still loading - not
// "No records found" - and shows the row once a later read has it, without being reopened (Stage#720).
function Pets() {
    const { rows, loading, awaitingCommand } = useStageQuery({ scope: 'pets', name: 'Pets' });
    const state = loading || awaitingCommand ? 'loading' : rows.length === 0 ? 'empty' : rows.map(row => String(row.name)).join(',');
    return <output aria-label='pets'>{state}</output>;
}

function AfterCommand({ show }: { show: boolean }) {
    const data = useStageData();
    useEffect(() => data.refreshAfterCommand(), []); // eslint-disable-line react-hooks/exhaustive-deps
    return <><output aria-label='settling'>{String(data.settling)}</output>{show ? <Pets /> : null}</>;
}

function view(show: boolean) {
    return <StageDataProvider routes={undefined} locale='en' locales={['en']} screen='Pets'><AfterCommand show={show} /></StageDataProvider>;
}

async function advance(milliseconds: number) {
    await act(async () => { await vi.advanceTimersByTimeAsync(milliseconds); });
}

beforeEach(() => {
    vi.useFakeTimers();
    registerTestQueries({ Pets: '/pets' });
});
afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
});

describe('when a list is shown while a command settles', () => {
    it('says it is loading while the projection has not caught up', async () => {
        vi.spyOn(QueryFor.prototype, 'perform').mockResolvedValue(QueryResult.empty([]));
        const { rerender } = render(view(false));
        rerender(view(true));
        await advance(4000);
        expect(screen.getByLabelText('pets').textContent).toBe('loading');
    });

    it('shows the row once a later read has it', async () => {
        vi.spyOn(QueryFor.prototype, 'perform')
            .mockResolvedValueOnce(QueryResult.empty([]))
            .mockResolvedValueOnce(QueryResult.empty([]))
            .mockResolvedValue(QueryResult.empty([{ name: 'Fido' }]));
        const { rerender } = render(view(false));
        rerender(view(true));
        await advance(7000);
        expect(screen.getByLabelText('pets').textContent).toBe('Fido');
    });

    it('stops the tail reads when no query is awaiting the command', async () => {
        const perform = vi.spyOn(QueryFor.prototype, 'perform').mockResolvedValue(QueryResult.empty([{ name: 'Fido' }]));
        render(view(true));
        await advance(0);
        const initialReads = perform.mock.calls.length;
        await advance(500);
        expect(perform).toHaveBeenCalledTimes(initialReads + 1);
        await advance(1000);
        expect(perform).toHaveBeenCalledTimes(initialReads + 2);
        await advance(1500);
        expect(perform).toHaveBeenCalledTimes(initialReads + 3);
        expect(screen.getByLabelText('settling').textContent).toBe('false');
        await advance(12000);
        expect(perform).toHaveBeenCalledTimes(initialReads + 3);
    });

    it('continues the tail reads while an empty list awaits the command', async () => {
        const perform = vi.spyOn(QueryFor.prototype, 'perform').mockResolvedValue(QueryResult.empty([]));
        render(view(true));
        await advance(0);
        const initialReads = perform.mock.calls.length;
        await advance(500);
        await advance(1000);
        await advance(1500);
        expect(screen.getByLabelText('settling').textContent).toBe('true');
        await advance(3000);
        expect(perform).toHaveBeenCalledTimes(initialReads + 4);
        expect(screen.getByLabelText('settling').textContent).toBe('true');
        await advance(4000);
        expect(perform).toHaveBeenCalledTimes(initialReads + 5);
        expect(screen.getByLabelText('settling').textContent).toBe('true');
        await advance(5000);
        expect(perform).toHaveBeenCalledTimes(initialReads + 6);
        expect(screen.getByLabelText('settling').textContent).toBe('false');
    });

    it('says it is empty once the command has settled', async () => {
        vi.spyOn(QueryFor.prototype, 'perform').mockResolvedValue(QueryResult.empty([]));
        const { rerender } = render(view(false));
        rerender(view(true));
        await advance(commandSettleDelays[commandSettleDelays.length - 1] + 100);
        expect(screen.getByLabelText('pets').textContent).toBe('empty');
    });

    it('says at once that a list emptied by the command is empty', async () => {
        vi.spyOn(QueryFor.prototype, 'perform')
            .mockResolvedValueOnce(QueryResult.empty([{ name: 'Fido' }]))
            .mockResolvedValue(QueryResult.empty([]));
        render(<StageDataProvider routes={undefined} locale='en' locales={['en']} screen='Pets'><Pets /><Command /></StageDataProvider>);
        await advance(10);
        expect(screen.getByLabelText('pets').textContent).toBe('Fido');
        await act(async () => { screen.getByRole('button').click(); await vi.advanceTimersByTimeAsync(600); });
        expect(screen.getByLabelText('pets').textContent).toBe('empty');
    });
});

function Command() {
    const data = useStageData();
    return <button type='button' onClick={() => data.refreshAfterCommand()}>Remove</button>;
}
