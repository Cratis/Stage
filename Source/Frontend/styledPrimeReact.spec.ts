// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { exportedNames } from './styledPrimeReact';

describe('reading what a styled PrimeReact module exports', () => {
    it('takes the exported name of every specifier, renamed or not', () => {
        const source = 'var i=1;export{i as Button,y as ButtonProps,C as ButtonProvider,v as defaultProps,I as useButtonContext};';
        expect(exportedNames(source)).toEqual(['Button', 'ButtonProps', 'ButtonProvider', 'defaultProps', 'useButtonContext']);
    });

    it('reads every export statement once', () => {
        const source = 'export{a as Dialog};export{Dialog,b as DialogProps};';
        expect(exportedNames(source)).toEqual(['Dialog', 'DialogProps']);
    });
});
