// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, useContext, useEffect, useRef, type ComponentType } from 'react';
import type { ExternalComponent } from '@cratis/scene.model';

/** The optional library widget that groups a row's actions; without one, Stage draws its native action buttons. */
export const stageRowMenuComponent = 'Stage:rowMenu';

/** The action presentation supplied by the optional popup widget. */
export interface RowMenuItemProps {
    id: string;
    label: string;
    disabled?: boolean;
    title?: string;
    unavailable?: boolean;
    onClick?: () => void;
}

interface RowMenuSubject {
    row: Record<string, unknown>;
    target: HTMLDivElement | null;
    restoreFocus?: () => void;
    Item?: ComponentType<RowMenuItemProps>;
}

export const RowMenuContext = createContext<RowMenuSubject | undefined>(undefined);
export const ScreenRowMenuActionsContext = createContext<ExternalComponent[]>([]);

/** A menu's clicked row is authoritative for every command and form consumer in that menu. */
export function useRowMenuSubject(): Record<string, unknown> | undefined {
    return useContext(RowMenuContext)?.row;
}

/** The actions this screen can safely group into its single table. */
export function useScreenRowMenuActions(): ExternalComponent[] {
    return useContext(ScreenRowMenuActionsContext);
}

/** A popup item disappears on selection, so a closed command dialog returns to its surviving trigger. */
export function useRowMenuDialogFocus(visible: boolean): void {
    const restoreFocus = useContext(RowMenuContext)?.restoreFocus;
    const wasVisible = useRef(visible);
    useEffect(() => {
        const closed = wasVisible.current && !visible;
        wasVisible.current = visible;
        if (!closed || !restoreFocus) return;
        // React Aria restores the dialog's old popup opener during unmount; return to the surviving row trigger afterwards.
        const frame = requestAnimationFrame(restoreFocus);
        return () => cancelAnimationFrame(frame);
    }, [visible, restoreFocus]);
}
