// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, useContext, type ReactNode } from 'react';
import type { ComponentRegistry, RegisteredComponentProps } from '@cratis/scene.react';
import { SceneElementView, SceneNavigationContextInternal, useSceneOutlets, useSceneTheme } from '@cratis/scene.react';
import { useEffect } from 'react';
import { ColorScheme, useLayoutConfig, defaultBlueprintThemes } from '@cratis/scene.blueprint.default';
import { screenHash, stageLocaleComponent, stageNavigationTreeComponent, stageThemeToggleComponent } from './blueprint';
import { opensElsewhere, type NavigationNode } from './navigationTree';
import { useStageData } from './stageData';
import { externalRouteHref } from './externalRouteHref';
import { StageScreenTables } from './stageScreenTables';

/** What the Stage's own components need that the document itself does not carry. */
export interface StageChromeState {
    locales: string[];
    locale: string;
    setLocale: (locale: string) => void;
    activity: string;
}

const emptyState: StageChromeState = { locales: [], locale: '', setLocale: () => undefined, activity: '' };

const StageChromeContext = createContext<StageChromeState>(emptyState);

/** Provides {@link StageChromeState} to the Stage's own components. */
export function StageChromeProvider({ state, children }: { state: StageChromeState; children: ReactNode }) {
    return <StageChromeContext.Provider value={state}>{children}</StageChromeContext.Provider>;
}

/** The name the status line is registered under. */
export const stageActivityComponent = 'Stage:activity';

/** The language picker in the topbar, shown only when the model declares more than one. */
export function StageLocale() {
    const { locales, locale, setLocale } = useContext(StageChromeContext);
    if (locales.length < 2) return null;

    return (
        <select className='stage-locale' aria-label='Locale' value={locale} onChange={event => setLocale(event.target.value)}>
            {locales.map(candidate => <option key={candidate} value={candidate}>{candidate}</option>)}
        </select>
    );
}

/**
 * The theme as one toggle: an icon button that shows a moon in a light theme and a sun in a dark one, and switches to the other, with
 * nothing else. The themes it switches between are the light and the dark one the Stage ships.
 */
export function StageThemeToggle({ element }: RegisteredComponentProps) {
    const { config, setThemeName, setColorScheme } = useLayoutConfig();
    const current = defaultBlueprintThemes.find(theme => theme.name === config.themeName);
    const dark = current?.isDark ?? config.colorScheme === ColorScheme.Dark;
    const target = defaultBlueprintThemes.find(theme => (theme.isDark ?? false) !== dark);
    if (!target) return null;

    const action = dark ? 'Switch to a light theme' : 'Switch to a dark theme';
    return (
        <div className='stage-theme-toggle' data-scene-id={element.id}>
            <button
                type='button'
                className='stage-theme-toggle__button'
                aria-label={action}
                title={action}
                onClick={() => { setThemeName(target.name); setColorScheme(target.isDark ? ColorScheme.Dark : ColorScheme.Light); }}>
                <svg width='18' height='18' viewBox='0 0 24 24' fill='none' stroke='currentColor' strokeWidth='2' strokeLinecap='round' strokeLinejoin='round' aria-hidden='true'>
                    {dark
                        ? <><circle cx='12' cy='12' r='4' /><path d='M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41' /></>
                        : <path d='M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z' />}
                </svg>
            </button>
        </div>
    );
}

/**
 * The application's navigation as the hierarchy the model declared: a group per module and feature, nested as deep
 * as the model nests them, each entry linking to the screen it opens.
 */
export function StageNavigationTree({ element }: RegisteredComponentProps) {
    const properties = (element.properties ?? {}) as { nodes?: NavigationNode[]; activeScreen?: string };
    return (
        <nav className='stage-navigation-tree' aria-label='Navigation' data-scene-id={element.id}>
            <NavigationLevel nodes={properties.nodes ?? []} activeScreen={properties.activeScreen} />
        </nav>
    );
}

function NavigationLevel({ nodes, activeScreen }: { nodes: NavigationNode[]; activeScreen?: string }) {
    return (
        <ul role='tree'>
            {nodes.map(node => node.screen !== undefined
                ? (
                    <li key={`${node.screen}:${node.label}`} role='treeitem'>
                        <NavigationEntryLink node={node} screen={node.screen} activeScreen={activeScreen} />
                    </li>
                )
                : (
                    <li key={node.label} role='treeitem' aria-expanded='true'>
                        <span className='stage-navigation-group'>{node.label}</span>
                        <NavigationLevel nodes={node.children} activeScreen={activeScreen} />
                    </li>
                ))}
        </ul>
    );
}

