// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import {
    DestinationKind, LogoKind, type ApplicationStructure, type DestinationReference, type DialogTemplate, type Form, type InstanceContribution, type Layout,
    type EffectiveConfiguration, type NavigationModel, type SceneElement, type Screen, type ScreenTemplate, type Theme, type UiProfile, type ExposureDeclaration,
} from '@cratis/scene.model';
import type { CommandOutcome, EffectiveNavItem, InteractionFinding, PackageCatalog } from '@cratis/scene.engine';
import { resolveStringsInElement } from '@cratis/scene.engine';
import {
    IconAdapterProvider,
    InteractionScope,
    SceneElementView,
    SliceCommandFieldsProvider,
    SliceInvokerProvider,
    createBrowserDispatcher,
    createHashSceneHistory,
    useSceneNavigation,
    type ComponentRegistry,
    type SceneNavigationContextValue,
} from '@cratis/scene.react';
import {
    ApplicationHost,
    ComponentName,
    LayoutThemeProvider,
    SlotName,
    composeApplicationScreen,
    defaultBlueprintThemes,
    effectiveShellOf,
    galleryScreenTemplates,
    externalComponent,
    type ApplicationDocument,
    type CompositionOptions,
} from '@cratis/scene.blueprint.default';
// The tokens (z-index scale, shadows, spacing) every Cratis Components overlay reads: a dialog or drawer is portaled to `body`, outside the
// shell, so without them its stacking falls back to `auto` and the page content paints over it.
import '@cratis/components/tokens';
import '@cratis/scene.blueprint.default/styles.css';
import { useStageRouteState } from './stageRoutes';
import { StageDataProvider, useStageData } from './stageData';
import { useComponentOutputsPublisher } from './stageComponentOutputs';
import { StageSliceDialog, stageCommandFormComponent, stageComponents } from './stageComponents';
import { useStrings } from './useStrings';
import { StageQueryBindings } from './stageQueryBindings';
import { ColorSchemeMirror, StageChromeProvider, stageActivityComponent, stageChromeComponents } from './StageChrome';
import { applicationChrome, type ApplicationIdentity, stageCatalogWith, stageProfile, stageRegistry } from './blueprint';
import { shellStartupProblems, stageShellSettings, themesWithAccent, type StageShellSettings } from './stageShell';
import { StageShellSettingsApplier, systemPrefersDark } from './StageShellSettings';
import { declaresRegions, stageNavigationModel } from './stageNavigationModel';
import { bindDataSources } from './stageDataSources';
import { navigateToScreen, screenArguments } from './stageNavigation';
import { registeredCommand, useStageCommandExecutor } from './commandExecutor';
import { createStageSliceInvoker, sliceFieldsFrom } from './stageSlices';
import { useStageSource } from './stageSource';
import { standaloneScreenTemplates } from './standaloneScreenTemplates';
import { withoutSeededSlots } from './withoutSeededSlots';
import { asDialogScreens } from './stageDialogScreens';
import { stageIconAdapters } from './stageIcons';
import './app.css';

export interface StageSceneApplication {
    uiProfiles?: UiProfile[];
    themes?: Theme[];
    layouts: Layout[];
    screenTemplates: ScreenTemplate[];
    dialogTemplates?: DialogTemplate[];
    screens: Screen[];
    exposures?: ExposureDeclaration[];
    instanceContributions?: InstanceContribution[];

    /** The navigation regions of the application and its modules; absent when the model declares none. */
    navigation?: NavigationModel;

    /** The modules, features and slices navigation targets and toolbar slice buttons resolve against. */
    structure?: ApplicationStructure;

    /** The JSON schema of every modeled command, by name, which a toolbar slice's task dialog reads its fields from. */
    commandSchemas?: Record<string, string>;
}

interface SceneCommandDetail {
    command: string;
}

interface SceneNavigateDetail {
    targetScreen: string;
    dialog?: string;
    destination?: DestinationReference;
}

