// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_selecting_a_projection_refusal : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_choose_the_first_semantic_identity_independently_of_declaration_order(bool reversed)
    {
        var views = Enumerable.Range(0, 8).Select(index => $$"""
                slice StateView View{{index}}
                  readmodel View{{index}}
                    id String key
                    count Int
                  projection Projection{{index}} => View{{index}}
                    no automap
                    from Observed
                      id = $eventSourceId
                      add count by 3
            """);
        var source = """
            module Work
              feature Work
                slice StateChange Recording
                  event Observed
                    name String
                  command Record
                    id String identifier
                    name String
                    produces Observed
                      for id
                      name = name
                  specification Recording
                    when Record
                      id = "one"
                      name = "recorded"
                    then Observed
                      for "one"
                      name = "recorded"
            """ + "\n" + string.Join('\n', reversed ? views.Reverse() : views);
        var plan = when_running_property_keyed_removal_only_projections.Compile(source);
        var specification = plan.Specifications.Values.Single();
        var expected = plan.Projections.Values.OrderBy(projection => projection.Id.ToString(), StringComparer.Ordinal).First();
        var refusal = SemanticRunAdmission.Check(plan, specification);
        Assert.NotNull(refusal);
        refusal.Capability.ShouldEqual(StageExecutionCapability.Projection);
        refusal.Construct.ShouldEqual(expected.Id.ToString());
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new(), TestContext.Current.CancellationToken)).Results);
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        result.Unsupported.ShouldEqual(refusal);
    }
}