/**
 * One navigation entry as the model authored it.
 *
 * The presentation hint decides what is shown - `icon`, `text` or `both` (the default); an icon-only entry keeps its
 * label as its accessible name. An entry that opens somewhere a plain link does not reach - an outlet, a dialog - is
 * executed through the Scene navigation host, the same as a toolbar item; an external route is an ordinary link.
 */
function NavigationEntryLink({ node, screen, activeScreen }: { node: NavigationNode; screen: string; activeScreen?: string }) {
    const navigation = useContext(SceneNavigationContextInternal);
    const mode = node.presentation === 'icon' || node.presentation === 'text' ? node.presentation : 'both';
    const destination = node.destination;
    const external = destination?.kind === 'external';
    const execute = destination && navigation && !external && opensElsewhere(destination)
        ? (event: { preventDefault: () => void }) => {
            event.preventDefault();
            navigation.navigate(destination);
        }
        : undefined;
    const href = external ? externalRouteHref(destination.route) : screenHash(screen);
    const refused = external && href === undefined;

    return (
        <a
            href={href}
            rel={external && !refused ? 'noreferrer' : undefined}
            aria-disabled={refused ? 'true' : undefined}
            aria-current={screen === activeScreen ? 'page' : undefined}
            aria-label={mode === 'icon' ? node.label : undefined}
            data-presentation={node.presentation}
            data-icon={node.icon}
            onClick={execute}>
            {mode !== 'text' && node.icon && <span className='stage-navigation-icon' aria-hidden='true' data-icon-name={node.icon} />}
            {mode !== 'icon' && node.label}
        </a>
    );
}

/**
 * The `core:outlet` the Stage renders: the screen the Scene navigation host placed in the named outlet, drawn
 * inside the Stage's own data scope so its queries, selections and commands work as they do on a primary screen.
 *
 * Scene's own outlet renders the placed screen in a scope of its own, which cannot see what the Stage resolves
 * queries and selections with, so the Stage draws the same region itself - including the `Close` button of an outlet the host
 * put in a slot the shell declares (`slot side`).
 */
export function StageOutlet({ element }: RegisteredComponentProps) {
    const name = typeof element.properties.name === 'string' ? element.properties.name : '';
    const navigation = useContext(SceneNavigationContextInternal);
    const outlets = useSceneOutlets();
    const data = useStageData();
    const placed = navigation?.outlets[name];
    const screen = placed && outlets ? outlets.screens[placed] : undefined;
    const closable = element.properties.closable === true;

    return (
        <div data-scene-outlet={name} data-scene-outlet-screen={placed}>
            {closable && placed && (
                <div className='scene-outlet-bar'>
                    <button type='button' className='scene-outlet-close' aria-label='Close' onClick={() => navigation?.closeOutlet(name)}>Close</button>
                </div>
            )}
            {screen && outlets && <StageScreenTables element={screen}><SceneElementView element={screen} registry={outlets.registry} resolveBinding={data.resolveBinding} /></StageScreenTables>}
        </div>
    );
}

/** What the last interaction did, or could not do. */
export function StageActivity() {
    const { activity } = useContext(StageChromeContext);
    return activity ? <p className='stage-activity' role='status'>{activity}</p> : null;
}

/**
 * Mirrors the active theme's colour scheme and page colors onto the document.
 *
 * The theme provider scopes its tokens to its own element, but PrimeReact portals a dialog or an overlay to
 * `body`, outside that element, and the page behind the shell is `body` too. Without this, a dark theme would
 * paint the shell dark, leave the page around it light and open every dialog light.
 */
export function ColorSchemeMirror() {
    const theme = useSceneTheme();
    const background = theme?.tokens?.['surface.background'];
    const color = theme?.tokens?.['text.color'];
    useEffect(() => {
        const scheme = theme?.isDark ? 'dark' : 'light';
        document.documentElement.setAttribute('data-scene-color-scheme', scheme);
        document.body.setAttribute('data-scene-color-scheme', scheme);
        document.body.style.backgroundColor = background ?? '';
        document.body.style.color = color ?? '';
    }, [theme?.isDark, background, color]);

    return null;
}

/** The Stage's own components, to merge over the blueprint's. */
export const stageChromeComponents: ComponentRegistry = {
    [stageLocaleComponent]: StageLocale,
    [stageThemeToggleComponent]: StageThemeToggle,
    [stageNavigationTreeComponent]: StageNavigationTree,
    [stageActivityComponent]: StageActivity,
    'core:outlet': StageOutlet,
};
