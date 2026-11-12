// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    createExtensionContext,
    webPlatform,
    type ExtensionContextController,
    type ExtensionLocale,
    type ExtensionSettings,
    type ExtensionStrings,
    type ExtensionTenant,
    type ExtensionTheme,
    type ExtensionUser,
    type ExtensionValues
} from '@cratis/scene.extensions';

/** The relative endpoints an extension component in the browser uses. Mirrors `ExtensionComponentEndpoints` in the host. */
export const extensionComponentEndpoints = {
    settings: (address: ExtensionComponentAddress) => `stage/extension-components/${path(address)}/settings`,
    invoke: (address: ExtensionComponentAddress) => `stage/extension-components/${path(address)}/invoke`
};

/** Which placed component this is: the screen that places it, the system it belongs to and its name in the system's contract. */
export interface ExtensionComponentAddress {
    screen: string;
    system: string;
    component: string;
}

/** How many times an invocation that is still running after the host's wait is asked after again before giving up. */
export const pendingInvocationAttempts = 5;

const path = (address: ExtensionComponentAddress) =>
    [address.screen, address.system, address.component].map(encodeURIComponent).join('/');

const isRecord = (value: unknown): value is Record<string, unknown> => typeof value === 'object' && value !== null && !Array.isArray(value);

/** The failure of an ability invoked from the browser: what the host said, and the status it said it with. */
export class ExtensionInvocationError extends Error {
    constructor(readonly ability: string, readonly status: number, readonly reason: string) {
        super(reason.length > 0 ? reason : `Invoking ${ability} failed (${status}).`);
        this.name = 'ExtensionInvocationError';
    }
}

/**
 * Reads the person using the screen from Arc's identity endpoint. The browser only ever learns who the person is and the roles they hold;
 * what a component may do is decided by the host when it invokes.
 */
export async function readExtensionUser(fetcher: typeof fetch, signal?: AbortSignal): Promise<{ user: ExtensionUser; tenant?: ExtensionTenant }> {
    const response = await fetcher('.cratis/me', { signal, credentials: 'same-origin', headers: { Accept: 'application/json' } }).catch(() => undefined);
    if (!response?.ok) return { user: anonymous };
    const body = await response.json().catch(() => undefined) as unknown;
    if (!isRecord(body)) return { user: anonymous };
    const name = typeof body.name === 'string' ? body.name : typeof body.userName === 'string' ? body.userName : '';
    const id = typeof body.id === 'string' ? body.id : typeof body.userId === 'string' ? body.userId : name;
    const roles = Array.isArray(body.roles) ? body.roles.filter((role): role is string => typeof role === 'string') : [];
    const tenant = typeof body.tenantId === 'string' && body.tenantId.length > 0 ? { id: body.tenantId } : undefined;
    return { user: { id, name, roles, signedIn: id.length > 0 }, tenant };
}

const anonymous: ExtensionUser = { id: '', name: '', roles: [], signedIn: false };

/** The tenant a Stage application runs for when the identity does not say another. */
export const defaultTenant: ExtensionTenant = { id: 'default' };

/**
 * Reads the public settings of the system a component belongs to. The host only ever sends the settings declared public;
 * anything else it holds for the system never leaves it, so there is nothing here to filter and nothing to ask for.
 */
export async function readExtensionSettings(address: ExtensionComponentAddress, fetcher: typeof fetch, signal?: AbortSignal): Promise<ExtensionSettings> {
    const response = await fetcher(extensionComponentEndpoints.settings(address), { signal, credentials: 'same-origin', headers: { Accept: 'application/json' } });
    if (!response.ok) throw new Error(`The settings of ${address.system}.${address.component} could not be read (${response.status}).`);
    const body = await response.json() as unknown;
    return isRecord(body) && isRecord(body.settings) ? Object.freeze({ ...body.settings }) : Object.freeze({});
}

/** What the person's browser says about their language and time zone. */
export function localeOf(language: string, availableLanguages: readonly string[]): ExtensionLocale {
    return {
        language,
        availableLanguages: availableLanguages.length > 0 ? [...availableLanguages] : [language],
        timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone
    };
}

