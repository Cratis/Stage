// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_two_child_collections;

public class with_one_missing_parent : Specification
{
    SemanticSpecificationRunRecord _result = null!;
    bool _referencePassed;
    string _referenceFailures = string.Empty;

    async Task Because()
    {
        var plan = when_running_property_keyed_removal_only_projections.Compile(Source);
        var reference = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
        _referencePassed = reference.Passed;
        _referenceFailures = string.Join(';', reference.Failures);
        _result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
    }

    [Fact] void should_update_only_the_present_parent_in_the_reference() => Assert.True(_referencePassed, _referenceFailures);

    [Fact]
    void should_refuse_the_mixed_case_by_name()
    {
        _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        _result.Unsupported!.Details.ShouldContain("several child collections");
    }

    const string Source = """
        type Entry
          code String
        module Work
          feature Work
            slice StateView Lookup
              event Opened
                name String
              event Placed
                code String
                other String
              command RecordOpened
                source String identifier
                name String
                produces Opened
                  for source
                  name = name
              command RecordPlaced
                source String identifier
                code String
                other String
                produces Placed
                  for source
                  code = code
                  other = other
              readmodel Current
                id String key
                name String
                lefts Entry[]
                rights Entry[]
              projection Build => Current
                no automap
                from Opened
                  name = name
                children lefts identified by code
                  from Placed key code
                    parent $eventSourceId
                    code = code
                children rights identified by code
                  from Placed key code
                    parent other
                    code = code
              specification Mixed
                given Opened
                  for "parent"
                  name = "parent name"
                given Placed
                  for "parent"
                  code = "one"
                  other = "absent"
                then readmodel Current
                  id = "parent"
                  name = "parent name"
                  lefts = [{"code":"one"}]
                  rights = []
        """;
}
