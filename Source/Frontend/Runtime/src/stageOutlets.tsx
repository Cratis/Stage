// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, type ReactNode } from 'react';
import type { ApplicationStructure, FeatureNode, Layout, NavigationModel, Screen, ScreenTemplate, SliceNode } from '@cratis/scene.model';
import { InputSourceKind, outletChainOf, placementSlotsOf, type ActionSlice, type OutletLevel } from '@cratis/scene.engine';
import { SceneOutletsProvider, type SceneOutletsContextValue } from '@cratis/scene.react';
import { schemaPropertiesOf } from './stageFormSchema';

/** The parts of a Stage application the outlets of a screen resolve through. */
export interface StageOutletsSource {
    layouts: Layout[];
    screenTemplates: ScreenTemplate[];
    screens: Screen[];
    navigation?: NavigationModel;
    outletLevels?: OutletLevel[];
    structure?: ApplicationStructure;
    commandSchemas?: Record<string, string>;
}

/**
 * The slices the hooks of an application run, as placement and the slice invoker see them: each slice's command, its screen and where
 * that screen fits, and the inputs its command takes - which an item or the route feed by name. The identifier the command acts on
 * (`x-identity`) has the item or screen as its source, so a hook can run the slice directly; any other input needs the form.
 *
 * @param source The application.
 * @returns The slices, by their qualified id.
 */
export function stageActionSlicesOf(source: StageOutletsSource): ActionSlice[] {
    const screens = new Map(source.screens.map(screen => [screen.name, screen]));
    return everySlice(source.structure).map(slice => {
        const screen = screens.get(slice.name);
        // The identifier the command acts on comes from the item or screen the hook acts on; anything else needs the form.
        const inputs = slice.command
            ? schemaPropertiesOf(source.commandSchemas?.[slice.command] ?? '').map(property => property.identity ? { name: property.name, source: InputSourceKind.DataContext } : { name: property.name })
            : [];
        return {
            id: slice.id,
            ...(slice.command ? { command: slice.command } : {}),
            ...(screen ? { screen: screen.name } : {}),
            ...(screen?.fits ? { fits: screen.fits } : {}),
            inputs,
        };
    });
}

function everySlice(structure: ApplicationStructure | undefined): SliceNode[] {
    const of = (features: FeatureNode[]): SliceNode[] => features.flatMap(feature => [...feature.slices, ...of(feature.features ?? [])]);
    return (structure?.modules ?? []).flatMap(module => [...(module.slices ?? []), ...of(module.features ?? [])]);
}

/**
 * What the outlets of a screen resolve through: the levels from the layout down to the screen, the levels that contribute to them, the
 * slices their hooks run, and the slots of the screen a hook can open in.
 *
 * @param source The application.
 * @param screenName The screen.
 * @returns The outlets' context.
 */
export function stageOutletsOf(source: StageOutletsSource, screenName: string | undefined): SceneOutletsContextValue {
    const screen = source.screens.find(candidate => candidate.name === screenName);
    if (!screen) return { chain: [] };
    const { chain, contributors } = outletChainOf({ layouts: source.layouts, screenTemplates: source.screenTemplates, screens: source.screens, navigation: source.navigation, levels: source.outletLevels }, screen.name);
    const template = source.screenTemplates.find(candidate => candidate.name === screen.screenTemplate);
    const layout = source.layouts.find(candidate => candidate.name === screen.layout);
    const filled = Object.entries(screen.slotContent ?? {})
        .filter(([, elements]) => elements.some(element => (element as { componentName?: string }).componentName !== 'core:slot'))
        .map(([slot]) => slot);
    const slots = placementSlotsOf([...(layout?.slots ?? []), ...(template?.slots ?? [])], filled);
    return { chain, contributors, slices: stageActionSlicesOf(source), slots };
}

/**
 * Gives the outlets of a screen what they resolve through (see {@link stageOutletsOf}).
 */
export function StageOutletsScope({ source, screen, children }: { source: StageOutletsSource; screen: string | undefined; children: ReactNode }) {
    const value = useMemo(() => stageOutletsOf(source, screen), [source, screen]);
    return <SceneOutletsProvider value={value}>{children}</SceneOutletsProvider>;
}
