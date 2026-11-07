// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, existsSync, rmSync } from 'node:fs';
import { resolve, join } from 'node:path';

const root = mkdtempSync(resolve('.ai-work/s2-s3/published-entrypoint-'));
const entrypoint = resolve('Source/Host/entrypoint-published.sh');
const secret = 'mongodb://synthetic-storage:27017/published';
const executable = (path, contents) => writeFileSync(path, contents, { mode: 0o700 });
let count = 0;
try {
    for (const name of ['app', 'stage', 'bin', 'package/model']) mkdirSync(join(root, name), { recursive: true });
    writeFileSync(join(root, 'package/manifest.json'), '{}');
    writeFileSync(join(root, 'storage'), secret + '\n');
    writeFileSync(join(root, 'bootstrap.sh'), `cd() { case "$1" in /app) builtin cd "$FIXTURE/app";; /stage) builtin cd "$FIXTURE/stage";; *) builtin cd "$@";; esac; }\n`);
    executable(join(root, 'app/Cratis.Chronicle.Server'), '#!/bin/sh\nenv > "$FIXTURE/kernel-env"\n');
    executable(join(root, 'bin/nc'), '#!/bin/sh\ntest -f "$FIXTURE/kernel-env"\n');
    executable(join(root, 'bin/dotnet'), '#!/bin/sh\nprintf "%s\\n" "$@" > "$FIXTURE/host-args"\n');
    const environment = Object.fromEntries(Object.entries(process.env).filter(([key]) => !key.startsWith('Stage__Published__') && !key.startsWith('Cratis__Chronicle__')));
    const run = (settings, expected) => {
        const result = spawnSync('/bin/bash', [entrypoint], {
            env: { ...environment, PATH: `${root}/bin:${process.env.PATH}`, FIXTURE: root, BASH_ENV: join(root, 'bootstrap.sh'), Stage__Published__PackagePath: join(root, 'package'), ...settings },
            encoding: 'utf8', timeout: 10000
        });
        assert.ifError(result.error);
        assert.equal(result.status, expected, result.stderr);
        assert.ok(!`${result.stdout}${result.stderr}`.includes(secret), 'storage secret leaked');
        count++;
    };
    run({}, 78);
    run({ Stage__Published__Storage__ConnectionStringFile: join(root, 'missing-secret') }, 78);
    run({ Stage__Published__Storage__ConnectionString: secret, Stage__Published__PackagePath: join(root, 'missing-package') }, 78);
    assert.ok(!existsSync(join(root, 'kernel-env')) && !existsSync(join(root, 'host-args')), 'refusal started a process');
    for (const settings of [{ Stage__Published__Storage__ConnectionString: secret }, { Stage__Published__Storage__ConnectionStringFile: join(root, 'storage') }]) {
        run(settings, 0);
        const kernel = readFileSync(join(root, 'kernel-env'), 'utf8');
        for (const value of ['Storage__Type=MongoDB', `Storage__ConnectionDetails=${secret}`, 'Features__Api=false', 'Features__Workbench=false', 'Authentication__Enabled=true']) {
            assert.ok(kernel.split('\n').includes(`Cratis__Chronicle__${value}`), value);
        }
        assert.equal(readFileSync(join(root, 'host-args'), 'utf8'), 'Cratis.Stage.Host.dll\n--published\n');
    }
    console.log(`Published entrypoint: ${count} cases passed (fake processes; no container).`);
} finally {
    rmSync(root, { recursive: true });
}
