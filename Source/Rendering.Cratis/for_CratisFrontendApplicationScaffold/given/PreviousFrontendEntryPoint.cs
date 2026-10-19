// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_CratisFrontendApplicationScaffold.given;

// Historical frozen plans used an unconditional Stage import and copied the runtime. Reverse only the entry point imports before comparing their
// original digests, so backend and Scene bytes are still checked against the unchanged historical expectations.
internal static class PreviousFrontendEntryPoint
{
    public static string Restore(string path, string text) => path == ".frontend/main.tsx"
        ? text.Replace(
            """
            import {
                App,
                StageSourceProvider,
                staticStageSource,
                stageTheme,
                type StageStaticContent,
            } from '@cratis/stage.runtime';
            """,
            "import { App } from './stage/App';\nimport { StageSourceProvider, staticStageSource, type StageStaticContent } from './stage/stageSource';\nimport { stageTheme } from './stage/stageTheme';",
            StringComparison.Ordinal)
            .Replace(
                "and `@cratis/stage.runtime` is the same\n// runtime a live Stage renders that Scene with. Editing this file to add a screen would put it outside the",
                "and `./stage` is the same runtime a\n// live Stage renders that Scene with. Editing this file to add a screen would put it outside the",
                StringComparison.Ordinal)
            .Replace(
            "StageSourceProvider, staticStageSource, type StageStaticContent",
            "StageSourceProvider, staticStageSource",
            StringComparison.Ordinal)
            .Replace(
                "import { stageTheme } from './stage/stageTheme';",
                "import { stageTheme } from './stage/stageTheme';\nimport { stage } from '../src/stage';",
                StringComparison.Ordinal)
            .Replace(
                """
                // A first-run scaffold has no Scene module yet. An eager glob keeps that frontend buildable and loads
                // the real module, including its bindings and proxy routes, as soon as an application render emits it.
                const stages = import.meta.glob<StageStaticContent>('../src/stage.ts', { eager: true, import: 'stage' });
                const stage = stages['../src/stage.ts'] ?? {
                    scene: { layouts: [], screenTemplates: [], screens: [] },
                    routes: { commands: {}, queries: {} },
                    strings: {},
                };

                """,
                string.Empty,
                StringComparison.Ordinal)
        : text;
}
