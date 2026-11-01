// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { Screen } from '@cratis/scene.model';

vi.mock('@cratis/showrunner.notes.react', () => ({ NotesButton: () => <button type='button'>Give feedback</button> }));

const { StageChromeProvider, StageNotes, stageChromeComponents } = await import('../StageChrome');
const { applicationChrome, stageLocaleComponent, stageNotesComponent, stageThemeToggleComponent } = await import('../blueprint');

const state = (notesEnabled: boolean) => ({ locales: [], locale: '', setLocale: () => undefined, activity: '', notesEnabled });

describe('when placing the feedback button', () => {
    it('shows it in the topbar when the host serves notes', () => {
        const view = render(<StageChromeProvider state={state(true)}><StageNotes /></StageChromeProvider>);
        expect(view.queryByText('Give feedback')).not.toBeNull();
    });

    it('does not show it when the host does not serve notes', () => {
        const view = render(<StageChromeProvider state={state(false)}><StageNotes /></StageChromeProvider>);
        expect(view.queryByText('Give feedback')).toBeNull();
    });

    it('registers it as a chrome component', () => {
        expect(stageChromeComponents[stageNotesComponent]).toBe(StageNotes);
    });

    it('puts it first in the end of the topbar, before the locale and theme', () => {
        const screens = [{ name: 'Home', layout: 'L', slotContent: {}, forms: [], contributions: [] }] as unknown as Screen[];
        const chrome = applicationChrome(screens, 'Home', ['en', 'nb'], 'en');
        const topbar = (chrome.topbar ?? Object.values(chrome)[0])[0] as unknown as { slots: Record<string, { componentName: string }[]> };
        expect(topbar.slots.end.map(entry => entry.componentName)).toEqual([stageNotesComponent, stageLocaleComponent, stageThemeToggleComponent]);
    });
});
