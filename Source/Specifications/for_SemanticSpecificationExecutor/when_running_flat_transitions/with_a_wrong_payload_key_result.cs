// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_flat_transitions;

public class with_a_wrong_payload_key_result : given.a_flat_projection
{
    Task Because() => Run(
        """
        from Claimed key alias
          id = alias
          owner = owner
        """,
        """
        given Claimed
          for "src"
          alias = "a1"
          owner = "bob"
        """,
        """
        id = "a1"
        owner = "alice"
        """);

    [Fact] void should_match_the_reference() => AssertParity(false);
}
