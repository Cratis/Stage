// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host;

internal enum StageRuntimeEngine
{
    EventModel,
    Semantic
}

internal static class StageRuntimeEngineSelection
{
    internal static StageRuntimeEngine Read(string[] arguments)
    {
        var configured = arguments.FirstOrDefault(argument => argument.StartsWith("--engine=", StringComparison.Ordinal))?[9..]
            ?? Environment.GetEnvironmentVariable("Stage__Runtime__Engine") ?? "eventmodel";
        return configured.ToLowerInvariant() switch
        {
            "eventmodel" => StageRuntimeEngine.EventModel,
            "semantic" => StageRuntimeEngine.Semantic,
            _ => throw new UnsupportedStageRuntimeEngine(configured)
        };
    }
}

internal sealed class UnsupportedStageRuntimeEngine(string engine) : Exception($"Stage runtime engine '{engine}' is not supported.");
