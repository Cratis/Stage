// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_append_refusals;

public class with_an_append_a_unique_value_constraint_refuses : given.a_specification_in_a_slice
{
    internal const string Source = """
        module Work
          feature Processing
            slice StateChange Register
              command Register
                id String identifier
                code String
                produces Registered
                  for id
                  code = code
              event Registered
                code String
              constraint UniqueCode
                unique code on Registered
                message "Code taken"
              specification CodesAreUnique
                given Registered
                  for "one"
                  code = "A-1"
                when append Registered
                  for "two"
                  code = "A-1"
                then error "Code taken"
        """;

    void Establish() => Prepare(Source);
    void Because() => Validate();

    [Fact] void should_render_the_specification() => _diagnostics.ShouldBeEmpty();
}
