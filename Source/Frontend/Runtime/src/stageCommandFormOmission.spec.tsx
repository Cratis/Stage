// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { FormGenerationMode } from '@cratis/scene.model';
import { RegisteredForm as StageCommandForm, commandResult } from './testing/commands';
import { StageDataProvider } from './stageData';
import { element } from './testElements';

describe('an unrendered optional command member', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); });

    it('is absent from the native request when Stage never supplies it', async () => {
        const fetched = vi.fn<typeof fetch>().mockResolvedValue(new Response(JSON.stringify(commandResult()), { status: 200 }));
        vi.stubGlobal('fetch', fetched);
        const schema = JSON.stringify({ type: 'object', required: ['name'], properties: { name: { type: 'string' }, archiveReason: { type: 'string' } } });
        render(
            <PrimeReactProvider>
                <StageDataProvider routes={{ commands: { RenameProject: '/api/projects/rename' }, queries: {} }} locale='en' locales={['en']} screen='Projects'>
                    <StageCommandForm element={element('rename-project', 'Stage:commandForm', {
                        command: 'RenameProject', label: 'Rename project', schema, generationMode: FormGenerationMode.Manual,
                        fields: [{ name: 'name', label: 'Name' }],
                    })} />
                </StageDataProvider>
            </PrimeReactProvider>,
        );
        expect(screen.getAllByRole('textbox')).toHaveLength(1);
        fireEvent.change(screen.getByLabelText('Name'), { target: { value: 'Kept edit' } });
        fireEvent.click(screen.getByRole('button', { name: 'Execute Rename project' }));
        await waitFor(() => expect(fetched).toHaveBeenCalledTimes(1));
        const body: unknown = JSON.parse(String(fetched.mock.calls[0][1]?.body));
        expect(body).toEqual({ name: 'Kept edit' });
        expect(Object.hasOwn(body as object, 'archiveReason')).toBe(false);
    });
});
