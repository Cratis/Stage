// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useMemo, useState } from 'react';
import type { DialogTemplate, Layout, SceneElement, Screen, ScreenTemplate, Theme, UiProfile } from '@cratis/scene.model';
import type { CommandOutcome, InteractionFinding } from '@cratis/scene.engine';
import { resolveStringsInElement } from '@cratis/scene.engine';
import { InteractionScope, SceneElementView, createBrowserDispatcher } from '@cratis/scene.react';
import {
    ComponentName,
    LayoutConfigProvider,
    LayoutThemeProvider,
    SlotName,
    externalComponent,
    shellComponentForLayout,
} from '@cratis/scene.blueprint.default';
import '@cratis/scene.blueprint.default/styles.css';
import { useStageRoutes, type StageRoutes } from './stageRoutes';
import { StageDataProvider, useStageData } from './stageData';
import { stageComponents } from './stageComponents';
import { useStrings } from './useStrings';
import { ColorSchemeMirror, StageChromeProvider, stageActivityComponent, stageChromeComponents, stageTemplateComponent } from './StageChrome';
import { applicationChrome, resolveNames, screenFromHash, screenHash, stageProfile, stageRegistry, templateFor } from './blueprint';
import './app.css';

export interface StageSceneApplication {
    uiProfiles?: UiProfile[];
    themes?: Theme[];
    layouts: Layout[];
    screenTemplates: ScreenTemplate[];
    dialogTemplates?: DialogTemplate[];
    screens: Screen[];
}

interface SceneCommandDetail {
    command: string;
}

interface SceneNavigateDetail {
    targetScreen: string;
}

const endpoint = 'stage/scene';

const registry = stageRegistry({ ...stageComponents, ...stageChromeComponents });

/**
 * Composes one screen the way the default blueprint composes its own: the application shell for the screen's
 * layout, the chrome every screen shares in the shell's slots, and the screen - inside the screen template it
 * names, when it names one - in the shell's content slot.
 *
 * A layout the selected package set does not provide remains unresolved in the rendered tree. Showing that
 * missing component is deliberate: silently substituting a default shell would hide a package/profile problem
 * and produce a screen the model did not ask for.
 */
export function composeStageScreen(scene: StageSceneApplication, screen: Screen, locales: string[], locale: string): SceneElement {
    const profile = stageProfile(scene.uiProfiles);
    const template = templateFor(scene.screenTemplates, screen);
    const slotEntries = Object.entries(screen.slotContent);
    const content: SceneElement[] = template
        ? [externalComponent(`template-${screen.name}`, stageTemplateComponent, { arrangement: template.arrangement }, screen.slotContent)]
        : slotEntries.flatMap(([, elements]) => elements);

    const shell = shellComponentForLayout(screen.layout) ?? screen.layout;
    const chrome = shell === ComponentName.FullPageShell
        ? { [SlotName.ConfigPanel]: applicationChrome(scene.screens, screen.name, locales, locale)[SlotName.ConfigPanel] }
        : applicationChrome(scene.screens, screen.name, locales, locale);
    const composed = externalComponent(`screen-${screen.name}`, shell, { screenName: screen.name }, {
        ...chrome,
        [SlotName.Content]: [...content, externalComponent('activity', stageActivityComponent)],
    });

    return resolveNames(composed, profile);
}

export function App() {
    const [scene, setScene] = useState<StageSceneApplication>();
    const routes = useStageRoutes();
    const strings = useStrings();
    const [selectedScreen, setSelectedScreen] = useState(() => screenFromHash(globalThis.location?.hash ?? '') ?? '');
    const [error, setError] = useState('');
    const [activity, setActivity] = useState('');

    const select = (name: string) => {
        setSelectedScreen(name);
        setActivity('');
        if (globalThis.location && screenFromHash(globalThis.location.hash) !== name) globalThis.history?.replaceState(null, '', screenHash(name));
    };

    useEffect(() => {
        const abort = new AbortController();
        fetch(endpoint, { signal: abort.signal })
            .then(response => {
                if (!response.ok) throw new Error(`The Stage returned ${response.status}.`);
                return response.json() as Promise<StageSceneApplication>;
            })
            .then(application => {
                setScene(application);
                setSelectedScreen(current => application.screens.some(screen => screen.name === current) ? current : application.screens[0]?.name ?? '');
            })
            .catch(reason => {
                if (!abort.signal.aborted) setError(reason instanceof Error ? reason.message : String(reason));
            });
        return () => abort.abort();
    }, []);

    useEffect(() => {
        const hashChanged = () => {
            const name = screenFromHash(globalThis.location.hash);
            if (name && scene?.screens.some(screen => screen.name === name)) {
                setSelectedScreen(name);
                setActivity('');
            }
        };
        const command = (event: Event) => {
            const detail = (event as CustomEvent<SceneCommandDetail>).detail;
            setActivity(`The modeled command “${detail.command}” is ready for input.`);
        };
        const navigate = (event: Event) => {
            const detail = (event as CustomEvent<SceneNavigateDetail>).detail;
            if (scene?.screens.some(screen => screen.name === detail.targetScreen)) {
                select(detail.targetScreen);
            } else {
                setActivity(`The modeled screen “${detail.targetScreen}” is not available.`);
            }
        };

        globalThis.addEventListener('hashchange', hashChanged);
        globalThis.addEventListener('cratis.scene.command', command);
        globalThis.addEventListener('cratis.scene.navigate', navigate);
        return () => {
            globalThis.removeEventListener('hashchange', hashChanged);
            globalThis.removeEventListener('cratis.scene.command', command);
            globalThis.removeEventListener('cratis.scene.navigate', navigate);
        };
    }, [scene]);

    const screen = scene?.screens.find(candidate => candidate.name === selectedScreen) ?? scene?.screens[0];
    const element = useMemo(
        () => scene && screen ? resolveStringsInElement(composeStageScreen(scene, screen, strings.locales, strings.locale), strings.dictionary) : undefined,
        [scene, screen, strings.locales, strings.locale, strings.dictionary],
    );

    if (error) return <main className='stage-message'><h1>Unable to render this Stage</h1><p>{error}</p></main>;
    if (!scene) return <main className='stage-message'><h1>Preparing the Stage</h1></main>;
    if (!screen || !element) {
        return (
            <main className='stage-message'>
                <h1>Nothing to show yet</h1>
                <p>This model has no slice carrying a command or a read model, so there is no screen to render.</p>
            </main>
        );
    }

    return (
        <LayoutConfigProvider>
            <LayoutThemeProvider>
                <ColorSchemeMirror />
                <StageDataProvider routes={routes} locale={strings.locale} locales={strings.locales} screen={screen.name}>
                    <StageSceneView
                        activity={activity}
                        element={element}
                        routes={routes}
                        scene={scene}
                        setActivity={setActivity}
                        select={select}
                        strings={strings} />
                </StageDataProvider>
            </LayoutThemeProvider>
        </LayoutConfigProvider>
    );
}

