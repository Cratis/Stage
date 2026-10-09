// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, useContext, type ReactNode } from 'react';
import type { FlowArrangement } from '@cratis/scene.model';
import { evaluateFlowArrangement } from '@cratis/scene.engine';
import type { ComponentRegistry, RegisteredComponentProps } from '@cratis/scene.react';
import { useSceneTheme } from '@cratis/scene.react';
import { useEffect } from 'react';
import { FlowArrangementView } from './FlowArrangementView';
import { useSizeClass } from './useSizeClass';
import { stageLocaleComponent } from './blueprint';

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

/** The name the template wrapper is registered under. */
export const stageTemplateComponent = 'Stage:template';

/** The name the status line is registered under. */
export const stageActivityComponent = 'Stage:activity';

function isFlowArrangement(arrangement: unknown): arrangement is FlowArrangement {
    return !!arrangement && typeof arrangement === 'object' && 'root' in arrangement;
}

/**
 * A screen's content inside the screen template it names.
 *
 * The template's own `arrangement flow` positions its slots - header, body, aside - exactly as the document
 * wrote it, evaluated for the window's current size class. A template with no flow arrangement stacks them.
 * This sits inside the blueprint shell's content slot, so the shell, not this, owns the chrome around it.
 */
export function StageTemplate({ element, slots }: RegisteredComponentProps) {
    const sizeClass = useSizeClass();
    const arrangement = (element.properties as Record<string, unknown> | undefined)?.arrangement;
    const nodes: Record<string, ReactNode> = {};
    for (const [name, content] of Object.entries(slots)) {
        nodes[name] = <section className='stage-slot' data-slot={name}>{content}</section>;
    }

    return (
        <div className='stage-template' data-scene-id={element.id}>
            {isFlowArrangement(arrangement)
                ? <FlowArrangementView node={evaluateFlowArrangement(arrangement, sizeClass)} slots={nodes} />
                : Object.entries(nodes).map(([name, node]) => <div key={name}>{node}</div>)}
        </div>
    );
}

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
    [stageTemplateComponent]: StageTemplate,
    [stageLocaleComponent]: StageLocale,
    [stageActivityComponent]: StageActivity,
};
