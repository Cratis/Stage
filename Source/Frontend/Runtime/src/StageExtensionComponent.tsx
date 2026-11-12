// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { createPortal } from 'react-dom';
import {
    ExtensionContextProvider,
    type ExtensionComponentDefinition,
    type ExtensionComponentKind,
    type ExtensionContextController,
    type ExtensionProperties,
    type ExtensionValues
} from '@cratis/scene.extensions';
import type { ExternalComponent } from '@cratis/scene.model';
import { useStageData } from './stageData';
import { loadExtensionDefinition, type ExtensionModuleImporter } from './stageExtensionModule';
import {
    createStageExtensionContext,
    defaultTenant,
    localeOf,
    readExtensionSettings,
    readExtensionTheme,
    readExtensionUser,
    type ExtensionComponentAddress
} from './stageExtensionContext';

/** The name a scene element is registered under when it places a component of an extension. */
export const stageExtensionComponent = 'Stage:extensionComponent';

/** A signal an extension component raised, with the component that raised it. */
export interface RaisedExtensionSignal extends ExtensionComponentAddress {
    signal: string;
    values: ExtensionValues;
}

/** What the component needs from the page it runs in. A host or a spec replaces these; the defaults are the browser's own. */
export interface StageExtensionServices {
    fetcher?: typeof fetch;
    importer?: ExtensionModuleImporter;

    /** Receives the signals components raise, which is how a screen's `on <signal>` interactions hear them. */
    onSignal?: (raised: RaisedExtensionSignal) => void;
}

const StageExtensionServicesContext = createContext<StageExtensionServices>({});

/** Supplies the services extension components below use. */
export function StageExtensionServicesProvider({ services, children }: { services: StageExtensionServices; children?: ReactNode }) {
    return <StageExtensionServicesContext.Provider value={services}>{children}</StageExtensionServicesContext.Provider>;
}

const strings = (value: unknown): readonly string[] => Array.isArray(value) ? value.filter((item): item is string => typeof item === 'string') : [];
const text = (element: ExternalComponent, name: string) => typeof element.properties[name] === 'string' ? element.properties[name] as string : '';
const record = (value: unknown): Record<string, unknown> => typeof value === 'object' && value !== null && !Array.isArray(value) ? value as Record<string, unknown> : {};

type Loaded =
    | { phase: 'loading' }
    | { phase: 'failed'; reason: string }
    | { phase: 'ready'; definition: ExtensionComponentDefinition; controller: ExtensionContextController };

interface SlotHost {
    host: HTMLElement;
    name: string;
}

/**
 * The static stand-in for an extension component. It is what the page holds before the component has loaded and where it cannot run, and
 * it carries what identifies the component and nothing that needs its context. It matches the placeholder the host renders.
 * When the model names a placeholder from the package, that is shown in a frame that cannot run script.
 */
export function ExtensionComponentPlaceholder({ address, kind, placeholder, failure }: { address: ExtensionComponentAddress; kind: string; placeholder?: string; failure?: string }) {
    const name = `${address.system}.${address.component}`;
    return (
        <div data-stage-extension-component={name} data-kind={kind} role="region" aria-label={name} aria-busy={failure === undefined}>
            {placeholder && failure === undefined && <iframe title={name} src={placeholder} sandbox="" loading="lazy" style={{ border: 0, width: '100%' }} />}
            {failure !== undefined && <p role="alert">{`${name} could not be shown. ${failure}`}</p>}
        </div>
    );
}

/**
 * Places a component of an extension on a screen. It loads the extension's frontend module, checks the component is the kind the model says
 * it is, and gives it the context of Scene's extension contract: the person, the tenant, the language and time zone, the theme, the settings
 * the system declares public, the strings of the screen, and `invoke`, `raise` and `on`.
 *
 * Secrets never reach here. The settings endpoint answers with the public settings only, and `invoke` goes to the host, which holds the
 * credentials and runs the ability; the page has nothing of the extension's backend to leak.
 * @see https://github.com/Cratis/Stage/blob/main/Documentation/extensions/components.md
 */
