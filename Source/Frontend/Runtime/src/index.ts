// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export { App, App as StageApplication, composeStageScreen, stageApplicationRegistry, stageComposition, stageShellOf } from './App';
export type { StageSceneApplication } from './App';
// TODO(Cratis/Stage#707, phase 2): stageTheme stays exported here while the generated PrimeReact application imports it from the package root;
// once the Scene release with Cratis/Scene#250 is consumed, import it from '@cratis/stage.runtime/primereact' and drop this export, so the root has no PrimeReact import.
export { stageTheme, stageDarkSelector } from './stageTheme';
export { StageSourceProvider, staticStageSource, useStageSource, stageEndpoints } from './stageSource';
export type { StageSource, StageStaticContent } from './stageSource';
export type { StageRoutes } from './stageRoutes';
export { executeRegisteredCommand, registeredCommand, useStageCommandExecutor } from './commandExecutor';
export { hostedBasePath } from './hostedBasePath';
export { registeredQuery, performQuery, subscribeQuery, registerQueryRouteResolver } from './queryExecutor';
export { StageCommandForm } from './StageCommandForm';
export { StageGuardedAction } from './StageGuardedAction';
export { StageDataProvider, useStageData, useStageQuery, dataChanged } from './stageData';
export { createStageSliceInvoker, sliceFieldsFrom } from './stageSlices';
