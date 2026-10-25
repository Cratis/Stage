// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import type { Screen } from '@cratis/scene.model';
import { screenTitle } from './screenTitle';

const screenWith = (name: string, content: unknown[]) => ({ name, slotContent: { content } }) as unknown as Screen;
const title = (text: string) => ({ id: 't', name: 't', componentName: 'core:title', properties: { text }, slots: {} });

describe('naming a screen where the application lists it', () => {
    it('should use the text of its title', () => {
        expect(screenTitle(screenWith('PipelineScreen', [title('Sales pipeline')]))).toEqual('Sales pipeline');
    });

    it('should humanize the screen name when it has no title', () => {
        expect(screenTitle(screenWith('OpportunityList', []))).toEqual('Opportunity list');
    });
});

describe('naming a screen: only a title at the top of its body counts', () => {
    it('should not take a title nested in a section', () => {
        const section = { id: 's', name: 's', componentName: 'core:section', properties: {}, slots: { content: [title('Register invoice')] } };
        expect(screenTitle(screenWith('InvoiceList', [section]))).toEqual('Invoice list');
    });

    it('should prefer the content slot over another slot, whatever the order they are declared in', () => {
        const screen = { name: 'Any', slotContent: { header: [title('Header')], content: [title('Body')] } } as unknown as Screen;
        expect(screenTitle(screen)).toEqual('Body');
    });

    it('should drop a trailing Screen from the humanized name', () => {
        expect(screenTitle(screenWith('PipelineScreen', []))).toEqual('Pipeline');
    });
});