/** The colors a component draws with: whether the page is light or dark, and the design tokens in force on the element. */
export function readExtensionTheme(element: Element = document.documentElement): ExtensionTheme {
    const scheme = element.getAttribute('data-theme') ?? element.getAttribute('data-color-scheme') ?? '';
    const dark = scheme.length > 0
        ? scheme.toLowerCase().includes('dark')
        : typeof matchMedia === 'function' && matchMedia('(prefers-color-scheme: dark)').matches;
    const tokens: Record<string, string> = {};
    const computed = getComputedStyle(element);
    for (const sheet of Array.from(document.styleSheets)) {
        let rules: CSSRuleList;
        try {
            rules = sheet.cssRules;
        } catch {
            continue; // A sheet from another origin cannot be read; its tokens are not part of the contract.
        }
        for (const rule of Array.from(rules)) {
            if (!(rule instanceof CSSStyleRule)) continue;
            for (const name of Array.from(rule.style)) {
                if (name.startsWith('--cratis-') && !(name in tokens)) tokens[name] = computed.getPropertyValue(name).trim();
            }
        }
    }
    return { colorScheme: dark ? 'dark' : 'light', tokens };
}

/** What a component's context is made of that the page, not the host, knows. */
export interface StageExtensionContextOptions {
    address: ExtensionComponentAddress;
    user: ExtensionUser;
    tenant: ExtensionTenant;
    locale: ExtensionLocale;
    theme: ExtensionTheme;
    settings: ExtensionSettings;
    strings: ExtensionStrings;

    /** The abilities the contract lets the component invoke. Anything else is refused here as well as by the host. */
    invokes: readonly string[];

    /** The signals the contract lets the component raise. */
    signals: readonly string[];

    /** Receives a signal the component raises, so the screen's `on <signal>` interactions can handle it. */
    onRaise: (signal: string, values: ExtensionValues) => void;

    /** Draws what the screen placed in one of the component's slots into a host element the component owns. */
    renderSlot?: (host: HTMLElement, slotName: string) => () => void;
    fetcher?: typeof fetch;
    newInteraction?: () => string;
}

const asValues = (text: string): ExtensionValues => {
    if (text.length === 0) return {};
    try {
        const parsed = JSON.parse(text) as unknown;
        return isRecord(parsed) ? parsed : { value: parsed };
    } catch {
        return { value: text };
    }
};

/**
 * Builds the context a component receives from Scene's contract. `invoke` goes through the host and nothing else: the browser is given
 * no credentials for an extension's backend, so a call is checked against the contract's allowlist and the person's authorization on the
 * host, whatever the component says.
 */
export function createStageExtensionContext(options: StageExtensionContextOptions): ExtensionContextController {
    const fetcher = options.fetcher ?? fetch;
    const newInteraction = options.newInteraction ?? (() => crypto.randomUUID());
    return createExtensionContext({
        user: options.user,
        tenant: options.tenant,
        locale: options.locale,
        theme: options.theme,
        settings: options.settings,
        strings: options.strings,
        renderSlot: options.renderSlot,
        invoke: async (ability, inputs) => {
            if (!options.invokes.includes(ability)) {
                throw new ExtensionInvocationError(ability, 403, `${options.address.system}.${options.address.component} does not invoke ${ability}.`);
            }
            // One identity for the interaction, kept across the asks below, so asking again after a wait never invokes twice.
            const interaction = newInteraction();
            const body = JSON.stringify({ ability, interaction, input: JSON.stringify(inputs ?? {}) });
            for (let attempt = 0; attempt < pendingInvocationAttempts; attempt++) {
                const response = await fetcher(extensionComponentEndpoints.invoke(options.address), {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
                    body
                });
                const result = await response.json().catch(() => ({})) as { status?: string; outcome?: string; output?: string; reason?: string };
                if (response.status === 202 || result.status === 'Pending') continue;
                if (!response.ok) throw new ExtensionInvocationError(ability, response.status, result.reason ?? '');
                if (result.outcome === 'failed' || result.outcome === 'stopped') throw new ExtensionInvocationError(ability, response.status, result.reason ?? '');
                return asValues(result.output ?? '');
            }
            throw new ExtensionInvocationError(ability, 202, `${ability} is still running; it was not abandoned and its outcome will settle on the host.`);
        },
        raise: (signal, values) => {
            if (!options.signals.includes(signal)) {
                throw new Error(`${options.address.system}.${options.address.component} does not raise ${signal}.`);
            }
            options.onRaise(signal, values);
        }
    });
}

