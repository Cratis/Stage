// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public static class register_project_scenarios
{
    public static string WithPriorProjectAndSubsetExpectation => RegisterProjectCorpus.LegacyV1.SourceForms
        .Single(_ => _.Name == "single").Documents.Single().Text
        .Replace("specification RegisteringAProject\n", "specification RegisteringAProject\n        given ProjectRegistered\n          for \"4fa85f64-5717-4562-b3fc-2c963f66afa7\"\n          projectId = \"4fa85f64-5717-4562-b3fc-2c963f66afa7\"\n          name = \"Earlier project\"\n", StringComparison.Ordinal)
        .Replace("then readmodel ProjectSummary\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          name = \"Screenplay\"", "then readmodel ProjectSummary\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"", StringComparison.Ordinal);
}
