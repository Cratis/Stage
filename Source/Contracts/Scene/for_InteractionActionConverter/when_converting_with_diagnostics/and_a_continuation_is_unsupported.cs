// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_InteractionActionConverter.when_converting_with_diagnostics;

public class and_a_continuation_is_unsupported : Specification
{
    [Theory]
    [InlineData("success")]
    [InlineData("failure")]
    [InlineData("result")]
    public void should_keep_the_parent_action_and_supported_continuations(string branch)
    {
        var location = new SourceLocation(6, 3, "actions.play");
        InteractionActionSyntax[] next = [new UnknownAction(location), new NavigateBackActionSyntax(location)];
        var action = new ExecuteCommandActionSyntax("Register", location)
        {
            OnSuccess = branch == "success" ? next : [],
            OnFailure = branch == "failure" ? next : [],
            OnResult = branch == "result" ? next : []
        };
        var diagnostics = new ConversionDiagnostics();
        var result = InteractionActionConverter.Convert(action, diagnostics, "Register.onClick")!;

        result.OnSuccess.Concat(result.OnFailure).Concat(result.OnResult).Count().ShouldEqual(1);
        diagnostics.All.Single().Code.ShouldEqual("STAGE-SCENE-ACTION-003");
        diagnostics.All.Single().Location.ShouldEqual(location);
        diagnostics.All.Single().Path.ShouldEqual("Register.onClick");
        Catch.Exception(() => InteractionActionConverter.Convert(action)).ShouldBeOfExactType<UnknownInteractionAction>();
    }

    sealed record UnknownAction(SourceLocation Location) : InteractionActionSyntax(Location);
}
