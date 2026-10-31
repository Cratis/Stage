// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Common;
using Cratis.Scene.Model.Interactions;
using Cratis.Screenplay;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_FormConverter.when_converting_submit_continuations;

public class and_the_command_returns_a_record : Specification
{
    Cratis.Scene.Model.Forms.Form _form = null!;

    void Because()
    {
        const string source = """
            concept ProjectId : Uuid
            behavior Continue
              on submit
                navigate to Details
                  with projectId from registeredId
            module Projects
              form Register for RegisterProject
                uses Continue
              feature Registration
                slice StateChange Register
                  command RegisterProject
                    projectId ProjectId generated identifier
                    returns
                      registeredId = projectId
                  screen Overview
                    action RegisterProject
                  screen Details
                    title "Project"
            """;
        var compilation = new ScreenplayCompiler().Compile(source);
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        _form = new ScreenplaySceneVisitor().Visit(compilation.Value!).Screens.Single(screen => screen.Name == "Overview").Forms.Single();
    }

    [Fact] void should_carry_the_used_behavior_as_a_submit_continuation() => _form.OnSubmit.Count.ShouldEqual(1);
    [Fact] void should_not_run_the_used_submit_behavior_twice() => _form.Behaviors.ShouldBeEmpty();
    [Fact] void should_bind_the_named_field() => _form.OnSubmit.Single().Arguments.Single().Value.Path.ShouldEqual("registeredId");
    [Fact] void should_read_only_the_command_response() => _form.OnSubmit.Single().Arguments.Single().Value.Kind.ShouldEqual(BindingSourceKind.CommandResponse);
}
