// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PackageKind, type SceneElement, type Screen, type UiProfile } from '@cratis/scene.model';
import { hostProfilePackages, type PackageCatalog } from '@cratis/scene.engine';
import { coreComponents, type ComponentRegistry } from '@cratis/scene.react';
import { authoredNavigationEntries, navigationPathTo, navigationTree, unlistedNavigationEntries } from './navigationTree';
import { screenTitle } from './screenTitle';
import { humanizeScreenName } from './humanize';
import { cratisComponents, cratisComponentsPackageManifest } from '@cratis/scene.components/runtime';
import {
    ComponentName,
    SlotName,
    blueprintPackageCatalog,
    defaultBlueprintComponents,
    defaultBlueprintName,
    defaultBlueprintThemes,
    externalComponent,
    navigationElement,
    previewComponents,
    previewProfile,
} from '@cratis/scene.blueprint.default';

/** The qualified name Stage registers its locale picker under. */
export const stageLocaleComponent = 'Stage:locale';

/** The package a host renders with when the profile lists no component library: the Cratis component library, built on React Aria. */
export const stageBaselinePackage = 'Cratis.Components';

/** The component library the profile opts in to by listing it; the application registers its support only then. */
export const primeReactPackage = 'PrimeReact';

/**
 * A component library a profile opted in to, registered by the host: the components it registers and what it declares.
 */
export interface StageLibrary {
    /** The components the library adds to the registry. */
    components: ComponentRegistry;

    /** What the library declares, so a bare name resolves to it. */
    declared: string[];
}

const loadedLibraries = new Map<string, StageLibrary>();

/** Whether the profiles the scene declares list PrimeReact. */
export function listsPrimeReact(profiles: UiProfile[] | undefined): boolean {
    return previewProfile(profiles).packages.includes(primeReactPackage);
}

/**
 * Registers a component library a profile opts in to. The application or live Stage that installs the library registers it
 * before rendering; the runtime itself never imports one, so it stays fully usable with none registered.
 *
 * @param name The package name the library is listed under in a profile.
 * @param library The components the library adds and what it declares.
 */
export function registerStageLibrary(name: string, library: StageLibrary): void {
    loadedLibraries.set(name, library);
}

/**
 * The profile a scene renders against: the shared one - the model's own `ui profile` with the packages every host renders with
 * admitted - ordered core, then Cratis.Components, then the libraries the profile lists (PrimeReact only when it lists it), then
 * the blueprint, so the blueprint's component still wins a name another package also declares.
 *
 * @param profiles The profiles the scene declares.
 * @returns The profile to resolve component names with.
 */
export function stageProfile(profiles: UiProfile[] | undefined): UiProfile {
    const profile = previewProfile(profiles);
    return {
        ...profile,
        name: profiles?.length ? profile.name : 'Stage',
        packages: hostProfilePackages(profile.packages, {
            baseline: stageBaselinePackage,
            catalog: { [defaultBlueprintName]: PackageKind.Blueprint },
        }),
    };
}

/** What each package every Stage declares, so a bare name resolves to the package that provides it. */
export const stageCatalog: PackageCatalog = {
    ...blueprintPackageCatalog,
    [stageBaselinePackage]: cratisComponentsPackageManifest.components,
};

/** Every component the Stage can render: Scene's core, the Cratis component library, the libraries a profile opted in to, the default blueprint's shell, and the template wrapper the shared composition places screens in. */
export function stageRegistry(overrides: ComponentRegistry): ComponentRegistry {
    const libraries = [...loadedLibraries.values()].reduce<ComponentRegistry>((all, library) => ({ ...all, ...library.components }), {});
    return { ...coreComponents, ...cratisComponents, ...libraries, ...defaultBlueprintComponents, ...previewComponents, ...overrides };
}

/**
 * What every package the application renders with declares: the Stage's own packages, and the components of the
 * custom packages the application approved.
 *
 * A custom package never replaces what a Stage package declares; it only adds its own entry.
 *
 * @param approved What each approved custom package declares, by package name.
 * @returns The catalog component names are resolved against.
 */
export function stageCatalogWith(approved: PackageCatalog | undefined): PackageCatalog {
    const libraries = Object.fromEntries([...loadedLibraries].map(([name, library]) => [name, library.declared]));
    return { ...approved, ...stageCatalog, ...libraries };
}

/** The qualified name Stage registers its navigation tree under. */
export const stageNavigationTreeComponent = 'Stage:navigationTree';

/** The name the application shows in its topbar and sidebar when the model gives it no logo. */
export const stageApplicationName = 'Cratis Stage';

