// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useMemo, useState } from 'react';
import type { DialogTemplate, Form, Layout, SceneElement, Screen, ScreenTemplate, Theme, UiProfile } from '@cratis/scene.model';
import type { CommandOutcome, InteractionFinding } from '@cratis/scene.engine';
import { resolveStringsInElement } from '@cratis/scene.engine';
import { InteractionScope, SceneElementView, createBrowserDispatcher, type ComponentRegistry } from '@cratis/scene.react';
import {
    ComponentName,
    LayoutConfigProvider,
    LayoutThemeProvider,
    SlotName,
    externalComponent,
    shellComponentForLayout,
} from '@cratis/scene.blueprint.default';
import '@cratis/scene.blueprint.default/styles.css';
import { useStageRouteState, type StageRoutes } from './stageRoutes';
import { StageDataProvider, useStageData } from './stageData';
import { stageCommandFormComponent, stageComponents } from './stageComponents';
import { useStrings } from './useStrings';
import { ColorSchemeMirror, StageChromeProvider, stageActivityComponent, stageChromeComponents, stageTemplateComponent } from './StageChrome';
import { applicationChrome, resolveNames, screenFromHash, screenHash, stageProfile, stageRegistry, templateFor } from './blueprint';
import { bindDataSources } from './stageDataSources';
import { screenParameters } from './stageNavigation';
import { useStageSource } from './stageSource';
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

/**
 * The registry a Stage renders with: Scene's core vocabulary, PrimeReact and the default blueprint, then any
 * components the host adds, then the Stage's own table, action, form and chrome - which always win, because they
 * are what reach the running application.
 * @param components Components the host adds, such as a package an application composes with.
 * @returns The registry.
 */
export function stageApplicationRegistry(components: ComponentRegistry = {}): ComponentRegistry {
    return stageRegistry({ ...components, ...stageComponents, ...stageChromeComponents });
}

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
    const screenForms = screen.forms.map(form => commandFormElement(screen.name, form));
    const templateSlots = template ? mergeSlots(template.content, screen.slotContent, { forms: screenForms }) : undefined;
    const content: SceneElement[] = template
        ? [externalComponent(`template-${screen.name}`, stageTemplateComponent, { arrangement: template.arrangement }, templateSlots)]
        : [...slotEntries.flatMap(([, elements]) => elements), ...screenForms];

    const shell = shellComponentForLayout(screen.layout) ?? screen.layout;
    const chrome = shell === ComponentName.FullPageShell
        ? { [SlotName.ConfigPanel]: applicationChrome(scene.screens, screen.name, locales, locale)[SlotName.ConfigPanel] }
        : applicationChrome(scene.screens, screen.name, locales, locale);
    const composed = externalComponent(`screen-${screen.name}`, shell, { screenName: screen.name }, {
        ...chrome,
        [SlotName.Content]: [...content, externalComponent('activity', stageActivityComponent)],
    });

    return resolveNames(bindDataSources(composed), profile);
}

function commandFormElement(screenName: string, form: Form): SceneElement {
    return externalComponent(`form-${screenName}-${form.name}`, stageCommandFormComponent, {
        command: form.forCommand,
        label: form.name,
        generationMode: form.generationMode,
        layout: form.layout,
        fields: form.fields,
    });
}

function mergeSlots(...sources: (Record<string, SceneElement[]> | undefined)[]): Record<string, SceneElement[]> {
    const merged: Record<string, SceneElement[]> = {};
    for (const source of sources) {
        for (const [slot, elements] of Object.entries(source ?? {})) {
            merged[slot] = [...(merged[slot] ?? []), ...elements];
        }
    }

    return merged;
}

export interface AppProps {
    /** Components the host adds to the registry, beneath the Stage's own. */
    components?: ComponentRegistry;
}

