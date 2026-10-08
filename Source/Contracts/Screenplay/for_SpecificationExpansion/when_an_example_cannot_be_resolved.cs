// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_SpecificationExpansion;

public class when_an_example_cannot_be_resolved : Specification
{
    ApplicationSyntax _application = null!;
    Exception? _error;

    void Establish() => _application = new ScreenplayCompiler().Parse("""
        module Projects
          feature Registration
            slice StateChange Register
              example Input : MissingCommand
                name = "project"
              specification RegisteringAProject
                when Input
        """).Value!;

    void Because() => _error = Catch.Exception(() => SpecificationExpansion.Expand(_application));

    [Fact] void should_fail_with_a_typed_diagnostic() => _error.ShouldBeOfExactType<InvalidEventModel>();
    [Fact] void should_preserve_the_compiler_diagnostic_code() => _error!.Message.ShouldContain("PLAY0520");
    [Fact] void should_name_the_unresolved_declaration() => _error!.Message.ShouldContain("MissingCommand");
}
