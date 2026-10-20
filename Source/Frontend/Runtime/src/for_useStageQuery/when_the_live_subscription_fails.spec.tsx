// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ObservableQueryFor, ObservableQuerySubscription, QueryResult } from '@cratis/arc/queries';
import { registerTestQueries } from '../testing/queries';
import { StageDataProvider, useStageQuery } from '../stageData';

let deliver: (result: QueryResult<unknown>) => void;
const unsubscribe = vi.fn();
function Projects() {
    const { rows, error, loading } = useStageQuery({ scope: 'projects', name: 'Projects' });
    return <><output aria-label='projects'>{error || rows.map(row => String(row.name)).join(',')}</output><output aria-label='loading'>{String(loading)}</output></>;
}
function renderProjects() {
    return render(<StageDataProvider routes={undefined} locale='en' locales={['en']} screen='Projects'><Projects /></StageDataProvider>);
}

beforeEach(() => {
    vi.useFakeTimers();
    unsubscribe.mockClear();
    registerTestQueries({ Projects: '/projects?waitForFirstResult=true' });
    vi.spyOn(ObservableQueryFor.prototype, 'subscribe').mockImplementation(callback => {
        deliver = callback;
        return new ObservableQuerySubscription({ connect: vi.fn(), disconnect: unsubscribe, lastPingLatency: 0, averageLatency: 0 });
    });
});
afterEach(() => { vi.useRealTimers(); vi.restoreAllMocks(); });

describe('when the live subscription fails', () => {
    it('reads once through Arc after the first-result deadline and surfaces rejection', async () => {
        const perform = vi.spyOn(ObservableQueryFor.prototype, 'perform').mockResolvedValue(QueryResult.unauthorized());
        renderProjects();
        await act(() => vi.advanceTimersByTimeAsync(15000));
        expect(perform).toHaveBeenCalledOnce();
        expect(perform).toHaveBeenCalledWith({ waitForFirstResult: true });
        expect(unsubscribe).toHaveBeenCalledOnce();
        expect(screen.getByLabelText('projects').textContent).toBe('The query answered 403.');
        expect(screen.getByLabelText('loading').textContent).toBe('false');
    });
    it('leaves reconnection to Arc after the first ready result', async () => {
        const perform = vi.spyOn(ObservableQueryFor.prototype, 'perform');
        renderProjects();
        act(() => deliver(QueryResult.empty([{ name: 'Screenplay' }])));
        await act(() => vi.advanceTimersByTimeAsync(30000));
        expect(perform).not.toHaveBeenCalled();
        expect(screen.getByLabelText('projects').textContent).toBe('Screenplay');
    });
    it('clears stale rows when Arc delivers an authorization rejection', () => {
        renderProjects();
        act(() => deliver(QueryResult.empty([{ name: 'Screenplay' }])));
        act(() => deliver(QueryResult.unauthorized()));
        expect(screen.getByLabelText('projects').textContent).toBe('The query answered 403.');
    });
    it('cancels the first-result deadline on unmount', async () => {
        const perform = vi.spyOn(ObservableQueryFor.prototype, 'perform');
        const { unmount } = renderProjects();
        unmount();
        await act(() => vi.advanceTimersByTimeAsync(15000));
        expect(perform).not.toHaveBeenCalled();
        expect(unsubscribe).toHaveBeenCalledOnce();
    });
});
