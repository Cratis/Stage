// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useRef } from 'react';
import { ColorScheme, defaultBlueprintThemes, useLayoutConfig } from '@cratis/scene.blueprint.default';
import type { StageShellSettings } from './stageShell';
import { readThemeChoice } from './stageThemeChoice';

/** The query that says the operating system prefers a dark scheme. */
export const prefersDarkQuery = '(prefers-color-scheme: dark)';

/** Whether the operating system prefers a dark scheme right now. */
export function systemPrefersDark(): boolean {
    return globalThis.matchMedia?.(prefersDarkQuery).matches ?? false;
}

/**
 * Starts the shell the way the profile says: its sidebar mode, its menu tint and the theme its colour scheme selects. The user can
 * still switch theme or mode afterwards, as in the Scene preview; this is only what the application starts in.
 *
 * It renders nothing and sits inside the shell's layout configuration, so it speaks to the same provider the topbar's toggle does.
 */
export function StageShellSettingsApplier({ settings, onSystemSchemeChange }: { settings: StageShellSettings; onSystemSchemeChange?: (prefersDark: boolean) => void }) {
    // The setters change with every configuration change; reading them through a ref keeps this to the profile's own changes, so a
    // user switching the mode afterwards is not put back.
    const configuration = useRef(useLayoutConfig());
    configuration.current = useLayoutConfig();
    const { mode, menuTheme, colorScheme, themeName } = settings;

    useEffect(() => {
        const { setMode, setMenuTheme, setColorScheme, setThemeName } = configuration.current;
        if (mode !== undefined) setMode(mode);
        if (menuTheme !== undefined) setMenuTheme(menuTheme);
        // What the user chose with the toggle outranks what the profile starts in, so a refresh keeps it.
        const chosen = defaultBlueprintThemes.find(theme => theme.name === readThemeChoice());
        if (chosen) {
            setThemeName(chosen.name);
            setColorScheme(chosen.isDark ? ColorScheme.Dark : ColorScheme.Light);
            return;
        }

        if (colorScheme !== undefined) setColorScheme(colorScheme);
        if (themeName !== undefined) setThemeName(themeName);
    }, [mode, menuTheme, colorScheme, themeName]);

    useEffect(() => {
        if (!settings.followsSystem || !globalThis.matchMedia || !onSystemSchemeChange) return undefined;
        const query = globalThis.matchMedia(prefersDarkQuery);
        const changed = (event: MediaQueryListEvent) => onSystemSchemeChange(event.matches);
        query.addEventListener('change', changed);
        return () => query.removeEventListener('change', changed);
    }, [settings.followsSystem, onSystemSchemeChange]);

    return null;
}
