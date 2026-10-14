// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer.given;

public class a_redelivery_specification : a_reaction_specification
{
    internal const string RedeliverySource = """
        module Work
          feature Processing
            slice Automation FollowUp
              command Begin
                id String identifier
                produces Started
                  for id
                  id = id
              event Started
                id String
              event Followed
                id String
              reaction Worker
                when Started
                  produces Followed
                    id = id
              reaction Sibling
                when Started
                  produces Followed
                    id = id
              specification Following
                given Followed
                  for "earlier"
                  id = "earlier"
                given Started
                  for "source"
                  id = "value"
                when redelivered Started to Worker
                then Followed
                  for "source"
                  id = "value"
        """;

    void Establish() => Prepare(RedeliverySource);
}
