// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const { readFileSync } = require('node:fs');
const path = require('node:path');

// STAGE_VECTOR_EXTRA_CSS names a stylesheet to add to every page: a way to try a stylesheet fix against the vector before the package that ships it is released.
const extraCss = process.env.STAGE_VECTOR_EXTRA_CSS ? readFileSync(process.env.STAGE_VECTOR_EXTRA_CSS, 'utf8') : undefined;
const axeSource = readFileSync(require.resolve('axe-core/axe.min.js', { paths: [path.join(__dirname, '..', 'generated-react-browser')] }), 'utf8');
const schemes = ['light', 'dark'];

async function show(page, baseUrl, screen, scheme) {
    await page.goto(`${baseUrl}/#/${screen.screen}`, { waitUntil: 'domcontentloaded', timeout: 30_000 });
    await page.locator(`[data-scene-id="template-${screen.screen}-${screen.template}"]`).waitFor({ timeout: 30_000 });
    // The color scheme is the Scene configuration's: the layout wrapper carries it (the Stage's theme switcher sets the same attribute).
    await page.evaluate(value => { document.querySelectorAll('[data-scene-color-scheme]').forEach(element => element.setAttribute('data-scene-color-scheme', value)); }, scheme);
    if (extraCss) await page.addStyleTag({ content: extraCss });
    await page.waitForTimeout(400);
    await page.addScriptTag({ content: axeSource });
}

function formatViolations(violations) {
    const describe = node => {
        const data = node.any?.find(check => check.data?.contrastRatio)?.data;
        const detail = data ? ` [${data.fgColor} on ${data.bgColor}, ${data.contrastRatio}:1, needs ${data.expectedContrastRatio}]` : '';
        return `${node.target.join(' ')}${detail} ${node.html.slice(0, 120)}`;
    };
    return violations.map(violation => `${violation.id}: ${violation.nodes.map(describe).slice(0, 4).join(', ')}`);
}

async function contrast(page) {
    const violations = await page.evaluate(async () => {
        const result = await window.axe.run(document.body, { runOnly: { type: 'rule', values: ['color-contrast'] } });
        return result.violations;
    });
    return formatViolations(violations);
}

module.exports = { show, schemes, formatViolations, contrast };
