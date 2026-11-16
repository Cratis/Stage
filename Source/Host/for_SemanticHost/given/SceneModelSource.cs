// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.for_SemanticHost.given;

internal static class SceneModelSource
{
    internal static string ForEngine(string engine)
    {
        const string source = Cratis.Stage.Api.for_SceneSynthesizer.given.a_scene_model.Source;

        // Picker and guard specs exercise Scene behavior, not enum command admission.
        // Keep semantic enum coverage; use plain stage fields for legacy execution.
        return engine == "eventmodel"
            ? source.Replace("stage ProjectStage", "stage String", StringComparison.Ordinal)
            : source;
    }
}
