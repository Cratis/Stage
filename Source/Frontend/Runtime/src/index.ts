// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export { App, App as StageApplication, composeStageScreen, stageApplicationRegistry, stageComposition, stageShellOf } from './App';
export type { StageSceneApplication } from './App';
export { StageSourceProvider, staticStageSource, useStageSource, stageEndpoints } from './stageSource';
export type { StageSource, StageStaticContent } from './stageSource';
export type { StageRoutes } from './stageRoutes';
export { stageTheme, stageDarkSelector } from './stageTheme';
export { executeRegisteredCommand, registeredCommand, useStageCommandExecutor } from './commandExecutor';
export { registeredQuery, performQuery, subscribeQuery, registerQueryRouteResolver } from './queryExecutor';
export { StageCommandForm } from './StageCommandForm';
export { StageGuardedAction } from './StageGuardedAction';
export { StageDataProvider, useStageData, useStageQuery, dataChanged } from './stageData';
export { createStageSliceInvoker, sliceFieldsFrom } from './stageSlices';