function logo(id: string, title: string, image?: string): SceneElement {
    return externalComponent(id, ComponentName.Logo, { label: title, initials: title.slice(0, 1).toUpperCase() || 'S', targetScreen: '', ...(image ? { lightImageUrl: image } : {}) });
}

/**
 * The chrome the blueprint's application shell wraps every screen in, besides the screen itself.
 *
 * When the model declares navigation regions, the topbar and sidebar draw the *effective* navigation of the module the user is in
 * (Scene's regions: inherit, extend, override) and nothing here lists items. A model that declares none keeps what it always had:
 * the entries its screens authored as `Navigation` contributions - or one flat list of its screens - as the menu, and the topbar and
 * sidebar draw no region of their own.
 */
export function applicationChrome(screens: Screen[], activeScreen: string, locales: string[], locale: string, application?: { title: string; image?: string }, regions = false, showFooter = true, showThemeSwitcher = true): Record<string, SceneElement[]> {
    const first = screens[0]?.name ?? '';
    const title = application?.title ?? stageApplicationName;
    const authored = authoredNavigationEntries(screens);
    // A screen the model gave no navigation entry stays reachable - under its own name, in the module and feature it was
    // declared in, after the authored entries - so adding one entry never hides the rest of the application.
    const listed = new Set(authored.map(entry => entry.targetScreen));
    const unlisted = unlistedNavigationEntries(screens, listed, 'Other screens');
    const tree = navigationTree([...authored, ...(authored.length > 0 ? unlisted : [])]);
    const entries = screens.map((screen, index) => navigationElement({ label: screenTitle(screen), targetScreen: screen.name, icon: 'pi pi-table', group: 'Screens', order: index }, activeScreen));
    const activeScreenModel = screens.find(screen => screen.name === activeScreen);
    const activeTitle = activeScreenModel ? screenTitle(activeScreenModel) : humanizeScreenName(activeScreen);
    const authoredTrail = navigationPathTo(navigationTree(authored), activeScreen);
    // A screen no entry lists is still somewhere in the hierarchy when the model recorded where it was declared.
    const placed = screens.find(screen => screen.name === activeScreen)?.path?.length ? navigationPathTo(tree, activeScreen) : [];
    const trail = authoredTrail.length > 0 ? authoredTrail : placed;
    const menu = authored.length > 0
        ? externalComponent('menu-hierarchy', stageNavigationTreeComponent, { nodes: tree, activeScreen })
        : externalComponent('menu-screens', ComponentName.Menu, { title: 'Screens', label: 'Screens' }, { items: entries });
    const end: SceneElement[] = [];
    if (locales.length > 1) end.push(externalComponent('topbar-locale', stageLocaleComponent, { locales, locale }));
    if (showThemeSwitcher && defaultBlueprintThemes.length > 1) end.push(externalComponent('topbar-theme', ComponentName.ThemeSwitcher, { label: 'Theme' }));
    const regionNavigation = { regionNavigation: regions };

    return {
        [SlotName.Topbar]: [externalComponent('topbar', ComponentName.Topbar, regionNavigation, { logo: [logo('topbar-logo', title, application?.image)], end })],
        [SlotName.Sidebar]: [externalComponent('sidebar', ComponentName.Sidebar, regionNavigation, { logo: [logo('sidebar-logo', title, application?.image)] })],
        ...(regions ? {} : { [SlotName.Menu]: [menu] }),
        [SlotName.Breadcrumb]: [externalComponent('breadcrumb', ComponentName.Breadcrumb, {
            homeTargetScreen: first,
            items: (trail.length > 0 ? trail : [activeTitle]).map(label => ({ label })),
        })],
        ...(showFooter ? { [SlotName.Footer]: [externalComponent('footer', ComponentName.Footer, { text: title })] } : {}),
        [SlotName.ConfigPanel]: [externalComponent(ComponentName.ConfigPanel, ComponentName.ConfigPanel, {
            title: 'Settings',
            themes: defaultBlueprintThemes.map(theme => ({ name: theme.name, label: theme.name, isDark: theme.isDark ?? false })),
        })],
    };
}

/** The hash a screen is addressed by - what the blueprint's breadcrumb links point at. */
export function screenHash(name: string): string {
    return `#/${encodeURIComponent(name)}`;
}

/** The screen name a hash addresses, or undefined when it addresses none. Parameters after `?` are not part of it. */
export function screenFromHash(hash: string): string | undefined {
    const match = /^#\/([^?]+)/.exec(hash);
    if (!match) return undefined;
    try {
        return decodeURIComponent(match[1]);
    } catch {
        return undefined;
    }
}
