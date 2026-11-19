// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class invocation_constraint_model
{
    internal const string Source = """
        concept CompletionId : Uuid
          generate uuidv7
        module Work
          feature Processing
            slice Automation FollowUp
              command Start
                id String identifier
                produces Started
                  for id
                  id = id
              event Started
                id String
              reaction Worker
                runs as system
                when Started
                  invokes Complete
                    id = id
                    on refused by constraint OneCompletion
                      acknowledge
              specification Recorded
                given generated Complete.completion = "c0ffee00-0000-7000-8000-000000000001"
                when append Started
                  for "fine"
                  id = "fine"
                then invokes Complete
                  id = "fine"
            slice StateChange Completion
              command Complete
                completion CompletionId generated identifier
                id String
                produces Completed
                  for completion
                  id = id
                produces CompletionRecorded
                  for id
                  completion = completion
              event CompletionRecorded
                completion CompletionId
              event Completed
                id String
              constraint OneCompletion
                unique id on Completed
        """;

    internal const string Claimed = """
              specification Claimed
                given generated Complete.completion = "c0ffee00-0000-7000-8000-000000000001"
                given Completed
                  for "c0ffee00-0000-4000-8000-000000000009"
                  id = "fine"
                when append Started
                  for "fine"
                  id = "fine"
                then invokes Complete
                  id = "fine"
        """;

    internal const string Mismatched = """
              specification Mismatched
                given generated Complete.completion = "c0ffee00-0000-7000-8000-000000000001"
                when append Started
                  for "fine"
                  id = "fine"
                then invokes Complete
                  id = "wrong"
        """;
}