/**
 * The registry a Stage renders with: Scene's core vocabulary, the libraries the profile lists (PrimeReact only then) and the default blueprint, then any
 * components the host adds, then the Stage's own table, action, form and chrome - which always win, because they
 * are what reach the running application.
 * @param components Components the host adds, such as a package an application composes with.
 * @returns The registry.
 */
export function stageApplicationRegistry(components: ComponentRegistry = {}): ComponentRegistry {
    return stageRegistry({ ...components, ...stageComponents, ...stageChromeComponents });
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

/**
 * The shell the application starts in - the one the profile's root template names, resolved by the Scene engine against the
 * default blueprint and the application's own templates, exactly as the Studio preview resolves it.
 *
 * @param scene The scene the Stage renders.
 * @param screenTemplates The templates the approved domain blueprints bring.
 * @returns The effective shell, with the engine's diagnostics.
 */
export function stageShellOf(scene: StageSceneApplication, screenTemplates?: ScreenTemplate[]) {
    const first = scene.screens[0];
    return effectiveShellOf(scene as ApplicationDocument, first, {
        profile: stageProfile(scene.uiProfiles),
        blueprintTemplates: standaloneScreenTemplates(screenTemplates?.length ? [...galleryScreenTemplates, ...screenTemplates] : galleryScreenTemplates),
    });
}

/**
 * What the Stage decides about composing a screen, on top of the composition it shares with the preview: its own chrome around the
 * shell (the effective navigation regions are Scene's), its command form, its status line and its data sources.
 */
export function stageComposition(scene: StageSceneApplication, locales: string[], locale: string, packageCatalog?: PackageCatalog, dictionary?: Record<string, string>, blueprintTemplates?: ScreenTemplate[], showFooter = true): CompositionOptions {
    const regions = declaresRegions(scene.navigation);
    const application = applicationIdentity(scene);
    return {
        profile: stageProfile(scene.uiProfiles),
        catalog: stageCatalogWith(packageCatalog),
        // A screen that fills a blueprint's template finds it among the default blueprint's and the approved domain blueprints'.
        // A template that fits inside another one is composed on its own: a Screenplay screen fills only the template it names (standaloneScreenTemplates).
        blueprintTemplates: standaloneScreenTemplates(blueprintTemplates?.length ? [...galleryScreenTemplates, ...blueprintTemplates] : galleryScreenTemplates),
        chrome: (screen, shell) => shell === ComponentName.FullPageShell
            ? { [SlotName.ConfigPanel]: applicationChrome(scene.screens, screen.name, locales, locale, application, regions, showFooter)[SlotName.ConfigPanel] }
            : applicationChrome(scene.screens, screen.name, locales, locale, application, regions, showFooter),
        form: commandFormElement,
        trailing: [externalComponent('activity', stageActivityComponent)],
        decorate: element => {
            const bound = bindDataSources(element);
            return dictionary ? resolveStringsInElement(bound, dictionary) : bound;
        },
    };
}

/**
 * What the application calls itself and how it draws its mark: the logo its navigation declares, otherwise the `identity` of the ui profile
 * (its name, an icon logo and whether the name is shown), so the model's own title replaces the Stage's. Undefined when the model says neither.
 */
export function applicationIdentity(scene: StageSceneApplication): ApplicationIdentity | undefined {
    const logo = scene.navigation?.application.logo;
    if (logo) return { title: logo.title, image: logo.image || undefined };

    const identity = stageProfile(scene.uiProfiles).identity;
    if (!identity?.name) return undefined;

    const mark = identity.logo;
    return {
        title: identity.name,
        image: mark?.kind === LogoKind.Image ? mark.image || undefined : undefined,
        icon: mark?.kind === LogoKind.Icon ? mark.icon : undefined,
        showName: identity.showName,
    };
}

/**
 * Composes one screen the way the application shows it: the shared composition - the shell for the screen's layout, the chrome in
 * its slots, the screen inside the template it names - with the Stage's own decisions.
 */
export function composeStageScreen(scene: StageSceneApplication, screen: Screen, locales: string[], locale: string, packageCatalog?: PackageCatalog, blueprintTemplates?: ScreenTemplate[]): SceneElement {
    const [dialog] = asDialogScreens([screen], ownTemplatesOf(scene));
    return composeApplicationScreen(scene as ApplicationDocument, dialog, stageComposition(scene, locales, locale, packageCatalog, undefined, blueprintTemplates)).element;
}

function ownTemplatesOf(scene: StageSceneApplication): { name: string }[] {
    return [...(scene.screenTemplates ?? []), ...(scene.dialogTemplates ?? [])];
}

/** The browser's local storage, where the shell keeps what a user chose; none where the browser refuses it. */
function browserStorage(): Storage | null {
    try {
        return globalThis.localStorage ?? null;
    } catch {
        return null;
    }
}

export interface AppProps {
    /** Components the host adds to the registry, beneath the Stage's own. */
    components?: ComponentRegistry;

    /**
     * What each package the host adds components for declares, by package name. A name an authored screen gives as
     * `Package.component`, or a bare name only such a package declares, resolves to the host's component through it.
     */
    packageCatalog?: PackageCatalog;

    /** The screen templates the approved domain blueprints bring; a screen's `template blueprint ...` reference resolves to one of them. */
    screenTemplates?: ScreenTemplate[];

    /** The dialog templates the approved domain blueprints bring, which an authored dialog destination can open. */
    dialogTemplates?: DialogTemplate[];

    /** Hides a navigation item the user may not see - hidden, never disabled, so nothing about it leaks. */
    isVisible?: (item: EffectiveNavItem) => boolean;
}

export function App({ components, packageCatalog, screenTemplates, dialogTemplates, isVisible }: AppProps = {}) {
    const source = useStageSource();
    const [scene, setScene] = useState<StageSceneApplication>();
    const [error, setError] = useState('');
    const registry = useMemo(() => stageApplicationRegistry(components), [components, scene]);
    // Routes and strings are read alongside the scene, not after it, so a command is registered by the time it is first used.
    const routeState = useStageRouteState();
    const strings = useStrings();

    useEffect(() => {
        const abort = new AbortController();
        source.scene(abort.signal)
            // The application's own dialog templates win a name a blueprint also declares.
            .then(loaded => setScene(dialogTemplates?.length
                ? { ...loaded, dialogTemplates: [...(loaded.dialogTemplates ?? []), ...dialogTemplates.filter(template => !loaded.dialogTemplates?.some(own => own.name === template.name))] }
                : loaded))
            .catch(reason => {
                if (!abort.signal.aborted) setError(reason instanceof Error ? reason.message : String(reason));
            });
        return () => abort.abort();
    }, [source, dialogTemplates]);

    if (error) return <main className='stage-message'><h1>Unable to render this Stage</h1><p>{error}</p></main>;
    if (!scene) return <main className='stage-message'><h1>Preparing the Stage</h1></main>;
    return <StageApplication scene={scene} registry={registry} routeState={routeState} strings={strings} packageCatalog={packageCatalog} screenTemplates={screenTemplates} isVisible={isVisible} />;
}

interface StageApplicationProps {
    scene: StageSceneApplication;
    registry: ComponentRegistry;
    routeState: ReturnType<typeof useStageRouteState>;
    strings: ReturnType<typeof useStrings>;
    packageCatalog?: PackageCatalog;
    screenTemplates?: ScreenTemplate[];
    isVisible?: (item: EffectiveNavItem) => boolean;
}

/**
 * The application once its scene is known. Where the user is - the screen in the primary region, the screens in nested outlets,
 * the dialog on top, and the address that reaches it - and the composition of every screen are the shared application host's, the
 * one the Studio preview runs: destinations are resolved, placed in outlets and recorded in the URL fragment by Scene, and the
 * topbar and sidebar are the effective navigation regions. The Stage supplies what is its own: the shell chrome, the data and
 * the commands the screens run against.
 */
function StageApplication({ scene, registry, routeState, strings, packageCatalog, screenTemplates, isVisible }: StageApplicationProps) {
    const history = useMemo(() => createHashSceneHistory(), []);
    const [prefersDark, setPrefersDark] = useState(systemPrefersDark);
    const initial = scene.screens[0]?.name ?? '';
    const profile = useMemo(() => stageProfile(scene.uiProfiles), [scene]);
    const shell = useMemo(() => initial ? stageShellOf(scene, screenTemplates) : undefined, [scene, screenTemplates, initial]);
    const shellProblems = useMemo(() => shell ? shellStartupProblems(profile, shell) : [], [profile, shell]);
    const shellSettings = useMemo(() => shell ? stageShellSettings(profile, shell, prefersDark) : undefined, [profile, shell, prefersDark]);
    const themes = useMemo(() => themesWithAccent(defaultBlueprintThemes, shellSettings?.accent), [shellSettings?.accent]);
    const showFooter = shellSettings?.showFooter ?? true;
    const composition = useMemo(
        () => stageComposition(scene, strings.locales, strings.locale, packageCatalog, strings.dictionary, screenTemplates, showFooter),
        [scene, strings.locales, strings.locale, strings.dictionary, packageCatalog, screenTemplates, showFooter]);
    const onSystemSchemeChange = useCallback((dark: boolean) => setPrefersDark(dark), []);
    const navigationModel = useMemo(() => stageNavigationModel(scene, isVisible, strings.dictionary), [scene, isVisible, strings.dictionary]);
    // Own templates come first: a template of the application wins over a blueprint's of the same name, as in composition.
    const document = useMemo(() => ({
        ...scene,
        screens: asDialogScreens(withoutSeededSlots(scene.screens, [...scene.screenTemplates, ...(scene.dialogTemplates ?? []), ...galleryScreenTemplates, ...(screenTemplates ?? [])]), ownTemplatesOf(scene)),
    }) as ApplicationDocument, [scene, screenTemplates]);

    if (!initial) {
        return (
            <main className='stage-message'>
                <h1>Nothing to show yet</h1>
                <p>This model has no slice carrying a command or a read model, so there is no screen to render.</p>
            </main>
        );
    }

    // A root template that cannot be the shell stops the application with the reason, rather than drawing it in some other shell.
    if (shellProblems.length > 0) {
        return (
            <main className='stage-message' role='alert' data-stage-startup-problem='root-template'>
                <h1>Unable to start this Stage</h1>
                <p>The ui profile&apos;s root template cannot be the application&apos;s shell.</p>
                <ul>{shellProblems.map(problem => <li key={problem}>{problem}</li>)}</ul>
            </main>
        );
    }

    return (
        <ApplicationHost
            application={document}
            composition={composition}
            navigationModel={navigationModel}
            registry={registry}
            initialScreen={initial}
            history={history}
            layoutConfigStorage={browserStorage()}
            scope={(navigation, children) => <StageScope scene={scene} routeState={routeState} strings={strings} navigation={navigation} themes={themes} shellSettings={shellSettings} onSystemSchemeChange={onSystemSchemeChange}>{children}</StageScope>}
            renderScreen={(element, configuration) => <StageScreen element={element} configuration={configuration} registry={registry} />}
            renderDialog={() => <StageSliceDialog />}
            renderDialogTemplate={(template, content, navigation) => <StageDialog template={template} content={content} registry={registry} strings={strings} close={() => navigation.closeDialog()} />}
        />
    );
}

interface StageScopeProps {
    scene: StageSceneApplication;
    routeState: ReturnType<typeof useStageRouteState>;
    strings: ReturnType<typeof useStrings>;
    navigation: SceneNavigationContextValue;
    themes: Theme[];
    shellSettings?: StageShellSettings;
    onSystemSchemeChange: (prefersDark: boolean) => void;
    children: ReactNode;
}

/**
 * What the Stage provides around the screen on the page and every dialog over it: the theme, the data and command scope, the
 * status line, and the dispatcher and invoker that point what the document does at the running application.
 */
function StageScope({ scene, routeState, strings, navigation, themes, shellSettings, onSystemSchemeChange, children }: StageScopeProps) {
    const [activity, setActivity] = useState('');
    const { navigate } = navigation;

    useEffect(() => setActivity(''), [navigation.currentScreen, navigation.currentDialog]);

    useEffect(() => {
        const command = (event: Event) => {
            const detail = (event as CustomEvent<SceneCommandDetail>).detail;
            setActivity(`The modeled command “${detail.command}” is ready for input.`);
        };
        globalThis.addEventListener('cratis.scene.command', command);
        return () => globalThis.removeEventListener('cratis.scene.command', command);
    }, []);

    useEffect(() => {
        // What a component announces when it is outside a navigation host, or names only a screen: the same
        // destinations, executed by the host, once the model has them.
        const announced = (event: Event) => {
            const detail = (event as CustomEvent<SceneNavigateDetail>).detail;
            const destination: DestinationReference | undefined = detail.destination
                ?? (detail.dialog ? { kind: DestinationKind.Dialog, dialog: detail.dialog } : detail.targetScreen ? { screen: detail.targetScreen } : undefined);
            if (!destination) return;
            if (destination.dialog !== undefined) {
                if (scene.dialogTemplates?.some(candidate => candidate.name === destination.dialog)) navigate(destination);
                else setActivity(`The modeled dialog “${destination.dialog}” is not available in this Stage runtime.`);
            } else if (destination.screen !== undefined && !scene.screens.some(candidate => candidate.name === destination.screen)) {
                setActivity(`The modeled screen “${destination.screen}” is not available.`);
            } else {
                navigate(destination);
            }
        };

        globalThis.addEventListener('cratis.scene.navigate', announced);
        return () => globalThis.removeEventListener('cratis.scene.navigate', announced);
    }, [navigate, scene]);

    return (
        <IconAdapterProvider registry={stageIconAdapters}>
            <LayoutThemeProvider themes={themes}>
                {shellSettings && <StageShellSettingsApplier settings={shellSettings} onSystemSchemeChange={onSystemSchemeChange} />}
                <ColorSchemeMirror />
                <StageDataProvider routes={routeState.routes} routesReady={routeState.ready} parameters={navigation.currentParameters} locale={strings.locale} locales={strings.locales} screen={navigation.primaryScreen ?? scene.screens[0]?.name ?? ''}>
                    <StageInteractions activity={activity} routesReady={routeState.ready} scene={scene} setActivity={setActivity} strings={strings}>
                        {children}
                    </StageInteractions>
                </StageDataProvider>
            </LayoutThemeProvider>
        </IconAdapterProvider>
    );
}

interface StageInteractionsProps {
    activity: string;
    routesReady: boolean;
    scene: StageSceneApplication;
    setActivity: (activity: string) => void;
    strings: ReturnType<typeof useStrings>;
    children: ReactNode;
}

function StageInteractions({ activity, routesReady, scene, setActivity, strings, children }: StageInteractionsProps) {
    const data = useStageData();
    const navigation = useSceneNavigation();
    const executeCommand = useStageCommandExecutor();
    const invoker = useMemo(() => createStageSliceInvoker(executeCommand, routesReady, data.refreshAfterCommand), [executeCommand, routesReady, data.refreshAfterCommand]);
    const fieldsFor = useMemo(() => sliceFieldsFrom(scene.commandSchemas), [scene.commandSchemas]);

    // Everything a document's interactions can do, pointed at the running application. The engine decides
    // what runs; this only says where a command goes and what a notification looks like.
    const dispatcher = createBrowserDispatcher({
        executeCommand: async (command, args): Promise<CommandOutcome> => {
            const commandType = registeredCommand(command);
            if (!commandType && !routesReady) {
                setActivity(`The modeled command “${command}” is waiting for Stage routes.`);
                return { isSuccess: false, validationErrors: [] };
            }

            if (!commandType) {
                setActivity(`The modeled command “${command}” is not registered by this Stage.`);
                return { isSuccess: false, validationErrors: [] };
            }

            const outcome = await executeCommand(command, args);
            if (outcome.messages.length > 0) setActivity(outcome.messages.join(' '));
            if (outcome.isSuccess) data.refreshAfterCommand();
            return { isSuccess: outcome.isSuccess, validationErrors: outcome.validationErrors, response: outcome.response };
        },
        // A navigation is a step the user can go back from, and its arguments are the screen's parameters - the
        // same address a row click or a deep link gives the screen.
        navigate: (screenName, args) => {
            if (scene.screens.some(candidate => candidate.name === screenName)) {
                navigateToScreen(screenName, screenArguments(args));
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

            navigation.navigate({ kind: DestinationKind.Dialog, dialog: template.name });
            return { isConfirmed: true };
        },
        closeDialog: () => navigation.closeDialog(),
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
            <SliceInvokerProvider invoker={invoker}>
                <SliceCommandFieldsProvider fieldsFor={fieldsFor}>
                    <InteractionScope dispatcher={dispatcher} context={{ resolve: data.resolveBinding, localize: key => strings.dictionary[key] ?? key }} attachments={[]} onFindings={reportFindings}>
                        {children}
                    </InteractionScope>
                </SliceCommandFieldsProvider>
            </SliceInvokerProvider>
        </StageChromeProvider>
    );
}

/** The composed screen, drawn against the Stage's own data: the rows, selections and routes its bindings resolve through. */
function StageScreen({ element, configuration, registry }: { element: SceneElement; configuration: EffectiveConfiguration | undefined; registry: ComponentRegistry }) {
    const data = useStageData();
    const publishOutputs = useComponentOutputsPublisher();
    return (
        <>
            <StageQueryBindings element={element} />
            <SceneElementView element={element} registry={registry} resolveBinding={data.resolveBinding} queryResults={data.queryResults} onComponentOutputsChanged={publishOutputs} configuration={configuration} />
        </>
    );
}

interface StageDialogProps {
    template: DialogTemplate;
    content: SceneElement;
    registry: ComponentRegistry;
    strings: ReturnType<typeof useStrings>;
    close: () => void;
}

function StageDialog({ template, content, registry, strings, close }: StageDialogProps) {
    const data = useStageData();
    const publishOutputs = useComponentOutputsPublisher();
    const resolved = resolveStringsInElement(content, strings.dictionary);

    // A dialog is modal: it sits above the shell, it is reachable with the pointer wherever the page is scrolled to, and
    // Escape closes it - which, through the navigation host, goes back to where it was opened from.
    useEffect(() => {
        const escape = (event: KeyboardEvent) => {
            if (event.key === 'Escape') close();
        };
        globalThis.addEventListener('keydown', escape);
        return () => globalThis.removeEventListener('keydown', escape);
    }, [close]);

    return (
        <div className='stage-dialog-backdrop'>
            <section role='dialog' aria-modal='true' aria-label={template.displayName ?? template.name} className='stage-dialog'>
                <button type='button' onClick={close}>Close</button>
                <SceneElementView element={resolved} registry={registry} resolveBinding={data.resolveBinding} queryResults={data.queryResults} onComponentOutputsChanged={publishOutputs} />
            </section>
        </div>
    );
}
