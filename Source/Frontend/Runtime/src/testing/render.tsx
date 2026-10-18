// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render as renderElement } from '@testing-library/react';
import { StageSourceProvider } from '../stageSource';
import { httpStageSource } from '../../../src/httpStageSource';

export * from '@testing-library/react';

/** Supplies the live host's transport to runtime specifications; an explicit inner provider still wins. */
export function render(element: Parameters<typeof renderElement>[0], options?: Parameters<typeof renderElement>[1]) {
    return renderElement(<StageSourceProvider source={httpStageSource}>{element}</StageSourceProvider>, options);
}
