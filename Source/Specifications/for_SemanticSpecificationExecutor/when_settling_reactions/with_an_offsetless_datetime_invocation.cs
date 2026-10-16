// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_an_offsetless_datetime_invocation : given.a_reaction_plan
{
    void Establish()
    {
        Compile(
            "reaction Worker\n  when Started\n    item\n    invokes Complete\n      item = item\n      at = \"2026-01-01T00:00:00.0000000Z\"",
            "then error \"DateTime command property 'at' requires an explicit offset or Z.\"",
            new() { CommandProperties = "at DateTime\nvalidate\n  at >= \"2026-01-01T00:00:00Z\"" });

        // Valid source literals require offsets. Inject malformed transport data after plan validation to exercise invocation preparation.
        var expression = (SemanticValueExpression)_plan.Reactions.Single().Triggers.Single().Invokes.Single().Mappings.Single(mapping => mapping.Source is SemanticValueExpression).Source;
        typeof(SemanticValueExpression).GetField("<Value>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(expression, SemanticValue.Text("2026-01-01T00:00:00"));
    }

    async Task Because() => await Execute();

    [Fact] void should_match_reference_rejection() => AssertMatchesReference();
    [Fact] void should_preserve_the_offset_failure() => _result.Trace!.Rejection.ShouldEqual("DateTime command property 'at' requires an explicit offset or Z.");
}
