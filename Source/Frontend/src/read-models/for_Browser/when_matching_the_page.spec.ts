// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { browserEndpoint, isBrowserPath } from '../path';
import { endpoints } from '../endpoints';

describe('when matching the read-model browser page', () => {
    it('should match standalone and proxied page paths', () => {
        expect(isBrowserPath('/read-models')).toBe(true);
        expect(isBrowserPath('/api/play/session/read-models/')).toBe(true);
    });
    it('should leave normal scene paths alone', () => {
        expect(isBrowserPath('/')).toBe(false);
        expect(isBrowserPath('/api/play/session/')).toBe(false);
        expect(isBrowserPath('/read-models/another')).toBe(false);
        expect(isBrowserPath('/some-read-models')).toBe(false);
    });
    it('should keep endpoints relative to the session base', () => {
        const page = 'https://studio.example/api/play/session/read-models';
        expect(new URL(browserEndpoint(endpoints.readModels, '/api/play/session/read-models'), page).pathname).toBe('/api/play/session/stage/read-models');
        expect(new URL(browserEndpoint(endpoints.readModels, '/api/play/session/read-models/'), `${page}/`).pathname).toBe('/api/play/session/stage/read-models');
    });
    it('should keep encoded detail endpoints relative to the session base', () => {
        const page = 'https://studio.example/api/play/session/read-models';
        expect(new URL(browserEndpoint(endpoints.readModel('sm:model'), '/api/play/session/read-models'), page).pathname).toBe('/api/play/session/stage/read-models/sm%3Amodel');
        expect(new URL(browserEndpoint(endpoints.readModel('sm:model'), '/api/play/session/read-models/'), `${page}/`).pathname).toBe('/api/play/session/stage/read-models/sm%3Amodel');
    });
});
