// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const words = /[A-Z]+(?![a-z])|[A-Z]?[a-z]+\d*|\d+/g;
const screenSuffixes = new Set(['Screen', 'Page', 'Dialog']);

function sentence(parts: string[]): string {
    const text = parts.map((part, index) => {
        const acronym = part.length > 1 && part === part.toUpperCase() && /[A-Z]/.test(part);
        if (acronym) return part;
        return index === 0 ? part.charAt(0).toUpperCase() + part.slice(1) : part.toLowerCase();
    }).join(' ');
    return text;
}

/**
 * Humanizes a name the way a label reads: PascalCase and camelCase are split into words and written in sentence case, while a run of
 * capitals stays as written - `postalCode` is "Postal code", `RegisterSKU` is "Register SKU".
 *
 * @param name The name.
 * @returns The label text; a name with no letters or digits is returned as it is.
 */
export function humanize(name: string): string {
    const parts = name.match(words);
    return parts ? sentence(parts) : name;
}

/**
 * Humanizes a screen's name, dropping a trailing `Screen`, `Page` or `Dialog` that only says what kind of thing it is:
 * `PipelineScreen` is "Pipeline". A name that is only that word is kept.
 *
 * @param name The screen's name.
 * @returns The label text.
 */
export function humanizeScreenName(name: string): string {
    const parts = name.match(words);
    if (!parts) return name;
    return sentence(parts.length > 1 && screenSuffixes.has(parts[parts.length - 1]) ? parts.slice(0, -1) : parts);
}

/** The Button variant a toolbar action takes: its authored emphasis, else primary for the first action and secondary for the ones after it. */
export function actionVariant(authored: string, followsAnotherAction: boolean): 'solid' | 'outline' | 'ghost' {
    switch (authored.toLowerCase()) {
        case 'primary': return 'solid';
        case 'secondary': return 'outline';
        case 'quiet': return 'ghost';
        default: return followsAnotherAction ? 'outline' : 'solid';
    }
}
