// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_reaction_invocation_ordering : a_reaction_plan
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task should_reject_before_destination_admission(bool denied)
    {
        Compile("reaction Worker\n  when Started\n    invokes Complete\n      item = \"\"", denied ? "then denied" : "then error \"Item required\"", new()
        {
            CommandDestination = "",
            Policy = denied ? "policy Access\n  require role \"Automation\"" : "",
            Authorization = denied ? "authorize Access" : "validate\n  item not empty message \"Item required\""
        });
        await Execute();
        AssertMatchesReference();
    }

    [Fact]
    public async Task should_project_an_explicit_invocation_destination_after_an_unsourced_append()
    {
        Compile("reaction Worker\n  when Started\n    item\n    invokes Complete\n      item = item", "then readmodel Marker\n  item = \"one\"", new() { Action = "when append Started\n  item = \"one\"\n  amount = 1" });
        await Execute();
        AssertMatchesReference();
        Assert.Single(_result.Trace!.ReadModels);
    }
}
