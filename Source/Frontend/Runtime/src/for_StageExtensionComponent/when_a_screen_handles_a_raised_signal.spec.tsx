// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render, fireEvent, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { reactComponent, useRaise } from '@cratis/scene.extensions';
import { InteractionScope } from '@cratis/scene.react';
import type { ActionDispatcher } from '@cratis/scene.engine';
import type { Behavior, ExternalComponent } from '@cratis/scene.model';
import { StageDataProvider } from '../stageData';
import { StageExtensionComponent, StageExtensionServicesProvider } from '../StageExtensionComponent';
import { forgetExtensionModules } from '../stageExtensionModule';
import { aHost, placement } from './given/a_placed_component';

function Chart() {
    const raise = useRaise();
    return (
        <div>
            <button type='button' onClick={() => raise('pointChosen', { index: 2 })}>Choose</button>
            <button type='button' onClick={() => raise('dismissed', { index: 9 })}>Other</button>
        </div>);
}

const importer = async () => ({ Chart: reactComponent(Chart) });

const execute = (command: string, ...inputs: [string, string][]) => ({
    kind: 'ExecuteCommand', command, arguments: inputs.map(([name, path]) => ({ name, value: { path } })), onSuccess: [], onFailure: [], onResult: [],
});

const behavior = (trigger: Record<string, unknown>, command: string): Behavior => ({
    bindings: [{ trigger, actions: [execute(command, ['index', 'signal.index'])] }],
} as unknown as Behavior);

function aScreenHandling(behaviors: Behavior[]) {
    forgetExtensionModules();
    const executeCommand = vi.fn(async () => ({ isSuccess: true }));
    const dispatcher = { executeCommand } as unknown as ActionDispatcher;
    const element = { ...placement({ signals: ['pointChosen', 'dismissed'] }), behaviors } as ExternalComponent;
    const host = aHost();
    const view = render(
        <StageDataProvider routes={undefined} locale='en' locales={['en']} screen='Dashboard'>
            <InteractionScope dispatcher={dispatcher} context={{ resolve: () => undefined }} attachments={[]}>
                <StageExtensionServicesProvider services={{ fetcher: host.fetcher, importer }}>
                    <StageExtensionComponent element={element} />
                </StageExtensionServicesProvider>
            </InteractionScope>
        </StageDataProvider>);
    return { executeCommand, view };
}

describe('when a screen handles a signal an extension component raises', () => {
    it('runs the behavior declared for that signal with the values the signal carried', async () => {
        const { executeCommand, view } = aScreenHandling([behavior({ signalName: 'pointChosen' }, 'ChoosePoint')]);
        fireEvent.click(await view.findByRole('button', { name: 'Choose' }));
        await waitFor(() => expect(executeCommand).toHaveBeenCalledWith('ChoosePoint', { index: 2 }));
    });

    it('runs nothing for a signal the screen does not handle', async () => {
        const { executeCommand, view } = aScreenHandling([behavior({ signalName: 'pointChosen' }, 'ChoosePoint')]);
        fireEvent.click(await view.findByRole('button', { name: 'Other' }));
        await new Promise(resolve => setTimeout(resolve, 20));
        expect(executeCommand).not.toHaveBeenCalled();
    });

    it('does not run an application trigger that has the name of the signal', async () => {
        const { executeCommand, view } = aScreenHandling([behavior({ triggerName: 'pointChosen' }, 'ChoosePoint')]);
        fireEvent.click(await view.findByRole('button', { name: 'Choose' }));
        await new Promise(resolve => setTimeout(resolve, 20));
        expect(executeCommand).not.toHaveBeenCalled();
    });
});
