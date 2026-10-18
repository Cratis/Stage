// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_BehaviorConverter.when_converting_with_diagnostics;

public class and_a_trigger_is_unknown : Specification
{
    [Theory]
    [InlineData(-1)]
    [InlineData(99)]
    public void should_keep_supported_bindings_and_report_the_omitted_trigger(int kind)
    {
        var location = new SourceLocation(6, 3, "behaviors.play");
        var invalid = new InteractionBindingSyntax(new BuiltInInteractionTriggerSyntax((InteractionTriggerKind)kind, location), null, [], location);
        var valid = invalid with { Trigger = new BuiltInInteractionTriggerSyntax(InteractionTriggerKind.Click, location) };
        var behavior = new BehaviorSyntax("Confirm", [], [invalid, valid], location);
        var diagnostics = new ConversionDiagnostics();
        var result = BehaviorConverter.Convert(behavior, null, diagnostics, "Projects");

        result.Bindings.Count.ShouldEqual(1);
        diagnostics.All.Single().Code.ShouldEqual("STAGE-SCENE-BEHAVIOR-001");
        diagnostics.All.Single().Location.ShouldEqual(location);
        diagnostics.All.Single().Path.ShouldEqual("Projects");
        Catch.Exception(() => BehaviorConverter.Convert(behavior)).ShouldBeOfExactType<UnknownInteractionTrigger>();
    }
}
