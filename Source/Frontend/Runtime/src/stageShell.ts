// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ColorScheme as ProfileColorScheme, type Theme, type UiProfile } from '@cratis/scene.model';
import { ShellDiagnosticCode, ShellOrigin, type EffectiveShell } from '@cratis/scene.engine';
import { ColorScheme, LayoutMode, MenuTheme, defaultBlueprintThemes, layoutModes, menuThemes } from '@cratis/scene.blueprint.default';

/** What the profile's root template, shell options and appearance ask of the running shell. */
export interface StageShellSettings {
    /** The sidebar mode the profile's `navigationMode` option names. */
    mode?: LayoutMode;

    /** How the sidebar is tinted, when the profile's `menuTheme` option names one. */
    menuTheme?: MenuTheme;

    /** Whether the shell draws its footer; the `showFooter` option, drawn unless it is `false`. */
    showFooter: boolean;

    /** The theme the colour scheme selects, by name. */
    themeName?: string;

    /** The colour scheme the sidebar and the page follow. */
    colorScheme?: ColorScheme;

    /** Whether the profile follows the operating system's scheme instead of fixing one. */
    followsSystem: boolean;

    /** The accent colour, as the profile wrote it. */
    accent?: string;

    /** Options and values the shell does not understand: stated, never silently dropped. */
    problems: string[];
}

/** The shell options the default blueprint's shells expose; any other name is stated as a problem. */
export const knownShellOptions: readonly string[] = ['navigationMode', 'menuTheme', 'showFooter'];

const hexColor = /^#(?:[0-9a-f]{3}|[0-9a-f]{6})$/i;

/**
 * Reads the profile's shell options and appearance into what the running shell is told to do.
 *
 * The effective shell is the engine's answer (`resolveEffectiveShell`); this only maps its options and the profile's
 * appearance onto the blueprint's layout configuration, so the Stage and the Scene preview start a shell the same way.
 *
 * @param profile The profile the application renders with.
 * @param shell The effective shell the engine resolved for it.
 * @param prefersDark Whether the operating system prefers a dark scheme, for a profile that follows the system.
 * @returns The settings, with every option or value that could not be applied.
 */
export function stageShellSettings(profile: UiProfile | undefined, shell: EffectiveShell, prefersDark = false): StageShellSettings {
    const problems: string[] = [];
    const options = shell.options ?? {};
    const settings: StageShellSettings = { showFooter: true, followsSystem: false, problems };

    for (const [name, value] of Object.entries(options)) {
        if (!knownShellOptions.includes(name)) {
            problems.push(`The shell '${shell.name}' has no option '${name}'; the options it understands are ${knownShellOptions.join(', ')}.`);
            continue;
        }

        if (name === 'navigationMode') {
            const mode = layoutModes.find(candidate => candidate === value);
            if (mode) settings.mode = mode;
            else problems.push(`Option 'navigationMode' is '${String(value)}'; expected one of ${layoutModes.join(', ')}.`);
        } else if (name === 'menuTheme') {
            const menuTheme = menuThemes.find(candidate => candidate === value);
            if (menuTheme) settings.menuTheme = menuTheme;
            else problems.push(`Option 'menuTheme' is '${String(value)}'; expected one of ${menuThemes.join(', ')}.`);
        } else if (typeof value === 'boolean') {
            settings.showFooter = value;
        } else {
            problems.push(`Option 'showFooter' is '${String(value)}'; expected true or false.`);
        }
    }

    const appearance = profile?.appearance;
    if (appearance?.colorScheme !== undefined) {
        settings.followsSystem = appearance.colorScheme === ProfileColorScheme.System;
        const dark = settings.followsSystem ? prefersDark : appearance.colorScheme === ProfileColorScheme.Dark;
        settings.colorScheme = dark ? ColorScheme.Dark : ColorScheme.Light;
        settings.themeName = defaultBlueprintThemes.find(theme => (theme.isDark ?? false) === dark)?.name;
    }

    if (appearance?.accent !== undefined) {
        if (hexColor.test(appearance.accent)) settings.accent = appearance.accent;
        else problems.push(`The accent '${appearance.accent}' is not a colour written as #rgb or #rrggbb.`);
    }

    return settings;
}

/**
 * What stops an application starting with the root template its profile names: the template is unknown, its blueprint is not
 * installed, its version pin is not satisfied or it cannot be a shell. A page that cannot be drawn in the shell it asked for
 * is stated, not drawn in a different one.
 *
 * @param profile The profile the application renders with.
 * @param shell The effective shell the engine resolved for it.
 * @returns The problems, empty when the root template resolved or the profile names none.
 */
export function shellStartupProblems(profile: UiProfile | undefined, shell: EffectiveShell): string[] {
    if (profile?.root === undefined) return [];
    const blocking = new Set<ShellDiagnosticCode>([
        ShellDiagnosticCode.RootTemplateUnknown,
        ShellDiagnosticCode.RootTemplateVersionNotSatisfied,
        ShellDiagnosticCode.RootTemplateNotApplicable,
        ShellDiagnosticCode.RootTemplateMissingContentSlot,
        ShellDiagnosticCode.RootTemplateCycle,
        ShellDiagnosticCode.RootTemplateBlueprintNotInstalled,
    ]);
    const found = shell.diagnostics.filter(diagnostic => blocking.has(diagnostic.code)).map(diagnostic => diagnostic.message);
    if (found.length === 0 && shell.origin !== ShellOrigin.Root) found.push(`The root template '${profile.root.template.id}' did not resolve, so the application would not start in the shell it names.`);
    return found;
}

/** The black or white that reads on a colour. */
function contrastOn(hex: string): string {
    const digits = hex.length === 4 ? [...hex.slice(1)].map(digit => digit + digit).join('') : hex.slice(1);
    const [red, green, blue] = [0, 2, 4].map(start => parseInt(digits.slice(start, start + 2), 16));
    return (red * 299 + green * 587 + blue * 114) / 1000 >= 150 ? '#111827' : '#ffffff';
}

/**
 * The themes with the profile's accent set as their primary colour. Without an accent the themes are returned as they are, so
 * the theme keeps its own.
 *
 * @param themes The themes the shell offers.
 * @param accent The accent colour, `#rgb` or `#rrggbb`.
 * @returns The themes to give the shell.
 */
export function themesWithAccent(themes: Theme[], accent: string | undefined): Theme[] {
    if (accent === undefined) return themes;
    const digits = accent.length === 4 ? [...accent.slice(1)].map(digit => digit + digit).join('') : accent.slice(1);
    const [red, green, blue] = [0, 2, 4].map(start => parseInt(digits.slice(start, start + 2), 16));
    return themes.map(theme => ({
        ...theme,
        tokens: {
            ...theme.tokens,
            'primary.color': accent,
            'primary.contrastColor': contrastOn(accent),
            'focus.ring': `0 0 0 2px rgb(${red} ${green} ${blue} / 0.4)`,
        },
    }));
}
