// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { LogoKind, type Screen, type UiProfile } from '@cratis/scene.model';
import { applicationChrome } from '../blueprint';
import { applicationIdentity, type StageSceneApplication } from '../App';

const sceneWith = (profile: Partial<UiProfile>, navigation?: StageSceneApplication['navigation']) =>
    ({ uiProfiles: [{ name: 'Web', targetPlatform: 'web', packages: [], ...profile }], screens: [], navigation }) as unknown as StageSceneApplication;
const screens = [{ name: 'Landing' }, { name: 'Other' }] as Screen[];
const logoOf = (identity: ReturnType<typeof applicationIdentity>, slot: string) => {
    const chrome = applicationChrome(screens, 'Landing', [], '', identity);
    return (chrome[slot][0] as unknown as { slots: { logo: { properties: Record<string, unknown> }[] } }).slots.logo[0].properties;
};

describe('when the profile names the application', () => {
    const identity = applicationIdentity(sceneWith({ identity: { name: 'DevConf', logo: { kind: LogoKind.Icon, icon: 'layers' }, showName: true } }));

    it('takes the title from the identity of the profile', () => expect(identity?.title).toBe('DevConf'));
    it('draws the icon of its logo', () => expect(identity?.icon).toBe('layers'));
    it('shows the name in the topbar', () => expect(logoOf(identity, 'topbar').label).toBe('DevConf'));
    it('shows the name in the sidebar', () => expect(logoOf(identity, 'sidebar').label).toBe('DevConf'));
    it('opens the landing page from the mark in the topbar', () => expect(logoOf(identity, 'topbar').targetScreen).toBe('Landing'));
    it('opens the landing page from the mark in the sidebar', () => expect(logoOf(identity, 'sidebar').targetScreen).toBe('Landing'));
});

describe('when the navigation declares a logo', () => {
    const identity = applicationIdentity(sceneWith({ identity: { name: 'Profile' } }, { application: { regions: [], logo: { title: 'Navigation' } }, modules: [] } as never));

    it('prefers the logo of the navigation', () => expect(identity?.title).toBe('Navigation'));
});

describe('when nothing names the application', () => {
    it('leaves the Stage to call itself what it always has', () => expect(applicationIdentity(sceneWith({}))).toBeUndefined());
    it('falls back to the Stage name', () => expect(logoOf(undefined, 'topbar').label).toBe('Cratis Stage'));
});
