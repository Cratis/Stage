// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { isAbsolute } from 'node:path';
import { fileURLToPath } from 'node:url';
import { defineConfig } from 'vite';

export default defineConfig({
    plugins: [{
        name: 'stage-runtime-styles',
        renderChunk(code, chunk) {
            // Library mode extracts CSS; keep the entry's existing automatic stylesheet loading.
            return chunk.isEntry && chunk.name === 'src/index' ? `import './app.css';\n${code}` : null;
        },
    }],
    build: {
        lib: {
            entry: {
                'src/index': fileURLToPath(new URL('./src/index.ts', import.meta.url)),
                styledPrimeReact: fileURLToPath(new URL('./styledPrimeReact.ts', import.meta.url)),
            },
            formats: ['es'],
            fileName: (_format, entryName) => `${entryName}.js`,
            cssFileName: 'src/app',
        },
        rolldownOptions: {
            // Preserve every peer (including subpaths) and Node builtin as a host-owned import.
            external: source => !source.startsWith('.') && !isAbsolute(source),
        },
    },
});