interface StageSceneViewProps {
    activity: string;
    element: SceneElement;
    routes: StageRoutes | undefined;
    scene: StageSceneApplication;
    select: (screen: string) => void;
    setActivity: (activity: string) => void;
    strings: ReturnType<typeof useStrings>;
}

function StageSceneView({ activity, element, routes, scene, select, setActivity, strings }: StageSceneViewProps) {
    const data = useStageData();

    // Everything a document's interactions can do, pointed at the running application. The engine decides
    // what runs; this only says where a command goes and what a notification looks like.
    const dispatcher = createBrowserDispatcher({
        executeCommand: async (command, args): Promise<CommandOutcome> => {
            const route = routes?.commands[command];
            if (!route) {
                setActivity(`The modeled command “${command}” is not registered by this Stage.`);
                return { isSuccess: false, validationErrors: [] };
            }

            const response = await fetch(route, {
                method: 'POST',
                headers: { 'content-type': 'application/json' },
                body: JSON.stringify(args),
            });

            if (!response.ok) {
                setActivity(`“${command}” failed with ${response.status}.`);
                return { isSuccess: false, validationErrors: [] };
            }

            const result = await response.json() as { isSuccess?: boolean; validationResults?: { message: string; members: string[] }[] };

            // Arc answers a rejected command with 200 and the reasons, so the status alone does not say
            // whether it worked. Reading the body is what makes 'on failure' mean what the document says.
            const validationErrors = (result.validationResults ?? []).map(_ => ({ member: _.members[0] ?? '', message: _.message }));
            if (validationErrors.length > 0) setActivity(validationErrors.map(_ => _.message).join(' '));
            if (validationErrors.length === 0) data.refreshQuery();

            return { isSuccess: result.isSuccess !== false && validationErrors.length === 0, validationErrors };
        },
        navigate: screenName => {
            if (scene.screens.some(candidate => candidate.name === screenName)) {
                select(screenName);
            } else {
                setActivity(`The modeled screen “${screenName}” is not available.`);
            }
        },
        navigateBack: () => globalThis.history?.back(),
        openDialog: async dialogTemplate => {
            setActivity(`The modeled dialog “${dialogTemplate}” is not available in this Stage runtime.`);
            return { isConfirmed: false };
        },
        closeDialog: () => setActivity('Dialog closed.'),
        confirm: async message => globalThis.confirm(message),
        setState: (target, value) => data.setState(target, value),
        notify: (level, message) => setActivity(`${level}: ${message}`),
        refreshQuery: async query => data.refreshQuery(query),
        raise: async trigger => setActivity(`The modeled trigger “${trigger}” is not available in this Stage runtime.`),
    });

    // A finding is what the engine could not do. Showing it is the whole difference between an interaction
    // that is broken and one that silently does nothing.
    const reportFindings = (findings: InteractionFinding[]) =>
        setActivity(findings.map(finding => finding.detail).join(' '));

    return (
        <StageChromeProvider state={{ locales: strings.locales, locale: strings.locale, setLocale: strings.setLocale, activity }}>
            <InteractionScope dispatcher={dispatcher} context={{ resolve: data.resolveBinding, localize: key => strings.dictionary[key] ?? key }} attachments={[]} onFindings={reportFindings}>
                <SceneElementView element={element} registry={registry} resolveBinding={data.resolveBinding} />
            </InteractionScope>
        </StageChromeProvider>
    );
}