export function StageExtensionComponent({ element, slots }: { element: ExternalComponent; slots?: Record<string, ReactNode[]> }) {
    const { locale, locales, screen: currentScreen } = useStageData();
    const services = useContext(StageExtensionServicesContext);
    const address = useMemo<ExtensionComponentAddress>(
        () => ({ screen: text(element, 'screen') || currentScreen, system: text(element, 'system'), component: text(element, 'component') }),
        [element, currentScreen]);
    const kind = (text(element, 'kind') || 'react') as ExtensionComponentKind;
    const moduleAddress = text(element, 'module');
    const placeholder = text(element, 'placeholder') || undefined;
    const properties = useMemo(() => record(element.properties.properties) as ExtensionProperties, [element.properties.properties]);
    const dictionary = useMemo(() => record(element.properties.strings) as Record<string, string>, [element.properties.strings]);
    const invokes = useMemo(() => strings(element.properties.invokes), [element.properties.invokes]);
    const signals = useMemo(() => strings(element.properties.signals), [element.properties.signals]);
    const [loaded, setLoaded] = useState<Loaded>({ phase: 'loading' });
    const [slotHosts, setSlotHosts] = useState<SlotHost[]>([]);
    const wrapper = useRef<HTMLDivElement>(null);
    const onSignal = services.onSignal;

    const raise = useCallback((signal: string, values: ExtensionValues) => {
        onSignal?.({ ...address, signal, values });
        wrapper.current?.dispatchEvent(new CustomEvent('stage-extension-signal', { bubbles: true, detail: { ...address, signal, values } }));
    }, [address, onSignal]);

    // The context is made once per placed component; what changes later (language, theme, strings) is updated in it, not rebuilt.
    const latest = useRef({ locale, locales, dictionary });
    latest.current = { locale, locales, dictionary };

    useEffect(() => {
        const abort = new AbortController();
        const fetcher = services.fetcher ?? ((input: RequestInfo | URL, init?: RequestInit) => fetch(input, init));
        setLoaded({ phase: 'loading' });
        (async () => {
            const [definition, settings, identity] = await Promise.all([
                loadExtensionDefinition({ module: moduleAddress, component: address.component, kind, importer: services.importer }),
                readExtensionSettings(address, fetcher, abort.signal),
                readExtensionUser(fetcher, abort.signal)
            ]);
            if (abort.signal.aborted) return;
            const controller = createStageExtensionContext({
                address,
                user: identity.user,
                tenant: identity.tenant ?? defaultTenant,
                locale: localeOf(latest.current.locale, latest.current.locales),
                theme: readExtensionTheme(),
                settings,
                strings: latest.current.dictionary,
                invokes,
                signals,
                onRaise: raise,
                renderSlot: (host, name) => {
                    const entry = { host, name };
                    setSlotHosts(current => [...current, entry]);
                    return () => setSlotHosts(current => current.filter(item => item !== entry));
                },
                fetcher
            });
            setLoaded({ phase: 'ready', definition, controller });
        })().catch(error => {
            if (!abort.signal.aborted) setLoaded({ phase: 'failed', reason: error instanceof Error ? error.message : String(error) });
        });
        return () => abort.abort();
        // eslint-disable-next-line react-hooks/exhaustive-deps -- the services are the page's, and what a component is loaded for is its address and module
    }, [address, moduleAddress, kind, invokes, signals, raise]);

    const controller = loaded.phase === 'ready' ? loaded.controller : undefined;
    useEffect(() => controller?.update({ locale: localeOf(locale, locales) }), [controller, locale, locales]);
    useEffect(() => controller?.update({ strings: dictionary }), [controller, dictionary]);
    useEffect(() => {
        if (!controller) return;
        const observer = new MutationObserver(() => controller.update({ theme: readExtensionTheme() }));
        observer.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme', 'data-color-scheme', 'class'] });
        return () => observer.disconnect();
    }, [controller]);

    // A screen or host that wants the component to react (a `uses` of a trigger) sends it to the component's element, and it arrives as `on(trigger)`.
    useEffect(() => {
        const element = wrapper.current;
        if (!controller || !element) return;
        const deliver = (event: Event) => {
            const detail = (event as CustomEvent<{ name?: unknown; values?: ExtensionValues }>).detail;
            if (typeof detail?.name === 'string') controller.trigger(detail.name, detail.values ?? {});
        };
        element.addEventListener('stage-extension-trigger', deliver);
        return () => element.removeEventListener('stage-extension-trigger', deliver);
    }, [controller]);

    const name = `${address.system}.${address.component}`;
    return (
        <div ref={wrapper} data-stage-extension-host={name}>
            {loaded.phase === 'ready'
                ? <ExtensionBody definition={loaded.definition} controller={loaded.controller} properties={properties} />
                : <ExtensionComponentPlaceholder address={address} kind={kind} placeholder={placeholder} failure={loaded.phase === 'failed' ? loaded.reason : undefined} />}
            {slotHosts.map((slot, index) => createPortal(slots?.[slot.name] ?? null, slot.host, `${slot.name}-${index}`))}
        </div>
    );
}

function ExtensionBody({ definition, controller, properties }: { definition: ExtensionComponentDefinition; controller: ExtensionContextController; properties: ExtensionProperties }) {
    if (definition.kind === 'react') {
        const Component = definition.component;
        return <ExtensionContextProvider context={controller.context}><Component {...properties} /></ExtensionContextProvider>;
    }
    return <MountHost definition={definition} controller={controller} properties={properties} />;
}

function MountHost({ definition, controller, properties }: { definition: Extract<ExtensionComponentDefinition, { kind: 'mount' }>; controller: ExtensionContextController; properties: ExtensionProperties }) {
    const host = useRef<HTMLDivElement>(null);
    const instance = useRef<ReturnType<typeof definition.mount>>(undefined);
    const initial = useRef(properties);

    useEffect(() => {
        if (!host.current) return;
        instance.current = definition.mount(host.current, initial.current, controller.context);
        return () => {
            instance.current?.unmount();
            instance.current = undefined;
        };
    }, [definition, controller]);
    useEffect(() => { instance.current?.update(properties); }, [properties]);

    return <div ref={host} />;
}
