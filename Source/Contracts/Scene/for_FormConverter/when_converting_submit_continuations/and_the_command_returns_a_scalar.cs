// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Common;
using Cratis.Scene.Model.Interactions;
using Cratis.Screenplay;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_FormConverter.when_converting_submit_continuations;

public class and_the_command_returns_a_scalar : Specification
{
    Cratis.Scene.Model.Forms.Form _form = null!;

    void Because()
    {
        const string source = """
            concept ProjectId : Uuid
            module Projects
              form Register for RegisterProject
                on submit
                  navigate to Details
                    with projectId from projectId
              feature Registration
                slice StateChange Register
                  command RegisterProject
                    projectId ProjectId generated identifier
                    returns projectId
                  screen Overview
                    action RegisterProject
                  screen Details
                    title "Project"
            """;
        var compilation = new ScreenplayCompiler().Compile(source);
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        _form = new ScreenplaySceneVisitor().Visit(compilation.Value!).Screens.Single(screen => screen.Name == "Overview").Forms.Single();
    }

    [Fact] void should_carry_the_submit_action_once() => _form.OnSubmit.Count.ShouldEqual(1);
    [Fact] void should_not_also_attach_a_submit_behavior() => _form.Behaviors.ShouldBeEmpty();
    [Fact] void should_read_the_scalar_root() => _form.OnSubmit.Single().Arguments.Single().Value.Path.ShouldEqual(string.Empty);
    [Fact] void should_read_the_command_response() => _form.OnSubmit.Single().Arguments.Single().Value.Kind.ShouldEqual(BindingSourceKind.CommandResponse);
    [Fact] void should_keep_the_destination() => ((NavigateAction)_form.OnSubmit.Single()).Screen.ShouldEqual("Details");
}
