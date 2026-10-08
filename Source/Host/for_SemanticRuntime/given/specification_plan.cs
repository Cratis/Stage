// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Host.for_SemanticRuntime.given;

public static class specification_plan
{
    public static SemanticExecutionPlan Create(bool seeded = false) => compiled_plan.From(Source(seeded));

    public static string Source(bool seeded = false) => $$"""
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId String identifier
                name String
                produces ProjectRegistered
                  for projectId
                  name = name
              event ProjectRegistered
                name String
              readmodel ProjectSummary
                projectId String
                name String
              query ProjectById => ProjectSummary?
                by projectId String
              specification RegisteringAProject
        {{(seeded ? "        given readmodel ProjectSummary\n          projectId = \"first\"\n          name = \"Existing\"" : string.Empty)}}
                when RegisterProject
                  projectId = "first"
                  name = "Screenplay"
                then ProjectRegistered
                  for "first"
                  name = "Screenplay"
        """;
}
