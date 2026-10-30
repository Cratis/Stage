// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** The browser storage key the user's theme choice lives under, so it survives a refresh. */
export const stageThemeChoiceKey = 'cratis.stage.theme';

function storage(): Storage | null {
    try {
        return globalThis.localStorage ?? null;
    } catch {
        return null;
    }
}

/** The theme the user chose with the toggle, or undefined when they never did or the browser refuses storage. */
export function readThemeChoice(): string | undefined {
    try {
        return storage()?.getItem(stageThemeChoiceKey) || undefined;
    } catch {
        return undefined;
    }
}

/** Remembers the user's theme choice in the browser. */
export function writeThemeChoice(themeName: string): void {
    try {
        storage()?.setItem(stageThemeChoiceKey, themeName);
    } catch {
        // Storage can be full or refused; the choice then lasts until the next refresh.
    }
}
