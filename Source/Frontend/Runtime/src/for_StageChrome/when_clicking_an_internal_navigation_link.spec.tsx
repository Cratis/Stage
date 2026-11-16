// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ContextType } from 'react';
import { DestinationKind, type ExternalComponent } from '@cratis/scene.model';
import { SceneNavigationContextInternal } from '@cratis/scene.react';
import { cleanup, createEvent, fireEvent, render } from '@testing-library/react';
import { afterEach, beforeEach, chai, describe, it, vi } from 'vitest';
import { StageNavigationTree } from '../StageChrome';
import type { NavigationNode } from '../navigationTree';
import { useStageRouting } from '../stageNavigation';

chai.should();

function renderEntry(destination?: NavigationNode['destination'], navigate?: ReturnType<typeof vi.fn>) {
    const nodes: NavigationNode[] = [
        { label: 'A', screen: 'A', children: [] },
        { label: 'X', screen: 'X', children: [], destination },
    ];
    const element = { id: 'navigation', componentName: 'Stage:navigation-tree', properties: { nodes, activeScreen: 'A' }, slots: {} } as unknown as ExternalComponent;
    const tree = <StageNavigationTree element={element} slots={{}} />;
    const navigation = { navigate } as unknown as NonNullable<ContextType<typeof SceneNavigationContextInternal>>;
    return render(navigate
        ? <SceneNavigationContextInternal.Provider value={navigation}>{tree}</SceneNavigationContextInternal.Provider>
        : tree).getByRole('link', { name: 'X' });
}

// Observe cancellation after React handles the click, then suppress jsdom's unsupported full-page navigation.
function clickWithoutBrowserNavigation(entry: HTMLElement, options: MouseEventInit = {}) {
    let intercepted = false;
    window.addEventListener('click', event => {
        intercepted = event.defaultPrevented;
        event.preventDefault();
    }, { once: true });
    fireEvent.click(entry, options);
    return intercepted;
}

beforeEach(() => {
    useStageRouting('path');
    globalThis.history.replaceState(null, '', '/A');
});

afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
    useStageRouting('hash');
    globalThis.history.replaceState(null, '', '/');
});

describe('when clicking an internal navigation link', () => {
    it('should address the screen by pathname in path mode', () => {
        renderEntry().getAttribute('href')!.should.equal('/X');
    });

    it('should change the pathname and notify the navigation host once without Scene context', () => {
        const popped = vi.fn();
        window.addEventListener('popstate', popped);
        try {
            fireEvent.click(renderEntry()).should.be.false;

            globalThis.location.pathname.should.equal('/X');
            globalThis.location.hash.should.equal('');
            popped.mock.calls.length.should.equal(1);
        } finally {
            window.removeEventListener('popstate', popped);
        }
    });

    it('should keep hash addressing and navigation in hash mode', () => {
        useStageRouting('hash');
        const entry = renderEntry();

        entry.getAttribute('href')!.should.equal('#/X');
        fireEvent.click(entry);

        globalThis.location.pathname.should.equal('/A');
        globalThis.location.hash.should.equal('#/X');
    });

    it.each([
        { ctrlKey: true },
        { metaKey: true },
        { shiftKey: true },
        { altKey: true },
        { button: 1 },
    ])('should leave the browser in charge of a non-plain click %j', options => {
        const intercepted = clickWithoutBrowserNavigation(renderEntry(), options);

        intercepted.should.be.false;
        globalThis.location.pathname.should.equal('/A');
    });

    it('should leave an already cancelled click alone', () => {
        const entry = renderEntry();
        const event = createEvent.click(entry);
        event.preventDefault();
        const preventDefault = vi.spyOn(event, 'preventDefault');

        fireEvent(entry, event);

        preventDefault.mock.calls.length.should.equal(0);
        globalThis.location.pathname.should.equal('/A');
    });

    it('should use path navigation for an ordinary screen even with a Scene host', () => {
        const navigate = vi.fn();

        fireEvent.click(renderEntry({ screen: 'X' }, navigate));

        navigate.mock.calls.length.should.equal(0);
        globalThis.location.pathname.should.equal('/X');
        globalThis.location.hash.should.equal('');
    });

    it('should leave an external route to the browser even with a Scene host', () => {
        const navigate = vi.fn();
        const entry = renderEntry({ kind: DestinationKind.External, route: 'https://example.com/docs' }, navigate);

        entry.getAttribute('href')!.should.equal('https://example.com/docs');
        clickWithoutBrowserNavigation(entry).should.be.false;
        navigate.mock.calls.length.should.equal(0);
        globalThis.location.pathname.should.equal('/A');
    });

    it.each([
        { screen: 'X', slot: 'side' },
        { kind: DestinationKind.Dialog, dialog: 'HelpDialog' },
    ])('should keep Scene navigation for a destination opening elsewhere %j', destination => {
        const navigate = vi.fn();
        const intercepted = clickWithoutBrowserNavigation(renderEntry(destination, navigate));

        intercepted.should.be.true;
        navigate.mock.calls.should.deep.equal([[destination]]);
        globalThis.location.pathname.should.equal('/A');
    });
});
