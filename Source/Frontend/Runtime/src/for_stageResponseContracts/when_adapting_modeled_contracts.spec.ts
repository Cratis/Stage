// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'chai/register-should';
import { describe, it } from 'vitest';
import { InteractionActionKind } from '@cratis/scene.model';
import type { ExecuteCommandAction, NavigateAction } from '@cratis/scene.model';
import { stageResponseContracts } from '../stageResponseContracts';

const contracts = stageResponseContracts({
    commands: { Register: { kind: 'value', type: { kind: 'string' } }, Rename: { kind: 'void' }, Unsupported: null },
    commandInputs: { Rename: { projectId: { kind: 'string' }, unsupported: null } },
    screenInputs: { Details: { projectId: { kind: 'string' }, unsupported: null } },
});

describe('when adapting modeled response contracts', () => {
    it('should carry the typed response', () => contracts.commandResponse('Register')!.should.deep.equal({ kind: 'value', type: { kind: 'string' } }));
    it('should preserve a void response', () => contracts.commandResponse('Rename')!.should.deep.equal({ kind: 'void' }));
    it('should not invent a missing command response type', () => (contracts.commandResponse('Missing') === undefined).should.be.true);
    it('should not invent an unsupported command response type', () => (contracts.commandResponse('Unsupported') === undefined).should.be.true);
    it('should not invent contracts when metadata is absent', () => (stageResponseContracts().commandResponse('Register') === undefined).should.be.true);

    const command: ExecuteCommandAction = { kind: InteractionActionKind.ExecuteCommand, command: 'Rename' };
    it('should use the modeled destination command input', () => contracts.targetType(command, { kind: 'argument', name: 'projectId' })!.should.deep.equal({ kind: 'string' }));
    it('should not invent a command argument type', () => (contracts.targetType(command, { kind: 'argument', name: 'missing' }) === undefined).should.be.true);

    const navigate: NavigateAction = { kind: InteractionActionKind.Navigate, screen: 'Details' };
    it('should use the typed destination query input', () => contracts.targetType(navigate, { kind: 'argument', name: 'projectId' })!.should.deep.equal({ kind: 'string' }));
    it('should not invent a screen input type', () => (contracts.targetType(navigate, { kind: 'argument', name: 'missing' }) === undefined).should.be.true);

    it('should keep null command input metadata unavailable', () => (contracts.targetType(command, { kind: 'argument', name: 'unsupported' }) === undefined).should.be.true);
    it('should keep null screen input metadata unavailable', () => (contracts.targetType(navigate, { kind: 'argument', name: 'unsupported' }) === undefined).should.be.true);
    it('should not invent command inputs without metadata', () => (stageResponseContracts().targetType(command, { kind: 'argument', name: 'projectId' }) === undefined).should.be.true);
    it('should not invent screen inputs without metadata', () => (stageResponseContracts().targetType(navigate, { kind: 'argument', name: 'projectId' }) === undefined).should.be.true);
    it('should ignore argument targets on unrelated actions', () => (contracts.targetType({ kind: InteractionActionKind.Notify }, { kind: 'argument', name: 'projectId' }) === undefined).should.be.true);
    it('should retain message typing without metadata', () => stageResponseContracts().targetType({ kind: InteractionActionKind.Notify }, { kind: 'message' })!.should.deep.equal({ kind: 'string' }));

    it('should use the Scene message type', () => contracts.targetType({ kind: InteractionActionKind.Notify }, { kind: 'message' })!.should.deep.equal({ kind: 'string' }));
    it('should not invent a state target type', () => (contracts.targetType({ kind: InteractionActionKind.SetState }, { kind: 'stateValue' }) === undefined).should.be.true);
});
