// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;

namespace Cratis.Stage.Host;

internal static class EventModelHost
{
    internal static async Task<StageApplication?> LoadModel(string modelPath, List<StageUnsupportedIssue> issues)
    {
        try
        {
            return await EventModelLoader.LoadStageApplicationFromPathAsync(modelPath);
        }
        catch (Exception exception) when (exception is UnsupportedUiSyntax)
        {
            issues.Add(new StageUnsupportedIssue("Scene", "model", exception.Message));

            return null;
        }
    }
}
