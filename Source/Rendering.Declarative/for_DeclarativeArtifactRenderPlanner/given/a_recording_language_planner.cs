// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.given;

/// <summary>
/// Plans the test kit with a caller-owned test language, so specs can inspect what the language received and make it refuse.
/// </summary>
public class a_recording_language_planner : a_declarative_planner
{
    internal static ArtifactRenderPlan PlanWithLanguage(Dictionary<string, string> templates, a_test_language language, Templates.TemplateBudget? budget = null)
    {
        var files = KitFiles();
        foreach (var (path, text) in templates)
        {
            files[path] = Bytes(text);
        }

        var request = Request(Profile(Archive(files)));
        return (budget is null ? new DeclarativeArtifactRenderPlanner([language]) : new DeclarativeArtifactRenderPlanner([language], new(budget))).Plan(request);
    }
}
