// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
    base: './',
    envPrefix: 'STAGE_',
    // CI sets STAGE_PRIMEUI_LICENSE (see the Dockerfile's frontend stage and publish.yml), remapped from
    // the PRIMEUI_LICENSE GitHub secret the same way Direct and Studio do it - but a local shell profile
    // naturally exports it under that bare name, not the STAGE_ prefix `envPrefix` otherwise requires.
    // Fall back to it so a local `npm run build`/`npm run dev` picks up a plain PRIMEUI_LICENSE without
    // every machine needing a second, prefixed copy.
    define: {
        'import.meta.env.STAGE_PRIMEUI_LICENSE': JSON.stringify(process.env.STAGE_PRIMEUI_LICENSE || process.env.PRIMEUI_LICENSE || ''),
    },
    plugins: [react()],
    test: {
        environment: 'jsdom',
        setupFiles: ['./src/testSetup.ts'],
    },
});
