// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, useContext, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { Button } from 'primereact/button';
import { Dialog } from 'primereact/dialog';

const CloseContext = createContext<() => void>(() => undefined);

/** Closes the action dialog the calling form is shown in - what a command that succeeded does; nothing when the form is not in one. */
export const useCloseActionDialog = (): (() => void) => useContext(CloseContext);

interface StageActionDialogProps {
    id: string;
    label: string;
    children: ReactNode;
}

/**
 * The dialog an action opens its command form in. The form is what knows the command succeeded, so unlike Scene's own dialog this one can be closed from
 * its content: a succeeded add to the basket, an address entered on the checkout page, leave the page they were done on instead of a form to fill in again.
 */
export function StageActionDialog({ id, label, children }: StageActionDialogProps) {
    const [open, setOpen] = useState(false);
    const close = useMemo(() => () => setOpen(false), []);

    return (
        <div data-scene-id={id}>
            <Button onClick={() => setOpen(true)}>{label}</Button>
            <Dialog.Root open={open} modal dismissable closeOnEscape onOpenChange={(event: { value?: boolean }) => { if (!event.value) setOpen(false); }}>
                <Dialog.Portal>
                    <Dialog.Backdrop />
                    <Dialog.Positioner>
                        <Dialog.Popup style={{ width: '32rem' }}>
                            <Dialog.Header>
                                <Dialog.Title>{label}</Dialog.Title>
                                <Dialog.Close aria-label='Close'><i className='pi pi-times' /></Dialog.Close>
                            </Dialog.Header>
                            <Dialog.Content><CloseContext.Provider value={close}>{children}</CloseContext.Provider></Dialog.Content>
                        </Dialog.Popup>
                    </Dialog.Positioner>
                </Dialog.Portal>
            </Dialog.Root>
        </div>
    );
}
