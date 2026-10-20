// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fileURLToPath } from 'node:url';
import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import { styledPrimeReact } from './styledPrimeReact';

export default defineConfig({
    plugins: [styledPrimeReact(), react()],
    test: {
        alias: [{ find: /^@cratis\/stage\.runtime$/, replacement: fileURLToPath(new URL('./src/index.ts', import.meta.url)) }],
        environment: 'jsdom',
        setupFiles: ['./src/testSetup.ts'],
        testTimeout: 20000,
    },
});
