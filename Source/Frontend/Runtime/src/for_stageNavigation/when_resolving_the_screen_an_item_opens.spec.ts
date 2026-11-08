// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { ScreenFit, type Screen } from '@cratis/scene.model';
import type { EffectiveNavItem } from '@cratis/scene.engine';
import { stageScreenFor } from '../stageNavigationModel';

const screen = (name: string, path: string[], fits?: ScreenFit[]) => ({ name, path, ...(fits ? { fits } : {}) }) as unknown as Screen;
const itemFor = (resolvedFeature: string) => ({ target: { resolvedFeature } }) as unknown as EffectiveNavItem;

describe('when a module\'s first screen fits only a dialog', () => {
    const screenFor = stageScreenFor([
        screen('PlaceOrder', ['Sales', 'Orders'], [ScreenFit.Dialog]),
        screen('List', ['Sales', 'Orders']),
        screen('CustomerScreen', ['Sales', 'Customers']),
    ]);

    it('opens the first page beneath the module', () => expect(screenFor(itemFor('Sales'))).toBe('List'));
    it('opens the page of the feature', () => expect(screenFor(itemFor('Sales.Orders'))).toBe('List'));
});

describe('when a screen lists page among what it fits', () => {
    const screenFor = stageScreenFor([screen('Details', ['Sales', 'Orders'], [ScreenFit.Pane, ScreenFit.Page])]);

    it('opens it', () => expect(screenFor(itemFor('Sales.Orders'))).toBe('Details'));
});

describe('when a feature has only a drawer', () => {
    const screenFor = stageScreenFor([screen('Edit', ['Sales', 'Orders'], [ScreenFit.Drawer])]);

    it('opens nothing', () => expect(screenFor(itemFor('Sales.Orders'))).toBeUndefined());
});
