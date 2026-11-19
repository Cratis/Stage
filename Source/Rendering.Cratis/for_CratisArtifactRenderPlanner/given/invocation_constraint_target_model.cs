// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

/// <summary>
/// A reaction whose triggering event is the target of a unique constraint and which invokes a command owned by another slice, with specifications that assert only the invocation.
/// </summary>
/// <remarks>
/// Recorded states the invocation the reaction makes. Mismatching states another command value, so it must fail an assertion. Conflicting appends a value another event source already holds and asserts that the constraint refuses it.
/// </remarks>
internal static class invocation_constraint_target_model
{
    internal static ExecutableSemanticModel Create() => invocation_refusal_outcome_model.Create(Source, true);

    const string Source = """
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
              constraint UniqueStarted
                unique id on Started
              reaction Worker
                runs as system
                when Started
                  invokes Complete
                    id = id
              specification Recorded
                when append Started
                  for "fine"
                  id = "fine"
                then no events
              specification Mismatching
                when append Started
                  for "fine"
                  id = "fine"
                then no events
              specification Conflicting
                when append Started
                  for "fine"
                  id = "fine"
                then no events
            slice StateChange Completion
              command Complete
                id String identifier
                produces Completed
                  for id
              event Completed
        """;
}
#endif
