// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import { Arc, useApplicationContext } from '@cratis/arc.react';
import { SliceKind, type ApplicationStructure, type Screen } from '@cratis/scene.model';
import { describe, expect, it } from 'vitest';
import { StageApplicationContext } from '../App';
import { DialogSlice, ScreenSlice, StageStructureProvider } from '../stageStructure';
import { knownSlicesOf, locateScreen, locateSlice, readStageNotes, screenContribution } from '../stageNotes';

const structure: ApplicationStructure = {
    modules: [{
        id: 'm1', name: 'Shop', features: [{
            id: 'f1', name: 'Orders', features: [], slices: [
                { id: 's1', name: 'PlaceOrder', kind: SliceKind.StateChange, command: 'PlaceOrder' },
                { id: 's2', name: 'OrderList', kind: SliceKind.StateView },
            ],
        }],
    }],
};
const screens = [{ name: 'OrderList', path: ['Shop', 'Orders'] }, { name: 'Loose' }] as unknown as Screen[];

function Probe() {
    const context = useApplicationContext();
    return <output data-testid='context'>{JSON.stringify({ screen: context.screen, contributions: context.contributions, active: context.activeSlices })}</output>;
}

describe('when declaring where the user is', () => {
    it('contributes the screen with its module and feature ancestry and the model references to the application context', () => {
        const view = render(
            <Arc sliceMarkers>
                <StageApplicationContext scene={{ screens } as never} screen='OrderList' notes={{ enabled: true, modelReferences: { 'Shop.Orders.OrderList': 'slice-9' } }} />
                <Probe />
            </Arc>);

        const seen = JSON.parse(view.getByTestId('context').textContent!);
        expect(seen.screen).toEqual({ id: 'OrderList', name: 'OrderList', path: 'Shop/Orders' });
        expect(seen.contributions.modelReferences).toEqual({ 'Shop.Orders.OrderList': 'slice-9' });
    });

    it('wraps a screen in the slice the structure says it shows', () => {
        const view = render(
            <Arc sliceMarkers>
                <StageStructureProvider structure={structure} screens={screens}>
                    <ScreenSlice screen='OrderList'><span>screen</span></ScreenSlice>
                </StageStructureProvider>
            </Arc>);

        const marker = view.getByText('screen').parentElement!;
        expect(marker.dataset.cratisModule).toBe('Shop');
        expect(marker.dataset.cratisFeature).toBe('Orders');
        expect(marker.dataset.cratisSlice).toBe('OrderList');
    });

    it('wraps a dialog opened for a slice in that slice, found by its command', () => {
        const view = render(
            <Arc sliceMarkers>
                <StageStructureProvider structure={structure} screens={screens}>
                    <DialogSlice slice='PlaceOrder'><span>dialog</span></DialogSlice>
                </StageStructureProvider>
            </Arc>);

        const marker = view.getByText('dialog').parentElement!;
        expect(marker.dataset.cratisSlice).toBe('PlaceOrder');
        expect(marker.dataset.cratisFeature).toBe('Orders');
    });

    it('leaves a dialog for an unknown slice unmarked', () => {
        const view = render(
            <Arc sliceMarkers>
                <StageStructureProvider structure={structure} screens={screens}>
                    <DialogSlice slice='Nowhere'><span>dialog</span></DialogSlice>
                </StageStructureProvider>
            </Arc>);

        expect(view.getByText('dialog').parentElement!.dataset.cratisSlice).toBeUndefined();
    });

    it('locates slices by qualified name and by command, and lists every slice the application knows', () => {
        expect(locateSlice(structure, 'Shop.Orders.PlaceOrder')).toEqual({ module: 'Shop', feature: 'Orders', slice: 'PlaceOrder' });
        expect(locateSlice(structure, 'PlaceOrder')).toEqual({ module: 'Shop', feature: 'Orders', slice: 'PlaceOrder' });
        expect(locateSlice(structure, 'Missing')).toBeUndefined();
        expect(knownSlicesOf(structure).map(slice => slice.slice)).toEqual(['PlaceOrder', 'OrderList']);
    });

    it('places a screen with no ancestry outside any module', () => {
        expect(locateScreen(structure, screens[1])).toEqual({ module: '', feature: '', slice: 'Loose' });
        expect(screenContribution(screens[1])).toEqual({ id: 'Loose', name: 'Loose', path: '' });
    });
});

describe('when reading what the host says about feedback', () => {
    const answer = (body: object | undefined) => (async () => body ? new Response(JSON.stringify(body)) : new Response('', { status: 404 })) as unknown as typeof fetch;
    const signal = new AbortController().signal;

    it('turns Notes on and keeps the references when the host serves Notes', async () => {
        const state = await readStageNotes(signal, answer({ references: [{ kind: 'slice', name: 'Shop.PlaceOrder', id: 'x' }], notesEnabled: true }));
        expect(state).toEqual({ enabled: true, modelReferences: { 'Shop.PlaceOrder': 'x' } });
    });

    it('leaves Notes off when the host does not say it is served, or says nothing', async () => {
        expect((await readStageNotes(signal, answer({ references: [] }))).enabled).toBe(false);
        expect((await readStageNotes(signal, answer(undefined))).enabled).toBe(false);
    });
});
