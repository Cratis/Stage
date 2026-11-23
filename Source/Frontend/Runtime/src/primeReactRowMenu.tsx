// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useRef, useState, type KeyboardEvent } from 'react';
import type { RegisteredComponentProps } from '@cratis/scene.react';
import { Menu, type MenuItemSelectEvent, type MenuRootOpenChangeEvent } from '@primereact/ui/menu';
import type { RowMenuProps } from './stageRowMenuActions';
import { RowMenuContext, type RowMenuItemProps } from './stageRowMenuContext';

function openWithArrowDown(event: KeyboardEvent<HTMLButtonElement>): void {
    if (event.key !== 'ArrowDown' || event.currentTarget.getAttribute('aria-expanded') === 'true') return;
    // PrimeReact's trigger opens on click; use that same path for the menu-button ArrowDown convention.
    event.preventDefault();
    event.currentTarget.click();
}

function returnFocusAfterMenuClose(restoreFocus: () => void, menuTarget: HTMLDivElement | null): void {
    requestAnimationFrame(() => {
        const current = document.activeElement;
        const inClosingMenu = menuTarget !== null && (menuTarget.contains(current) || current?.contains(menuTarget));
        // Opening focus can arrive after Escape; restore only from this popup or body, never from the new command dialog.
        if (current === document.body || inClosingMenu) restoreFocus();
    });
}

function PrimeReactRowMenuItem({ id, label, disabled, title, unavailable, onClick }: RowMenuItemProps) {
    return <Menu.Item value={id} data-scene-id={id} data-stage-availability={unavailable ? 'unavailable' : undefined} disabled={disabled} title={title} onSelect={(event: MenuItemSelectEvent) => {
        event.originalEvent.stopPropagation();
        onClick?.();
    }}>{label}</Menu.Item>;
}

/** Controllers stay mounted outside the popup so closing it cannot unmount their command dialog. */
export function PrimeReactRowMenu({ element }: RegisteredComponentProps) {
    const { row, label, actions, renderAction } = element.properties as unknown as RowMenuProps;
    const [target, setTarget] = useState<HTMLDivElement | null>(null);
    const [activated, setActivated] = useState(false);
    const host = useRef<HTMLSpanElement>(null);
    const restoreFocus = useCallback(() => { host.current?.querySelector<HTMLButtonElement>('button')?.focus(); }, []);
    return <span ref={host} onClick={event => event.stopPropagation()} onDoubleClick={event => event.stopPropagation()} onKeyDown={event => event.stopPropagation()}>
        <Menu.Root composite={false} onOpenChange={(event: MenuRootOpenChangeEvent) => {
            if (event.value) setActivated(true);
            else returnFocusAfterMenuClose(restoreFocus, target);
        }}>
            <Menu.Trigger aria-label={label} aria-haspopup='menu' onKeyDown={openWithArrowDown}>…</Menu.Trigger>
            <Menu.Portal><Menu.Positioner><Menu.Popup><Menu.List aria-label={label}><div ref={setTarget} /></Menu.List></Menu.Popup></Menu.Positioner></Menu.Portal>
            <RowMenuContext.Provider value={{ row, target, restoreFocus, Item: PrimeReactRowMenuItem }}>{activated && actions.map(renderAction)}</RowMenuContext.Provider>
        </Menu.Root>
    </span>;
}
