// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { Globals } from '@cratis/arc';
import { StageDataProvider, useStageData, useStageQuery } from '../stageData';

const route = '/api/projects/live-projects?waitForFirstResult=true';

class FakeEventSource {
    static readonly opened: FakeEventSource[] = [];

    onmessage?: (event: MessageEvent) => void;
    onerror?: (event: Event) => void;
    readyState = 0;
    closed = false;

    constructor(readonly url: string) {
        FakeEventSource.opened.push(this);
    }

    close() {
        this.closed = true;
        this.readyState = 2;
    }

    // The browser gives up on a subscription the server refused: the stream is CLOSED and nothing retries.
    refuse() {
        this.readyState = 2;
        this.onerror?.({} as Event);
    }

    // A dropped connection the browser is about to retry on its own.
    drop() {
        this.readyState = 0;
        this.onerror?.({} as Event);
    }

    emit(rows: unknown[]) {
        this.onmessage?.({ data: JSON.stringify({ data: rows, isSuccess: true, isValid: true, isAuthorized: true, hasExceptions: false }) } as MessageEvent);
    }
}

function Projects() {
    const { rows, error, loading, refresh } = useStageQuery({ scope: 'projects', route });
    const { refreshAfterCommand } = useStageData();
    return (
        <>
            <output aria-label='projects'>{error || rows.map(row => String(row.name)).join(',')}</output>
            <output aria-label='loading'>{String(loading)}</output>
            <button onClick={refresh}>refresh</button>
            <button onClick={refreshAfterCommand}>command</button>
        </>
    );
}

function renderProjects() {
    return render(
        <StageDataProvider routes={undefined} locale='en' locales={['en']} screen='Projects'>
            <Projects />
        </StageDataProvider>,
    );
}

describe('when the live subscription fails', () => {
    const originalFactory = Globals.eventSourceFactory;

    beforeEach(() => {
        FakeEventSource.opened.length = 0;
        Globals.eventSourceFactory = url => new FakeEventSource(url) as unknown as EventSource;
        vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: false, status: 501 }));
    });

    afterEach(() => {
        Globals.eventSourceFactory = originalFactory;
        vi.unstubAllGlobals();
    });

    it('shows the error when the connection is closed instead of loading forever', async () => {
        renderProjects();
        await waitFor(() => expect(FakeEventSource.opened).toHaveLength(1));
        expect(screen.getByLabelText('loading').textContent).toBe('true');

        act(() => FakeEventSource.opened[0].refuse());

        await waitFor(() => expect(screen.getByLabelText('projects').textContent).toBe('The query answered 501.'));
        expect(screen.getByLabelText('loading').textContent).toBe('false');
        expect(fetch).toHaveBeenCalledTimes(1);
        expect((fetch as ReturnType<typeof vi.fn>).mock.calls[0][0]).toContain('waitForFirstResult=true');
    });

    it('shows why a refused subscription was refused', async () => {
        vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: false, status: 403 }));
        renderProjects();
        await waitFor(() => expect(FakeEventSource.opened).toHaveLength(1));

        act(() => FakeEventSource.opened[0].refuse());

        await waitFor(() => expect(screen.getByLabelText('projects').textContent).toBe('The query answered 403.'));
    });

    it('replaces rows that went stale when a reconnect is refused', async () => {
        renderProjects();
        await waitFor(() => expect(FakeEventSource.opened).toHaveLength(1));
        act(() => FakeEventSource.opened[0].emit([{ name: 'Screenplay' }]));
        await waitFor(() => expect(screen.getByLabelText('projects').textContent).toBe('Screenplay'));

        act(() => FakeEventSource.opened[0].refuse());

        await waitFor(() => expect(screen.getByLabelText('projects').textContent).toBe('The query answered 501.'));
    });

    it('does not read the route while the browser is still retrying', async () => {
        renderProjects();
        await waitFor(() => expect(FakeEventSource.opened).toHaveLength(1));

        act(() => FakeEventSource.opened[0].drop());

        expect(fetch).not.toHaveBeenCalled();
        expect(screen.getByLabelText('loading').textContent).toBe('true');
    });

    it('reads the route when the browser has no EventSource', async () => {
        Globals.eventSourceFactory = undefined;
        vi.stubGlobal('EventSource', undefined);

        renderProjects();

        await waitFor(() => expect(screen.getByLabelText('projects').textContent).toBe('The query answered 501.'));
        expect(screen.getByLabelText('loading').textContent).toBe('false');
    });

    it('keeps an open stream when the list is refreshed', async () => {
        renderProjects();
        await waitFor(() => expect(FakeEventSource.opened).toHaveLength(1));

        fireEvent.click(screen.getByText('refresh'));
        fireEvent.click(screen.getByText('command'));

        expect(FakeEventSource.opened).toHaveLength(1);
        expect(FakeEventSource.opened[0].closed).toBe(false);
    });
});
