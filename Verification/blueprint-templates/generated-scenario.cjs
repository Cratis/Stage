// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The blueprint-template vector against a GENERATED React application rendered from model/templates.play (run by
// generated-react-browser/run.sh with STAGE_GENERATED_REACT_SCENARIO pointing here). Environment: STAGE_GENERATED_REACT_URL, STAGE_GENERATED_REACT_RESULT.
// Exit codes: 0 every assertion passed, 1 an assertion found a defect, 2 the scenario could not run.
const { writeFileSync } = require('node:fs');
const path = require('node:path');
const { chromium } = require(path.join(__dirname, '..', 'generated-react-browser', 'node_modules', 'playwright'));
const { run } = require('./assertions.cjs');

const baseUrl = process.env.STAGE_GENERATED_REACT_URL ?? 'http://127.0.0.1:19371';
const resultPath = process.env.STAGE_GENERATED_REACT_RESULT ?? 'generated-blueprint-templates.json';

(async () => {
    const result = { name: 'generated-blueprint-templates', baseUrl, status: 'unknown', remainingBlockers: [], assertions: [] };
    const record = (name, status, value) => result.assertions.push({ path: name, status, value });
    const block = (name, reason) => { record(name, 'blocked', reason); result.remainingBlockers.push(`${name}: ${reason}`); };
    let exitCode = 2;
    let browser;
    try {
        browser = await chromium.launch();
        const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        await run({ page, baseUrl, errors, prefix: 'generated.blueprintTemplates', record, block });
        const passed = result.assertions.filter(assertion => assertion.status === 'passed').length;
        result.status = result.remainingBlockers.length > 0 ? 'failed' : 'passed';
        result.summary = `${passed} assertions passed, ${result.remainingBlockers.length} blocked`;
        exitCode = result.remainingBlockers.length > 0 ? 1 : 0;
    } catch (error) {
        result.status = 'could-not-run';
        result.message = error.stack ?? error.message;
    } finally {
        if (browser) await browser.close();
        writeFileSync(resultPath, `${JSON.stringify(result, null, 2)}\n`);
    }

    console.log(result.summary ?? result.message);
    for (const blocker of result.remainingBlockers) console.log(`BLOCKED ${blocker}`);
    process.exit(exitCode);
})();
