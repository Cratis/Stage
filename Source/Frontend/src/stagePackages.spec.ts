// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it, vi } from 'vitest';
import { StagePackagesFailed, loadStagePackages, type StagePackagesDocument, type StagePackagesHost } from './stagePackages';

const set = {
    version: 1,
    packages: [{
        manifest: { name: 'Acme.Charts', version: '1.2.0', kind: 'ComponentLibrary', components: ['chart'] },
        module: { npm: '@acme/scene.charts', version: '1.2.0', export: 'acmeChartsRuntimePackage', styles: ['@acme/scene.charts/styles.css'] },
    }],
};

const chart = () => null;
const bundle = (overrides: Record<string, unknown> = {}) => ({
    manifest: { name: 'Acme.Charts', version: '1.2.0', kind: 'ComponentLibrary', components: ['chart'], ...overrides },
    components: { 'Acme.Charts:chart': chart },
});

const document = (overrides: Partial<StagePackagesDocument> = {}): StagePackagesDocument => ({
    set: set as never,
    modules: { '@acme/scene.charts': 'stage/packages/files/@acme/scene.charts/index.js' },
    styles: ['stage/packages/files/@acme/scene.charts/styles.css'],
    diagnostics: [],
    ...overrides,
});

const host = (read: StagePackagesDocument | undefined, module: Record<string, unknown> = { acmeChartsRuntimePackage: bundle() }) => {
    const styles: string[] = [];
    const imported: string[] = [];
    const value: StagePackagesHost = {
        read: async () => read,
        import: async url => { imported.push(url); return module; },
        addStyle: url => { styles.push(url); },
    };
    return { host: value, styles, imported };
};

describe('loading the custom packages a live Stage was handed', () => {
    it('loads the module from the URL the Stage reported and registers its components', async () => {
        const { host: h, imported } = host(document());
        const packages = await loadStagePackages(h);
        expect(imported).toEqual(['stage/packages/files/@acme/scene.charts/index.js']);
        expect(packages.components['Acme.Charts:chart']).toBe(chart);
        expect(packages.catalog).toEqual({ 'Acme.Charts': ['chart'] });
    });

    it('adds the style sheets of the packages', async () => {
        const { host: h, styles } = host(document());
        await loadStagePackages(h);
        expect(styles).toEqual(['stage/packages/files/@acme/scene.charts/styles.css']);
    });

    it('renders with the shipped packages when the Stage was handed none', async () => {
        const { host: h, imported } = host(document({ set: null, modules: {}, styles: [] }));
        expect(await loadStagePackages(h)).toEqual({ components: {}, catalog: {} });
        expect(imported).toEqual([]);
    });

    it('renders with the shipped packages when the Stage predates custom packages', async () => {
        expect(await loadStagePackages(host(undefined).host)).toEqual({ components: {}, catalog: {} });
    });

    it('refuses to render when the Stage reports a problem with the packages', async () => {
        const { host: h, imported } = host(document({ diagnostics: ['the packages folder has no @acme/scene.charts folder'] }));
        await expect(loadStagePackages(h)).rejects.toMatchObject({ diagnostics: ['the packages folder has no @acme/scene.charts folder'] });
        expect(imported).toEqual([]);
    });

    it('refuses a bundle that differs from what the set declared', async () => {
        const { host: h } = host(document(), { acmeChartsRuntimePackage: bundle({ version: '9.9.9' }) });
        await expect(loadStagePackages(h)).rejects.toBeInstanceOf(StagePackagesFailed);
    });

    it('refuses a module that does not export its bundle', async () => {
        const { host: h } = host(document(), {});
        await expect(loadStagePackages(h)).rejects.toThrow("expects '@acme/scene.charts' to export a package bundle named 'acmeChartsRuntimePackage'");
    });

    it('refuses a package whose module the Stage did not report, instead of loading some other address', async () => {
        const { host: h, imported } = host(document({ modules: {} }));
        await expect(loadStagePackages(h)).rejects.toThrow("which this host's build does not include");
        expect(imported).toEqual([]);
    });

    it('refuses a module that cannot be imported, and adds no style sheet', async () => {
        const failing = host(document());
        failing.host.import = vi.fn().mockRejectedValue(new Error('404'));
        await expect(loadStagePackages(failing.host)).rejects.toThrow('could not be loaded');
        expect(failing.styles).toEqual([]);
    });
});
