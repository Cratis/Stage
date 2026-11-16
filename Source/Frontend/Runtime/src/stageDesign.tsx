// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, type ReactNode } from 'react';
import { ColorScheme as ProfileColorScheme, type UiProfile } from '@cratis/scene.model';
import { resolveDesign, type ResolvedDesign } from '@cratis/scene.engine';
import { SceneThemeProvider } from '@cratis/scene.react';
import { ColorScheme, defaultBlueprintDesignSystem, defaultBlueprintThemes, useLayoutConfig } from '@cratis/scene.blueprint.default';

/**
 * Resolves the design the application is drawn from: the design system the profile carries - or the default one the blueprint ships when the
 * model declares none - for the theme the profile selects, in the mode the colour scheme selects, with the accent, corners and font the profile's
 * appearance replaces the theme's own with.
 *
 * This is Scene's `resolveDesign`, the one function Studio's preview and the generated application share, so a model looks the same in all of them.
 * The design system reaches the Stage in the Scene as `UiProfile.designSystem`, resolved from the model by the Screenplay-to-Scene bridge
 * (Cratis/Stage#1081); only the choice between its modes is made here, because the user can switch it.
 *
 * @param profile The profile the application renders with.
 * @param scheme The colour scheme the shell is showing: what the profile starts in until the user switches it.
 * @param accent The profile's accent when it is a colour the shell can use; the accent of the appearance otherwise stays out of the design.
 * @param systemIsDark Whether the operating system prefers a dark scheme, for a design that follows it.
 * @returns The resolved design: theme, mode, token values and resolved components.
 */
export function stageDesignOf(profile: UiProfile | undefined, scheme: ColorScheme, accent: string | undefined, systemIsDark = false): ResolvedDesign {
    const { accent: _declared, colorScheme: _scheme, ...appearance } = profile?.appearance ?? {};
    return resolveDesign({
        designSystem: profile?.designSystem ?? defaultBlueprintDesignSystem,
        theme: profile?.theme,
        packageThemes: defaultBlueprintThemes,
        appearance: {
            ...appearance,
            ...(accent === undefined ? {} : { accent }),
            colorScheme: scheme === ColorScheme.Dark ? ProfileColorScheme.Dark : ProfileColorScheme.Light,
        },
        systemIsDark,
    });
}

/** What the design the application is drawn from needs to know to be resolved. */
export interface StageDesignProviderProps {
    /** The profile the application renders with. */
    profile: UiProfile | undefined;

    /** The accent colour the shell can use, when the profile states one. */
    accent?: string;

    /** Whether the operating system prefers a dark scheme right now. */
    prefersDark: boolean;

    /** The application. */
    children: ReactNode;
}

/**
 * Draws everything below it from the application's design system: it resolves the design for the colour scheme the shell is in and writes
 * it onto a wrapping element as the custom properties the components read their look from. Switching the scheme re-resolves in place, so
 * nothing below it remounts.
 *
 * It sits inside the shell's layout configuration, which is where the colour scheme lives - the profile's start and the user's switch.
 */
export function StageDesignProvider({ profile, accent, prefersDark, children }: StageDesignProviderProps) {
    const { config } = useLayoutConfig();
    const design = useMemo(() => stageDesignOf(profile, config.colorScheme, accent, prefersDark), [profile, config.colorScheme, accent, prefersDark]);

    return <SceneThemeProvider design={design}>{children}</SceneThemeProvider>;
}
