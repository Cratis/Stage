// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_specifications;

public class with_composite_command_values : Specification
{
    SemanticRuntimeAdmission _admission = null!;

    void Because() => _admission = new(compiled_plan.From("""
        concept Count : Int
          validate
            > 5
        type Details
          count Count optional
          next Details optional
        module Inputs
          feature Checking
            slice StateChange Check
              command CheckInput
                id String
                value Details optional
                returns id
              specification CheckingInput
                when CheckInput
                  id = "one"
                  value = {"count":7,"next":{"count":8}}
                then returns "one"
        """));

    [Fact] void should_report_supported_specifications() => _admission.Entries.Single(entry => entry.Kind == "specification").Status.ShouldEqual("supported");
    [Fact] void should_not_block_the_model() => _admission.Blocking.ShouldBeEmpty();
}
