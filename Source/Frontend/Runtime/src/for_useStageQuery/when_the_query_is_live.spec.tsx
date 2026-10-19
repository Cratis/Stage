// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { Globals } from '@cratis/arc';
import { StageDataProvider, useStageQuery } from '../stageData';

class FakeEventSource {
    static readonly opened: FakeEventSource[] = [];

    onmessage?: (event: MessageEvent) => void;
    onerror?: () => void;
    closed = false;

    constructor(readonly url: string) {
        FakeEventSource.opened.push(this);
    }

    close() {
        this.closed = true;
    }

    emit(rows: unknown[]) {
        this.onmessage?.({ data: JSON.stringify({ data: rows, isSuccess: true, isValid: true, isAuthorized: true, hasExceptions: false }) } as MessageEvent);
    }
}

function Projects({ route }: { route: string }) {
    const { rows, error } = useStageQuery({ scope: 'projects', route });
    return <output aria-label='projects'>{error || rows.map(row => String(row.name)).join(',')}</output>;
}

function renderProjects(route: string) {
    return render(
        <StageDataProvider routes={undefined} locale='en' locales={['en']} screen='Projects'>
            <Projects route={route} />
        </StageDataProvider>,
    );
}

describe('when the query is live', () => {
    const originalFactory = Globals.eventSourceFactory;

    beforeEach(() => {
        FakeEventSource.opened.length = 0;
        Globals.eventSourceFactory = url => new FakeEventSource(url) as unknown as EventSource;
        vi.stubGlobal('fetch', vi.fn());
    });

    afterEach(() => {
        Globals.eventSourceFactory = originalFactory;
        vi.unstubAllGlobals();
    });

    it('subscribes instead of reading once', async () => {
        renderProjects('/api/projects/live-projects?waitForFirstResult=true');

        await waitFor(() => expect(FakeEventSource.opened).toHaveLength(1));
        expect(fetch).not.toHaveBeenCalled();
        expect(FakeEventSource.opened[0].url).toContain('api/projects/live-projects');
        expect(FakeEventSource.opened[0].url).not.toContain('waitForFirstResult');
    });

    it('shows each result the server pushes without a refresh', async () => {
        renderProjects('/api/projects/live-projects?waitForFirstResult=true');
        await waitFor(() => expect(FakeEventSource.opened).toHaveLength(1));

        act(() => FakeEventSource.opened[0].emit([{ name: 'Screenplay' }]));
        expect((await screen.findByLabelText('projects')).textContent).toBe('Screenplay');

        act(() => FakeEventSource.opened[0].emit([{ name: 'Screenplay' }, { name: 'Stage' }]));
        await waitFor(() => expect(screen.getByLabelText('projects').textContent).toBe('Screenplay,Stage'));
    });

    it('ends the subscription when the list goes away', async () => {
        const { unmount } = renderProjects('/api/projects/live-projects?waitForFirstResult=true');
        await waitFor(() => expect(FakeEventSource.opened).toHaveLength(1));

        unmount();

        expect(FakeEventSource.opened[0].closed).toBe(true);
    });

    it('keeps reading a snapshot query once', async () => {
        vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => ({ data: [{ name: 'Screenplay' }] }) }));

        renderProjects('/api/projects/all-projects');

        expect((await screen.findByLabelText('projects')).textContent).toBe('Screenplay');
        expect(FakeEventSource.opened).toHaveLength(0);
    });
});
