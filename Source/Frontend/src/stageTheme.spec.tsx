// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import { PrimeReactProvider } from '@primereact/core';
import { Button } from 'primereact/button';
import { describe, expect, it } from 'vitest';
import { stageDarkClass, stageTheme } from './stageTheme';

describe('the Stage theme', () => {
    it('makes PrimeReact inject the design tokens its components are painted with', () => {
        render(
            <PrimeReactProvider theme={stageTheme}>
                <Button>Execute</Button>
            </PrimeReactProvider>,
        );

        const injected = [...document.head.querySelectorAll('style')].map(style => style.textContent ?? '').join('\n');
        expect(injected).toContain('--p-primary-color');
    });

    it('keys its dark scheme off the class Stage puts on the document root', () => {
        expect(stageTheme.options.darkModeSelector).toBe(`.${stageDarkClass}`);
    });
});
