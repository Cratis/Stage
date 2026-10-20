// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { QueryFor, QueryResult } from '@cratis/arc/queries';
import { queryResult, registerTestQueries } from '../testing/queries';
import { StageDataProvider, useStageQuery } from '../stageData';

function Projects({ id, name = 'Projects' }: { id?: string; name?: string }) {
    const { rows, error, loading } = useStageQuery({ scope: 'projects', name, ready: !!id, arguments: { id, filter: 'active' } });
    return <><output aria-label='projects'>{error || rows.map(row => String(row.name)).join(',')}</output><output aria-label='loading'>{String(loading)}</output></>;
}
function view(id?: string, name?: string) {
    return <StageDataProvider routes={undefined} locale='en' locales={['en']} screen='Projects'><Projects id={id} name={name} /></StageDataProvider>;
}
beforeEach(() => registerTestQueries({ Projects: '/projects/{id}' }));
afterEach(() => { vi.restoreAllMocks(); vi.unstubAllGlobals(); });

describe('when performing a registered query', () => {
    it('waits for required arguments, then performs without routes.queries', async () => {
        const perform = vi.spyOn(QueryFor.prototype, 'perform').mockResolvedValue(QueryResult.empty([{ name: 'Stage' }]));
        const { rerender } = render(view());
        expect(perform).not.toHaveBeenCalled();
        rerender(view('one'));
        await waitFor(() => expect(screen.getByLabelText('projects').textContent).toBe('Stage'));
        expect(perform).toHaveBeenCalledWith({ id: 'one', filter: 'active' });
    });
    it('does not fetch the route when the modeled name is unregistered', async () => {
        const fetch = vi.fn();
        vi.stubGlobal('fetch', fetch);
        render(view('one', 'Missing'));
        await waitFor(() => expect(screen.getByLabelText('projects').textContent).toContain('not registered'));
        expect(fetch).not.toHaveBeenCalled();
    });
    it('keeps a domain row that has its own data property intact', async () => {
        vi.spyOn(QueryFor.prototype, 'perform').mockResolvedValue(QueryResult.empty({ name: 'Stage', data: 'payload' }));
        render(view('one'));
        await waitFor(() => expect(screen.getByLabelText('projects').textContent).toBe('Stage'));
    });
    it('clears rows on a typed authorization failure', async () => {
        vi.spyOn(QueryFor.prototype, 'perform').mockResolvedValue(QueryResult.unauthorized());
        render(view('one'));
        await waitFor(() => expect(screen.getByLabelText('projects').textContent).toBe('The query answered 403.'));
        expect(screen.getByLabelText('loading').textContent).toBe('false');
    });
    it('preserves the failure text for a typed validation failure', async () => {
        const result = new QueryResult(queryResult({ isValid: false, isSuccess: false }), Object, false);
        vi.spyOn(QueryFor.prototype, 'perform').mockResolvedValue(result);
        render(view('one'));
        await waitFor(() => expect(screen.getByLabelText('projects').textContent).toBe('The query could not be answered.'));
    });
    it('does not publish a transient not-ready result as rows', async () => {
        const result = new QueryResult(queryResult({ isReady: false, data: [{ name: 'Transient' }] }), Object, false);
        const perform = vi.spyOn(QueryFor.prototype, 'perform').mockResolvedValue(result);
        render(view('one'));
        await waitFor(() => expect(perform).toHaveBeenCalledOnce());
        expect(screen.getByLabelText('projects').textContent).toBe('');
        expect(screen.getByLabelText('loading').textContent).toBe('true');
    });
    it('ignores an old result after arguments change', async () => {
        let complete: (result: QueryResult<unknown>) => void = () => undefined;
        const old = new Promise<QueryResult<unknown>>(resolve => { complete = resolve; });
        vi.spyOn(QueryFor.prototype, 'perform').mockReturnValueOnce(old).mockResolvedValue(QueryResult.empty([{ name: 'Current' }]));
        const { rerender } = render(view('one'));
        rerender(view('two'));
        await waitFor(() => expect(screen.getByLabelText('projects').textContent).toBe('Current'));
        await act(async () => { complete(QueryResult.empty([{ name: 'Stale' }])); await old; });
        expect(screen.getByLabelText('projects').textContent).toBe('Current');
    });
});
