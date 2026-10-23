// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_flat_transitions;

public class with_two_transitions : given.a_flat_projection
{
    Task Because() => Run(
        """
        from Claimed key alias
          id = alias
          owner = owner
        from Released key alias
          id = alias
          note = note
        """,
        """
        given Claimed
          for "src"
          alias = "a1"
          owner = "bob"
        given Released
          for "other"
          alias = "a1"
          note = "gone"
        """,
        """
        id = "a1"
        owner = "bob"
        note = "gone"
        """);

    [Fact] void should_match_the_reference() => AssertParity(true);
}
