// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Rendering.Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer;

public class when_an_example_names_ambiguous_rendered_events : Specification
{
    ApplicationSet _context = null!;
    LocatedSlice _slice = null!;
    Exception? _error;

    void Establish()
    {
        var application = new ScreenplayCompiler().Parse("""
            module Projects
              feature Registration
                slice StateChange Register
                  event Registered
                    name String
                  example Fact : Registered
                    name = "project"
                  command Register
                    name String
                  specification Registration
                    when Register
                      name = "project"
                    then Fact
                slice StateChange Other
                  event Registered
                    name String
            """).Value!;
        _context = new ApplicationSet([application]);
        _slice = _context.Slices.Single(slice => slice.Slice.Name == "Register");
    }

    void Because() => _error = Catch.Exception(() => SpecificationRenderer.Render(_slice.Slice.Specifications.Single(), _slice.Slice.Commands.Single(), _slice, _context, "Projects"));

    [Fact] void should_refuse_instead_of_asserting_on_the_wrong_type() => _error.ShouldBeOfExactType<InvalidEventModel>();
    [Fact] void should_name_the_qualified_event() => _error!.Message.ShouldContain("Projects.Registration.Register.Registered");
    [Fact] void should_explain_the_available_rendering_path() => _error!.Message.ShouldContain("Use semantic rendering");
}
