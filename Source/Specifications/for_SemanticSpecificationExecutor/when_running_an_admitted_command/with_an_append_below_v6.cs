// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_an_append_below_v6 : an_append_comparison
{
    async Task Because() => await RunAppend(SemanticVersion.V2);

    [Fact] void should_keep_the_action_in_then_events_like_the_reference() => AssertParity(true);
}
