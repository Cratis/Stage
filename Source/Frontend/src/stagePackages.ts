// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    loadApprovedPackages,
    mergePackageRegistries,
    type ApprovedPackageImporters,
    type ApprovedPackageSet,
    type ComponentRegistry,
} from '@cratis/scene.react';
import type { DialogTemplate, ScreenTemplate } from '@cratis/scene.model';
import type { PackageCatalog } from '@cratis/scene.engine';

/** Where a live Stage serves the approved custom packages it was handed, relative to the page. */
export const stagePackagesEndpoint = 'stage/packages';

/**
 * What a live Stage says about the custom packages it was handed: the approved package set, where each package's
 * browser module is served from, the style sheets they bring, and every reason a package cannot be used.
 */
export interface StagePackagesDocument {
    /** The approved set, or `null` when the Stage was handed none. */
    set: ApprovedPackageSet | null;

    /** The page-relative URL of each package's entry module, by npm name. */
    modules: Record<string, string>;

    /** The page-relative URL of every style sheet the packages bring. */
    styles: string[];

    /** Why the packages cannot be used; empty when they can. */
    diagnostics: string[];
}

/**
 * The components the approved packages register and what each declares, ready for the Stage's `App`, and the templates a
 * domain blueprint among them brings: the screen templates an authored `template blueprint ...` reference names, and the dialog
 * templates a quick-create or confirmation opens.
 */
export interface StagePackages {
    components: ComponentRegistry;
    catalog: PackageCatalog;
    screenTemplates: ScreenTemplate[];
    dialogTemplates: DialogTemplate[];
}

/** How the packages' modules and style sheets reach the page. Replaced in specs. */
export interface StagePackagesHost {
    /** Reads the document the Stage serves. */
    read(signal?: AbortSignal): Promise<StagePackagesDocument | undefined>;

    /** Imports a module from the URL the Stage serves it at. */
    import(url: string): Promise<Record<string, unknown>>;

    /** Adds a style sheet to the page. */
    addStyle(url: string): void;
}

/** The host a browser page uses: it reads the Stage's document, imports modules by URL and links style sheets. */
export const browserStagePackagesHost: StagePackagesHost = {
    read: async signal => {
        const response = await fetch(stagePackagesEndpoint, { signal });
        // A Stage that predates custom packages has no such endpoint; it renders the packages it ships.
        return response.ok ? response.json() as Promise<StagePackagesDocument> : undefined;
    },
    // The URL is the one the Stage reported for an approved package, never one a scene supplied. Vite must not try to
    // resolve it at build time.
    import: url => import(/* @vite-ignore */ new URL(url, document.baseURI).href),
    addStyle: url => {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = url;
        document.head.appendChild(link);
    },
};

/** Thrown when the approved packages cannot be loaded; the Stage shows the reasons instead of rendering without them. */
export class StagePackagesFailed extends Error {
    constructor(readonly diagnostics: string[]) {
        super(`Approved packages could not be loaded:\n${diagnostics.join('\n')}`);
        this.name = 'StagePackagesFailed';
    }
}

/**
 * Loads the custom packages a live Stage was handed, before the first render.
 *
 * The packages are ES modules built with React left external, loaded at runtime from the Stage rather than compiled
 * into it. They load through the same `loadApprovedPackages` a generated application uses, so a package whose bundle
 * differs from its declaration, lacks its export or does not load is refused with the reason - a profile is never
 * rendered with a package silently missing.
 * @param host How to read the document and load modules.
 * @param signal Cancels the read.
 * @returns The packages' components and declarations; empty when the Stage was handed none.
 * @throws {StagePackagesFailed} When a package cannot be used.
 */
export async function loadStagePackages(host: StagePackagesHost = browserStagePackagesHost, signal?: AbortSignal): Promise<StagePackages> {
    const none: StagePackages = { components: {}, catalog: {}, screenTemplates: [], dialogTemplates: [] };
    const document = await host.read(signal).catch(() => undefined);
    if (!document?.set) {
        return none;
    }

    if (document.diagnostics.length > 0) {
        throw new StagePackagesFailed(document.diagnostics);
    }

    // One importer for each package the set names, reading the URL the Stage reported for it. A package the Stage did
    // not report has no importer, which is a diagnostic rather than an attempt to load some other address.
    const importers: ApprovedPackageImporters = Object.fromEntries(
        Object.entries(document.modules).map(([npm, url]) => [npm, () => host.import(url)]));

    const loaded = await loadApprovedPackages(document.set, importers, { allowExecutableImports: true });
    if (loaded.diagnostics.length > 0) {
        throw new StagePackagesFailed(loaded.diagnostics);
    }

    document.styles.forEach(style => host.addStyle(style));
    return {
        components: mergePackageRegistries(loaded.bundles),
        catalog: Object.fromEntries(loaded.bundles.map(bundle => [bundle.manifest.name, bundle.manifest.components])),
        screenTemplates: loaded.bundles.flatMap(bundle => bundle.screenTemplates ?? []),
        dialogTemplates: loaded.bundles.flatMap(bundle => bundle.dialogTemplates ?? []),
    };
}
