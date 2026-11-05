// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Screenplay;

namespace Cratis.Stage.Host;

internal static class EventModelHost
{
    internal static async Task<StageApplication?> LoadModel(
        string modelPath, List<StageUnsupportedIssue> issues, Func<string, Task<StageApplication>>? load = null)
    {
        try
        {
            var application = await (load ?? EventModelLoader.LoadStageApplicationFromPathAsync)(modelPath);
            ProtectedCommandReadsAdmission.EnsureExecutableBy(application.EventModel, modelPath, "eventmodel");
            if (application.Scene.RuntimeIssues.All(SafeSceneApplication.CanServeWithoutUnsafeBehavior))
            {
                return application with { Scene = SafeSceneApplication.From(application.Scene) };
            }

            issues.AddRange(application.Scene.RuntimeIssues.Select(issue => new StageUnsupportedIssue("Scene", issue.Artifact, issue.Details)));

            return application.Scene.RuntimeIssues.Count > 0 ? null : application;
        }
        catch (Exception exception) when (exception is InvalidEventModel or UnsupportedEventSourceRoutes or UnsupportedPublicEvents or UnsupportedForeignEvents or UnsupportedTranslationDirection or UnsupportedReactionIdentity or UnsupportedObserverFilters or UnsupportedGenerationStrategy or UnsupportedValidationRule)
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
