// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, existsSync, rmSync } from 'node:fs';
import { resolve, join } from 'node:path';

mkdirSync(resolve('.ai-work/s2-s3'), { recursive: true });
const root = mkdtempSync(resolve('.ai-work/s2-s3/published-entrypoint-'));
const entrypoint = resolve('Source/Host/entrypoint-published.sh');
const secret = 'mongodb://synthetic-storage:27017/published';
const kernelSecret = 'chronicle://synthetic-client:synthetic-secret@chronicle.internal:35000';
const executable = (path, contents) => writeFileSync(path, contents, { mode: 0o700 });
let count = 0;
try {
    for (const name of ['app', 'stage', 'bin', 'package/model']) mkdirSync(join(root, name), { recursive: true });
    writeFileSync(join(root, 'package/manifest.json'), '{}');
    writeFileSync(join(root, 'storage'), secret + '\n');
    writeFileSync(join(root, 'kernel'), kernelSecret + '\n');
    writeFileSync(join(root, 'kernel-without-auth'), `${kernelSecret}?auth=none\n`);
    writeFileSync(join(root, 'kernel-empty'), ' \n');
    writeFileSync(join(root, 'bootstrap.sh'), `cd() { case "$1" in /app) builtin cd "$FIXTURE/app";; /stage) builtin cd "$FIXTURE/stage";; *) builtin cd "$@";; esac; }\n`);
    executable(join(root, 'app/Cratis.Chronicle.Server'), '#!/bin/sh\nenv > "$FIXTURE/kernel-env"\n');
    executable(join(root, 'bin/nc'), '#!/bin/sh\ntest -f "$FIXTURE/kernel-env"\n');
    executable(join(root, 'bin/dotnet'), '#!/bin/sh\nprintf "%s\\n" "$@" > "$FIXTURE/host-args"\n');
    const environment = Object.fromEntries(Object.entries(process.env).filter(([key]) => !key.startsWith('Stage__Published__') && !key.startsWith('Cratis__Chronicle__') && key !== 'CRATIS_CHRONICLE_CONNECTION_STRING_FILE'));
    const started = () => ({ kernel: existsSync(join(root, 'kernel-env')), host: existsSync(join(root, 'host-args')) });
    const run = (settings, expected) => {
        rmSync(join(root, 'kernel-env'), { force: true });
        rmSync(join(root, 'host-args'), { force: true });
        const result = spawnSync('/bin/bash', [entrypoint], {
            env: { ...environment, PATH: `${root}/bin:${process.env.PATH}`, FIXTURE: root, BASH_ENV: join(root, 'bootstrap.sh'), Stage__Published__PackagePath: join(root, 'package'), ...settings },
            encoding: 'utf8', timeout: 10000
        });
        assert.ifError(result.error);
        assert.equal(result.status, expected, result.stderr);
        assert.ok(!`${result.stdout}${result.stderr}`.includes(secret), 'storage secret leaked');
        assert.ok(!`${result.stdout}${result.stderr}`.includes('synthetic-secret'), 'kernel secret leaked');
        count++;
        return result;
    };
    const kernelEnvironment = () => readFileSync(join(root, 'kernel-env'), 'utf8').split('\n');
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

    // The storage engine: the kernel is started with the type in its documented spelling, whatever case was configured.
    for (const [configured, kernelType] of [['PostgreSql', 'PostgreSql'], ['MsSql', 'MsSql'], ['postgresql', 'PostgreSql'], ['mssql', 'MsSql'], ['MongoDB', 'MongoDB']]) {
        run({ Stage__Published__Storage__Type: configured, Stage__Published__Storage__ConnectionStringFile: join(root, 'storage') }, 0);
        const kernel = kernelEnvironment();
        assert.ok(kernel.includes(`Cratis__Chronicle__Storage__Type=${kernelType}`), `${configured} starts the kernel on ${kernelType}`);
        assert.ok(kernel.includes(`Cratis__Chronicle__Storage__ConnectionDetails=${secret}`), `${configured} passes the connection`);
        assert.ok(kernel.includes(`Stage__Published__Storage__Type=${kernelType}`), `${configured} tells the host the type`);
        assert.ok(started().host, `${configured} starts the host`);
    }

    // An unknown storage type is refused before anything starts, naming the accepted ones.
    const unknown = run({ Stage__Published__Storage__Type: 'Oracle', Stage__Published__Storage__ConnectionString: secret }, 78);
    assert.ok(unknown.stderr.includes('MongoDB, PostgreSql, MsSql'), 'the refusal lists the accepted types');
    assert.deepEqual(started(), { kernel: false, host: false }, 'an unknown storage type started a process');

    // An external kernel: no kernel process, no storage required, the host started directly.
    const external = run({ CRATIS_CHRONICLE_CONNECTION_STRING_FILE: join(root, 'kernel') }, 0);
    assert.deepEqual(started(), { kernel: false, host: true }, 'an external kernel still started the bundled one');
    assert.equal(readFileSync(join(root, 'host-args'), 'utf8'), 'Cratis.Stage.Host.dll\n--published\n');
    assert.ok(external.stdout.includes('external Chronicle kernel'), 'says it uses the external kernel');
    run({ CRATIS_CHRONICLE_CONNECTION_STRING_FILE: join(root, 'kernel'), Stage__Published__Storage__Type: 'PostgreSql' }, 0);
    assert.deepEqual(started(), { kernel: false, host: true }, 'a storage type does not bring the bundled kernel back');

    // A mounted connection that disables authentication, or that is empty or unreadable, is refused.
    for (const file of ['kernel-without-auth', 'kernel-empty', 'missing-kernel']) {
        run({ CRATIS_CHRONICLE_CONNECTION_STRING_FILE: join(root, file), Stage__Published__Storage__ConnectionString: secret }, 78);
        assert.deepEqual(started(), { kernel: false, host: false }, `${file} started a process`);
    }

    console.log(`Published entrypoint: ${count} cases passed (fake processes; no container).`);
} finally {
    rmSync(root, { recursive: true });
}
