// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ObservableQueryFor, ObservableQuerySubscription, QueryResult } from '@cratis/arc/queries';
import { PrimeReactProvider } from '@primereact/core';
import { RegisteredTable } from '../testing/queries';
import { StageDataProvider } from '../stageData';
import { element } from '../testElements';

let deliver: (result: QueryResult<unknown>) => void;

function list(owner: string) {
    return (
        <PrimeReactProvider>
            <StageDataProvider routes={{ commands: {}, queries: { Projects: '/projects?waitForFirstResult=true' } }} locale='en' locales={['en']} screen='Projects'>
                <RegisteredTable element={element('projects', 'Stage:table', { query: 'Projects', label: 'Projects', queryArguments: { owner }, columns: [{ property: 'name', label: 'Name' }] })} slots={{}} />
            </StageDataProvider>
        </PrimeReactProvider>
    );
}
const renderList = () => render(list('a'));

beforeEach(() => {
    vi.spyOn(ObservableQueryFor.prototype, 'subscribe').mockImplementation(callback => {
        deliver = callback;
        return new ObservableQuerySubscription({ connect: vi.fn(), disconnect: vi.fn(), lastPingLatency: 0, averageLatency: 0 });
    });
});
afterEach(() => vi.restoreAllMocks());

describe('when a live list answers', () => {
    it('shows the loading message until the first answer, then no longer once rows render', () => {
        renderList();
        expect(screen.queryByText('Loading…')).not.toBeNull();

        act(() => deliver(QueryResult.empty([{ name: 'Screenplay' }])));

        expect(screen.queryByText('Screenplay')).not.toBeNull();
        expect(screen.queryByText('Loading…')).toBeNull();
    });
    it('shows no loading message when the first answer is empty', () => {
        renderList();

        act(() => deliver(QueryResult.empty([])));

        expect(screen.queryByText('Loading…')).toBeNull();
        expect(screen.getByRole('table')).not.toBeNull();
    });
    it('shows no loading message when the first answer is an authorization failure', () => {
        renderList();

        act(() => deliver(QueryResult.unauthorized()));

        expect(screen.queryByText('Loading…')).toBeNull();
        expect(screen.queryByText('The query answered 403.')).not.toBeNull();
    });
    it('keeps the rows without a loading message while a changed read has not answered yet', () => {
        const { rerender } = renderList();
        act(() => deliver(QueryResult.empty([{ name: 'Screenplay' }])));

        rerender(list('b'));

        expect(screen.queryByText('Screenplay')).not.toBeNull();
        expect(screen.queryByText('Loading…')).toBeNull();
    });
    it('shows no loading message once rows have rendered after a not-yet-ready delivery', () => {
        renderList();

        act(() => deliver({ ...QueryResult.empty([]), isReady: false } as QueryResult<unknown>));
        act(() => deliver(QueryResult.empty([{ name: 'Screenplay' }])));

        expect(screen.queryByText('Loading…')).toBeNull();
    });
});
