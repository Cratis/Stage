// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ObservableQueryFor, ObservableQuerySubscription, QueryResult } from '@cratis/arc/queries';
import { registerTestQueries } from '../testing/queries';
import { StageDataProvider, useStageQuery } from '../stageData';

let deliver: (result: QueryResult<unknown>) => void;
const unsubscribe = vi.fn();
function Projects() {
    const { rows, error, refresh } = useStageQuery({ scope: 'projects', name: 'Projects' });
    return <><output aria-label='projects'>{error || rows.map(row => String(row.name)).join(',')}</output><button onClick={refresh}>refresh</button></>;
}
function renderProjects() {
    return render(<StageDataProvider routes={undefined} locale='en' locales={['en']} screen='Projects'><Projects /></StageDataProvider>);
}

beforeEach(() => {
    unsubscribe.mockClear();
    registerTestQueries({ Projects: '/projects?waitForFirstResult=true' });
    vi.spyOn(ObservableQueryFor.prototype, 'subscribe').mockImplementation(callback => {
        deliver = callback;
        return new ObservableQuerySubscription({ connect: vi.fn(), disconnect: unsubscribe, lastPingLatency: 0, averageLatency: 0 });
    });
});
afterEach(() => vi.restoreAllMocks());

describe('when the query is live', () => {
    it('shows each typed result without requiring a route or a refresh', () => {
        renderProjects();
        act(() => deliver(QueryResult.empty([{ name: 'Screenplay' }])));
        expect(screen.getByLabelText('projects').textContent).toBe('Screenplay');
        act(() => deliver(QueryResult.empty([{ name: 'Stage' }])));
        expect(screen.getByLabelText('projects').textContent).toBe('Stage');
    });
    it('unsubscribes when the list goes away and ignores late deliveries', () => {
        const { unmount } = renderProjects();
        unmount();
        expect(unsubscribe).toHaveBeenCalledOnce();
        act(() => deliver(QueryResult.empty([{ name: 'Late' }])));
        expect(screen.queryByText('Late')).toBeNull();
    });
    it('keeps the subscription open on refresh', () => {
        renderProjects();
        fireEvent.click(screen.getByText('refresh'));
        expect(ObservableQueryFor.prototype.subscribe).toHaveBeenCalledOnce();
        expect(unsubscribe).not.toHaveBeenCalled();
    });
});
