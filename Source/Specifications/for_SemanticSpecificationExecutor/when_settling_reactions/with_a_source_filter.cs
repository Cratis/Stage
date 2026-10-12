// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_a_source_filter : given.a_reaction_plan
{
    [Theory]
    [InlineData("Account", "Account.Notes\n  streamId = \"period\"", true)]
    [InlineData("Account.Notes", "Account.Notes\n  streamId = \"period\"", true)]
    [InlineData("Account.Notes", "Account.All", false)]
    [InlineData("Account", "Other.All", false)]
    [InlineData("Account", "", false)]
    [InlineData("Account.Notes", "", false)]
    public async Task should_match_only_the_declared_source_and_stream(string filter, string route, bool matches)
    {
        var stream = route.Length == 0 ? "" : "\n  stream " + route.Replace("\n", "\n  ", StringComparison.Ordinal);
        Compile($"reaction Worker\n  from {filter}\n  when Started\n    item\n    produces Followed\n      item = item", matches ? "then Followed\n  item = \"one\"\n  no stream" : "", new() { Action = "when append Started\n  for \"one\"\n  item = \"one\"\n  amount = 1" + stream, EventSources = "eventsource Account\n  identifier String\n  stream Notes\n    streamId String\n  stream All\neventsource Other\n  identifier String\n  stream All" });
        await Execute();
        AssertMatchesReference();
        _result.Trace!.Facts.Count.ShouldEqual(matches ? 2 : 1);
    }
}
