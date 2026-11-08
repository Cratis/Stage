// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useContext, useEffect, useMemo, type ReactNode } from 'react';
import { SlotRole, type SceneElement } from '@cratis/scene.model';
import {
    SceneElementView,
    SceneNavigationContextInternal,
    SceneSlotRouteContext,
    coreSlotShape,
    routedSlotPath,
    useOptionalSceneRouter,
    useSceneSlots,
    type ComponentRegistry,
    type RegisteredComponentProps,
} from '@cratis/scene.react';
import { StageDataProvider, useStageData } from './stageData';
import { StageQueryBindings } from './stageQueryBindings';
import { StageScreenTables } from './stageScreenTables';
import { ScreenSlice } from './stageStructure';

function text(value: unknown): string | undefined {
    return typeof value === 'string' && value.length > 0 ? value : undefined;
}

/**
 * The `core:slot` the Stage renders, drawn inside the Stage's own data scope so the queries, selections and commands of the screen it
 * shows work as they do on a primary screen.
 *
 * - A **routed** slot shows what the router says (the address, in the running application): a hook that opens a screen in it, or a
 *   selection it shows, changes the address, and the address brings it back on a reload or a shared link. The screen it shows reads the
 *   route values it was opened with - its own scope's parameters win over the page's - so `ProductDetails by sku` reads the row's sku.
 * - A slot a `show <Screen> by <param> from <element>.selection` line stands in opens that screen, keyed by the selected row, whenever the
 *   element's selection changes.
 * - Otherwise it shows the screen the Scene navigation host placed in it (`navigate to … in slot side`).
 *
 * A panel is drawn as a landmark (`aside`), an optional slot is hidden until something fills it, and an optional pane or panel - like a
 * slot the host put a screen in - can be closed.
 */
export function StageSlot({ element }: RegisteredComponentProps) {
    const shape = coreSlotShape(element.properties);
    const navigation = useContext(SceneNavigationContextInternal);
    const router = useOptionalSceneRouter();
    const slots = useSceneSlots();
    const data = useStageData();
    const shows = text(element.properties.shows);
    const showsBy = text(element.properties.showsBy);
    const showsFrom = text(element.properties.showsFromElement) ?? text(element.properties.showsFrom);
    const route = shape.routed || shows ? router?.state.slots[shape.name] : undefined;
    const selected = showsFrom ? data.selections[showsFrom] : undefined;
    const key = selected && showsBy ? selected[showsBy] : undefined;
    const selection = key === undefined || key === null || key === '' ? undefined : String(key);

    // Only a change of the selection opens the screen: going back to an earlier address keeps what that address shows.
    useEffect(() => {
        if (!router || !shows || !showsBy || selection === undefined) return;
        const current = router.state.slots[shape.name];
        if (current?.screen === shows && current.parameters[showsBy] === selection) return;
        router.openInSlot(shape.name, { screen: shows, parameters: { [showsBy]: selection } });
    }, [selection, shows, showsBy, shape.name]);

    const placed = route ? route.screen ?? shows : navigation?.slots[shape.name];
    const screen = placed && slots ? slots.screens[placed] : undefined;
    if (shape.optional && !placed) return null;

    const closable = shape.closable || (shape.optional && (shape.role === SlotRole.Pane || shape.role === SlotRole.Panel));
    const close = () => route ? router?.closeSlot(shape.name) : navigation?.closeSlot(shape.name);
    const attributes = {
        'data-scene-slot': shape.name,
        'data-scene-slot-screen': placed,
        'data-scene-slot-role': shape.role,
        'data-scene-slot-routed': shape.routed ? 'true' : undefined,
        'data-scene-slot-route': route ? routedSlotPath(route, shape.routedBy) : undefined,
    };
    const content: ReactNode = (
        <>
            {closable && placed && (
                <div className='scene-slot-bar'>
                    <button type='button' className='scene-slot-close' aria-label='Close' onClick={close}>Close</button>
                </div>
            )}
            {screen && slots && placed && (route
                ? <StageRoutedScreen name={placed} screen={screen} registry={slots.registry} values={route.parameters} />
                : <ScreenSlice screen={placed}><StageScreenTables element={screen}><SceneElementView element={screen} registry={slots.registry} resolveBinding={data.resolveBinding} /></StageScreenTables></ScreenSlice>)}
        </>
    );

    return shape.role === SlotRole.Panel
        ? <aside aria-label={shape.name} {...attributes}>{content}</aside>
        : <div {...attributes}>{content}</div>;
}

interface StageRoutedScreenProps {
    name: string;
    screen: SceneElement;
    registry: ComponentRegistry;
    values: Record<string, string>;
}

// A screen shown in a routed slot reads the values it was opened with, over the page's own.
function StageRoutedScreen({ name, screen, registry, values }: StageRoutedScreenProps) {
    const data = useStageData();
    const key = JSON.stringify({ ...data.parameters, ...values });
    const parameters = useMemo(() => JSON.parse(key) as Record<string, string>, [key]);
    return (
        <SceneSlotRouteContext.Provider value={values}>
            <StageDataProvider routes={data.routes} routesReady={data.routesReady} parameters={parameters} locale={data.locale} locales={data.locales} screen={name}>
                <StageSlotScreen name={name} screen={screen} registry={registry} />
            </StageDataProvider>
        </SceneSlotRouteContext.Provider>
    );
}

function StageSlotScreen({ name, screen, registry }: Omit<StageRoutedScreenProps, 'values'>) {
    const data = useStageData();
    return (
        <ScreenSlice screen={name}>
            <StageScreenTables element={screen}>
                <StageQueryBindings element={screen} />
                <SceneElementView element={screen} registry={registry} resolveBinding={data.resolveBinding} queryResults={data.queryResults} />
            </StageScreenTables>
        </ScreenSlice>
    );
}
