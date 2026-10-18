// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding;

public class when_binding_ambiguous_nested_members : Specification
{
    Exception? _error;

    void Because()
    {
        var plan = compiled_plan.From("""
            type Child
              label String
            type Details
              child Child
            module Inputs
              feature Checking
                slice StateChange Check
                  command CheckInput
                    details Details
                    returns details
            """);
        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""{"details":{"child":{"label":"one","Label":"two"}}}""")!;
        _error = Catch.Exception(() => SemanticJsonValues.Bind(plan.Commands.Values.Single().Properties, payload, plan.Model));
    }

    [Fact] void should_identify_the_ambiguous_nested_member_path() => Assert.IsType<UnsupportedSemanticValue>(_error).Message.ShouldEqual("Ambiguous composite property 'details.child.label'.");
}
