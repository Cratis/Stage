// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, useContext, type ReactNode } from 'react';
import { Slice } from '@cratis/arc.react/slices';
import type { ApplicationStructure, Screen } from '@cratis/scene.model';
import { locateScreen, locateSlice } from './stageNotes';

interface StageStructure {
    structure?: ApplicationStructure;
    screens: Screen[];
}

const StageStructureContext = createContext<StageStructure>({ screens: [] });

/** Gives the screens and dialogs below the structure of the application, so they can declare which slice they are. */
export function StageStructureProvider({ structure, screens, children }: StageStructure & { children: ReactNode }) {
    return <StageStructureContext.Provider value={{ structure, screens }}>{children}</StageStructureContext.Provider>;
}

/** Declares the slice a screen shows, so the markers and the application context know where the user is. */
export function ScreenSlice({ screen, children }: { screen: string | undefined; children: ReactNode }) {
    const { structure, screens } = useContext(StageStructureContext);
    const location = locateScreen(structure, screens.find(candidate => candidate.name === screen));
    return location ? <Slice module={location.module} feature={location.feature} slice={location.slice}>{children}</Slice> : <>{children}</>;
}

/** Declares the slice a dialog was opened for, named by its qualified name or by its command. */
export function DialogSlice({ slice, children }: { slice: string | undefined; children: ReactNode }) {
    const { structure } = useContext(StageStructureContext);
    const location = locateSlice(structure, slice);
    return location ? <Slice module={location.module} feature={location.feature} slice={location.slice}>{children}</Slice> : <>{children}</>;
}
