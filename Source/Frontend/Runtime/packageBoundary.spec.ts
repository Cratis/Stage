// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { execFileSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import runtime from './package.json';
import host from '../package.json';

describe('the runtime package boundary', () => {
    it('uses the host pins as peers instead of installing private runtime dependencies', () => {
        const pins: Record<string, string> = { ...host.dependencies, ...host.devDependencies };
        expect(Object.keys(runtime.peerDependencies).length).toBeGreaterThan(0);
        for (const [name, version] of Object.entries(runtime.peerDependencies)) {
            expect(version, name).toBe(pins[name]);
        }
        expect(runtime).not.toHaveProperty('dependencies');
    });

    it('has one installed instance of React, Arc and each Scene registry peer throughout the workspace tree', () => {
        const names = Object.keys(runtime.peerDependencies).filter(name => name.startsWith('@cratis/') || name === 'react' || name === 'react-dom');
        const paths = execFileSync('npm', ['ls', ...names, '--all', '--parseable'], { cwd: '..', encoding: 'utf8' }).trim().split('\n');
        const counts = new Map<string, number>();
        for (const path of paths) {
            const manifest: { name: string } = JSON.parse(readFileSync(join(path, 'package.json'), 'utf8'));
            counts.set(manifest.name, (counts.get(manifest.name) ?? 0) + 1);
        }
        for (const name of names) expect(counts.get(name), name).toBe(1);
    });
});
