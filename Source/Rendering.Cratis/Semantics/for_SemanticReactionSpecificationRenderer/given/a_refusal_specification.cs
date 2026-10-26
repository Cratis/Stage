// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer.given;

public class a_refusal_specification : a_reaction_specification
{
    internal const string RefusalSource = """
        module Work
          feature Processing
            slice Automation FollowUp
              command Start
                id String identifier
                produces Started
                  for id
                  id = id
              command SeedHistory
                id String identifier
                produces Seeded
                  for id
              command Complete
                id String identifier
                produces Completed
                  for id
              event Started
                id String
              event Seeded
              event Completed
              event Failed
                reason String
              reaction Worker
                when Started
                  invokes Complete
                    id = id
                    on refused by validation
                      produces Failed
                        reason = $refusal.message
              specification Completing
                when append Started
                  for "one"
                  id = "one"
                then Completed
                  for "one"
        """;

    void Establish() => Prepare(RefusalSource);
}
