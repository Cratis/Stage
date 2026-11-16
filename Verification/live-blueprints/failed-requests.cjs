// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Keep failures across navigations: a successful entry-module response does not prove its imports loaded.
function trackFailedRequests(page) {
    const failures = [];
    page.on('requestfailed', request => {
        failures.push(`REQUEST FAILED ${request.url()}: ${request.failure()?.errorText ?? 'unknown failure'}`);
    });
    page.on('response', response => {
        const status = response.status();
        if (status >= 400) failures.push(`HTTP ${status} ${response.url()}`);
    });
    return failures;
}

module.exports = { trackFailedRequests };
