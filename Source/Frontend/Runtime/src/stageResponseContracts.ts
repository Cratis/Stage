// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ResponseContract, ResponseOperand, ResponseValidationContracts, ResponseValueType } from '@cratis/scene.engine';
import { InteractionActionKind } from '@cratis/scene.model';
import type { ExecuteCommandAction, InteractionAction, NavigateAction } from '@cratis/scene.model';

/** Typed transport metadata; absence is deliberately not an untyped successful response. */
export interface StageResponseContracts {
    commands: Record<string, ResponseContract | null>;
    commandInputs: Record<string, Record<string, ResponseValueType | null>>;
    screenInputs: Record<string, Record<string, ResponseValueType | null>>;
}

/** Adapts modeled contracts to Scene's validator without resolving or evaluating a response. */
export function stageResponseContracts(metadata?: StageResponseContracts): ResponseValidationContracts {
    return {
        commandResponse: command => metadata?.commands[command] ?? undefined,
        targetType: (action, operand) => operandType(metadata, action, operand),
    };
}

function operandType(
    metadata: StageResponseContracts | undefined,
    action: InteractionAction,
    operand: ResponseOperand,
): ResponseValueType | undefined {
    if (operand.kind === 'message') return { kind: 'string' };
    if (operand.kind !== 'argument') return undefined;
    return argumentType(metadata, action, operand.name);
}

function argumentType(
    metadata: StageResponseContracts | undefined,
    action: InteractionAction,
    name: string,
): ResponseValueType | undefined {
    switch (action.kind) {
        case InteractionActionKind.ExecuteCommand:
            return metadata?.commandInputs[(action as ExecuteCommandAction).command]?.[name] ?? undefined;
        case InteractionActionKind.Navigate:
            return metadata?.screenInputs[(action as NavigateAction).screen]?.[name] ?? undefined;
        default:
            return undefined;
    }
}
