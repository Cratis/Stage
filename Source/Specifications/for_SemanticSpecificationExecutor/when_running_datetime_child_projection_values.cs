// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_datetime_child_projection_values : Specification
{
    [Theory]
    [InlineData("2026-01-01T00:00:00Z")]
    [InlineData("2026-01-01T02:00:00.1234567+02:00")]
    public async Task should_compare_child_datetime_values_like_the_reference(string at)
    {
        var plan = when_running_property_keyed_removal_only_projections.Compile($$"""
            type Entry
              code String
              at DateTime
            module Scheduling
              feature Windows
                slice StateChange Record
                  command Open
                    id String identifier
                    name String
                    produces Opened
                      for id
                      name = name
                  command Record
                    id String identifier
                    code String
                    at DateTime
                    produces Recorded
                      for id
                      code = code
                      at = at
                  event Opened
                    name String
                  event Recorded
                    code String
                    at DateTime
                slice StateView Current
                  readmodel Current
                    id String key
                    name String
                    entries Entry[]
                  projection Build => Current
                    no automap
                    from Opened
                      name = name
                    children entries identified by code
                      from Recorded key code
                        parent $eventSourceId
                        code = code
                        at = at
                  specification Children
                    given Opened
                      for "parent"
                      name = "parent name"
                    when append Recorded
                      for "parent"
                      code = "one"
                      at = "{{at}}"
                    then Recorded
                      for "parent"
                      code = "one"
                      at = "{{at}}"
                    then readmodel Current
                      id = "parent"
                      name = "parent name"
                      entries = [{"code":"one","at":"{{at}}"}]
            """);
        var reference = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
        Assert.True(reference.Passed, string.Join(';', reference.Failures));
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed,
            $"{result.Outcome}: {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
    }
}
