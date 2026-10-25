// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { useEffect } from 'react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { FormGenerationMode } from '@cratis/scene.model';
import { PrimeReactProvider } from '@primereact/core';
import { StageCommandForm, schemaPropertiesOf } from '../../Runtime/src/StageCommandForm';
import { StageDataProvider, useStageData } from '../../Runtime/src/stageData';
import { registerStageCommands } from '../host/commandRegistration';
import { sliceFieldsFrom } from '../../Runtime/src/stageSlices';
import { element } from '../../Runtime/src/testElements';
import { commandResult } from '../../Runtime/src/testing/commands';

const schema = JSON.stringify({ required: ['text_'], properties: { text_: { type: 'string' } } });
const routes = { commands: { Text: '/api/escaped/text' }, queries: {}, commandSchemas: { Text: schema } };

function PopulationSource() {
    const { setComponentOutput } = useStageData();
    useEffect(() => { setComponentOutput('selection', 'text', 'prefilled'); }, [setComponentOutput]);
    return null;
}

describe('when binding escaped command members', () => {
    afterEach(() => { vi.unstubAllGlobals(); vi.restoreAllMocks(); });

    it.each([
        { name: 'text_', label: 'Identity' },
        { name: 'identityInput', sourceProperty: 'text_', label: 'Identity' },
    ])('uses the escaped destination for schema, accessor, validation and submission of $name', async field => {
        const fetched = vi.fn().mockResolvedValue(new Response(JSON.stringify(commandResult())));
        vi.stubGlobal('fetch', fetched);
        registerStageCommands(routes);
        const form = element('escaped-form', 'Stage:commandForm', {
            command: 'Text', label: 'Text', fields: [field], schema,
            generationMode: FormGenerationMode.Manual,
            arguments: [{ name: 'text_', source: 'component.selection.text' }],
        });
        render(<PrimeReactProvider><StageDataProvider routes={routes} locale='en' locales={['en']} screen='Escapes'>
            <PopulationSource />
            <StageCommandForm element={form} />
        </StageDataProvider></PrimeReactProvider>);

        expect(schemaPropertiesOf(schema)).toEqual([{ name: 'text_', type: 'text', required: true, unsupportedReason: undefined }]);
        const input = screen.getByLabelText('Identity') as HTMLInputElement;
        await waitFor(() => expect(input.value).toEqual('prefilled'));
        fireEvent.change(input, { target: { value: '' } });
        fireEvent.click(screen.getByRole('button', { name: 'Text' }));
        await waitFor(() => expect(screen.getAllByText('Identity is required').length).toBeGreaterThan(0));
        expect(fetched).not.toHaveBeenCalled();
        fireEvent.change(input, { target: { value: 'submitted' } });
        fireEvent.click(screen.getByRole('button', { name: 'Text' }));
        await waitFor(() => expect(fetched).toHaveBeenCalled());
        expect(JSON.parse(String((fetched.mock.calls[0][1] as RequestInit).body))).toEqual({ text_: 'submitted' });
    });

    it('uses the same escaped schema field for the toolbar task dialog', () => {
        expect(sliceFieldsFrom(routes.commandSchemas)('Text')).toEqual([
            { name: 'text_', type: 'string', label: 'Text_', required: true },
        ]);
    });
});
