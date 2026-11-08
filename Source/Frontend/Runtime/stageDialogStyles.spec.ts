// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { describe, expect, it } from 'vitest';

const require = createRequire(import.meta.url);

describe('the runtime dialog stylesheet', () => {
    it('is imported by the runtime entry', () => {
        expect(readFileSync('src/App.tsx', 'utf8')).toContain("import '@cratis/components/Dialogs/styles';");
    });

    it('ships the rules that position a command dialog over a backdrop', () => {
        const stylesheet = readFileSync(require.resolve('@cratis/components/Dialogs/styles'), 'utf8');
        expect(stylesheet).toContain('.cratis-dialog__backdrop');
        expect(stylesheet).toContain('.cratis-dialog__positioner');
    });
});