export function App({ components }: AppProps = {}) {
    const source = useStageSource();
    const registry = useMemo(() => stageApplicationRegistry(components), [components]);
    const [scene, setScene] = useState<StageSceneApplication>();
    const routeState = useStageRouteState();
    const routes = routeState.routes;
    const strings = useStrings();
    const [selectedScreen, setSelectedScreen] = useState(() => screenFromHash(globalThis.location?.hash ?? '') ?? '');
    const [parameters, setParameters] = useState(() => screenParameters(globalThis.location?.hash ?? ''));
    const [error, setError] = useState('');
    const [activity, setActivity] = useState('');

    const select = (name: string) => {
        setSelectedScreen(name);
        setParameters({});
        setActivity('');
        if (globalThis.location && (screenFromHash(globalThis.location.hash) !== name || globalThis.location.hash.includes('?'))) globalThis.history?.replaceState(null, '', screenHash(name));
    };

    useEffect(() => {
        const abort = new AbortController();
        source.scene(abort.signal)
            .then(application => {
                setScene(application);
                setParameters(screenParameters(globalThis.location?.hash ?? ''));
                setSelectedScreen(current => {
                    const hashScreen = screenFromHash(globalThis.location?.hash ?? '');
                    if (hashScreen && application.screens.some(screen => screen.name === hashScreen)) return hashScreen;
                    return application.screens.some(screen => screen.name === current) ? current : application.screens[0]?.name ?? '';
                });
            })
            .catch(reason => {
                if (!abort.signal.aborted) setError(reason instanceof Error ? reason.message : String(reason));
            });
        return () => abort.abort();
    }, [source]);

    useEffect(() => {
        const hashChanged = () => {
            const name = screenFromHash(globalThis.location.hash);
            if (name && scene?.screens.some(screen => screen.name === name)) {
                setSelectedScreen(current => {
                    if (current !== name) setActivity('');
                    return name;
                });
                setParameters(screenParameters(globalThis.location.hash));
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
                <StageDataProvider routes={routes} routesReady={routeState.ready} parameters={parameters} locale={strings.locale} locales={strings.locales} screen={screen.name}>
                    <StageSceneView
                        activity={activity}
                        element={element}
                        registry={registry}
                        routes={routes}
                        routesReady={routeState.ready}
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
    registry: ComponentRegistry;
    routes: StageRoutes | undefined;
    routesReady: boolean;
    scene: StageSceneApplication;
    select: (screen: string) => void;
    setActivity: (activity: string) => void;
    strings: ReturnType<typeof useStrings>;
}

function StageSceneView({ activity, element, registry, routes, routesReady, scene, select, setActivity, strings }: StageSceneViewProps) {
    const data = useStageData();
    const [dialog, setDialog] = useState<DialogTemplate>();

    // Everything a document's interactions can do, pointed at the running application. The engine decides
    // what runs; this only says where a command goes and what a notification looks like.
    const dispatcher = createBrowserDispatcher({
        executeCommand: async (command, args): Promise<CommandOutcome> => {
            const route = routes?.commands[command];
            if (!route && !routesReady) {
                setActivity(`The modeled command “${command}” is waiting for Stage routes.`);
                return { isSuccess: false, validationErrors: [] };
            }

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
            if (validationErrors.length === 0) data.refreshAfterCommand();

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
            const template = scene.dialogTemplates?.find(candidate => candidate.name === dialogTemplate);
            if (!template) {
                setActivity(`The modeled dialog “${dialogTemplate}” is not available in this Stage runtime.`);
                return { isConfirmed: false };
            }

            setDialog(template);
            return { isConfirmed: true };
        },
        closeDialog: () => setDialog(undefined),
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
                {dialog && <StageDialog template={dialog} registry={registry} strings={strings} close={() => setDialog(undefined)} />}
            </InteractionScope>
        </StageChromeProvider>
    );
}

interface StageDialogProps {
    template: DialogTemplate;
    registry: ComponentRegistry;
    strings: ReturnType<typeof useStrings>;
    close: () => void;
}

function StageDialog({ template, registry, strings, close }: StageDialogProps) {
    const data = useStageData();
    const profile = stageProfile(undefined);
    const content = resolveStringsInElement(
        resolveNames(externalComponent(`dialog-${template.name}`, stageTemplateComponent, { arrangement: template.arrangement }, template.content ?? {}), profile),
        strings.dictionary,
    );

    return (
        <section role='dialog' aria-label={template.displayName ?? template.name} className='stage-dialog'>
            <button type='button' onClick={close}>Close</button>
            <SceneElementView element={content} registry={registry} resolveBinding={data.resolveBinding} />
        </section>
    );
}
