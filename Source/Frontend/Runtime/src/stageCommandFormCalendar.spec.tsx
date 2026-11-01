// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { Dialog } from '@cratis/components/Dialogs';
import { render } from './testing/render';
import { RegisteredForm, commandResult } from './testing/commands';
import { StageDataProvider } from './stageData';
import { element } from './testElements';

const route = '/api/schedule';
const schema = JSON.stringify({
    type: 'object', required: ['dueDate'],
    properties: { dueDate: { type: 'string', format: 'date' }, reminder: { type: ['string', 'null'], format: 'date' } },
});

function view(inDialog: boolean) {
    const form = <StageDataProvider routes={{ commands: { Schedule: route }, queries: {} }} locale='en' locales={['en']} screen='Schedule'>
        <RegisteredForm element={element('schedule', 'Stage:commandForm', { command: 'Schedule', label: 'Schedule', schema, generationMode: 'auto' })} />
    </StageDataProvider>;
    return render(inDialog ? <Dialog title='Schedule task' visible onClose={() => {}} buttons={null} dismissable>{form}</Dialog> : form);
}

function press(button: HTMLElement) {
    fireEvent.pointerDown(button, { pointerType: 'mouse', button: 0 });
    fireEvent.mouseDown(button, { button: 0 });
    fireEvent.pointerUp(button, { pointerType: 'mouse', button: 0 });
    fireEvent.mouseUp(button, { button: 0 });
    fireEvent.click(button);
}

describe('a command form calendar', () => {
    afterEach(() => { vi.unstubAllGlobals(); vi.restoreAllMocks(); });

    for (const inDialog of [false, true]) {
        it(`should keep submit accessible and submit on the first click with the calendar open${inDialog ? ' inside a dialog' : ''}`, async () => {
            const fetched = vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult() });
            vi.stubGlobal('fetch', fetched);
            view(inDialog);

            for (const name of ['dueDate', 'reminder']) {
                const field = document.querySelector(`[data-field="${name}"]`) as HTMLElement;
                press(within(field).getByRole('button', { name: /Open calendar/ }));
                const calendar = await screen.findByRole('grid');
                const day = within(calendar).getAllByRole('button').find(button => button.textContent === '15' && button.getAttribute('aria-disabled') !== 'true');
                expect(day).toBeDefined();
                press(day!);
                await waitFor(() => expect(screen.queryByRole('grid')).toBeNull());
                fireEvent.blur(field);
            }
            const field = document.querySelector('[data-field="dueDate"]') as HTMLElement;
            press(within(field).getByRole('button', { name: /Open calendar/ }));
            await screen.findByRole('grid');
            const submit = screen.getByRole('button', { name: 'Submit' });
            press(submit);

            await waitFor(() => expect(fetched).toHaveBeenCalledOnce());
            const body = JSON.parse(String((fetched.mock.calls[0][1] as RequestInit).body));
            expect(body.dueDate).toMatch(/^\d{4}-\d{2}-15$/);
            expect(body.reminder).toBe(body.dueDate);
            expect(document.querySelector('[data-field="dueDate"] [data-invalid="true"]')).toBeNull();
        });
    }
});
