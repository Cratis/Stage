// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Screenplay;

namespace Cratis.Stage.Host;

internal static class EventModelHost
{
    internal static async Task<StageApplication?> LoadModel(string modelPath, List<StageUnsupportedIssue> issues)
    {
        try
        {
            var application = await EventModelLoader.LoadStageApplicationFromPathAsync(modelPath);
            if (application.Scene.RuntimeIssues.All(SafeSceneApplication.CanServeWithoutUnsafeBehavior))
            {
                return application with { Scene = SafeSceneApplication.From(application.Scene) };
            }

            issues.AddRange(application.Scene.RuntimeIssues.Select(issue => new StageUnsupportedIssue("Scene", issue.Artifact, issue.Details)));

            return application.Scene.RuntimeIssues.Count > 0 ? null : application;
        }
        catch (UnsupportedEventSourceRoutes exception)
        {
            issues.Add(new StageUnsupportedIssue("Plan", "model", exception.Message));

            return null;
        }
        catch (Exception exception) when (exception is UnsupportedUiSyntax)
        {
            issues.Add(new StageUnsupportedIssue("Scene", "model", exception.Message));

            return null;
        }
    }
}
