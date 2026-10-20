// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useId } from 'react';
import { useStageData } from './stageData';

/**
 * The `onComponentOutputsChanged` of one rendered tree - the screen, or a dialog above it.
 *
 * Scene keeps the outputs of each tree it renders apart and hands over all of them on every change, so a tree publishes under a scope of its own:
 * a dialog that opens publishes beside the screen instead of over it, and when it closes only what it published goes - the row the screen had
 * selected still resolves.
 *
 * @returns The callback to hand the tree's `SceneElementView`.
 */
export function useComponentOutputsPublisher(): (outputs: Record<string, Record<string, unknown>>) => void {
    const scope = useId();
    const { publishComponentOutputs, withdrawComponentOutputs } = useStageData();
    useEffect(() => () => withdrawComponentOutputs(scope), [scope, withdrawComponentOutputs]);
    return useCallback((outputs: Record<string, Record<string, unknown>>) => publishComponentOutputs(scope, outputs), [publishComponentOutputs, scope]);
}
