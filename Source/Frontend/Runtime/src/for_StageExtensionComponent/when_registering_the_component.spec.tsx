// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { StageExtensionComponent, stageExtensionComponent } from '../StageExtensionComponent';
import { stageComponents } from '../stageComponents';

describe('when registering the extension component', () => {
    it('renders scene elements named Stage:extensionComponent with it', () => {
        expect(stageExtensionComponent).toBe('Stage:extensionComponent');
        expect(stageComponents[stageExtensionComponent]).toBe(StageExtensionComponent);
    });
});
