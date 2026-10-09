// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_BehaviorConverter;

public class when_refusing_guarded_interactions : Specification
{
    [Theory]
    [InlineData("click", false)]
    [InlineData("double click", false)]
    [InlineData("select", true)]
    public void should_refuse_on_conversion_and_render_planning(string trigger, bool otherwise)
    {
        var source = $$"""
            module Work
              feature Items
                slice StateView Details
                  readmodel Item
                    status String
                  query ItemDetails => Item[]
                  screen Details
                    data Item[] via query ItemDetails
                    table Item
                      column status
                      on {{trigger}}
                        when item.status == "open"
                          notify info "Open"
            """;
        if (otherwise) source += "\n            otherwise\n              notify info \"Closed\"";
        var compiled = new ScreenplayCompiler().Compile(source);
        compiled.Success.ShouldBeTrue();
        compiled.Diagnostics.ShouldBeEmpty();
        var application = compiled.Value!;
        var binding = application.Modules.Single().Features.Single().Slices.Single().Screens.Single().Directives
            .OfType<ScreenTableSyntax>().Single().Behaviors.Single().Bindings.Single();
        binding.Actions.ShouldBeEmpty();
        AssertRefused(() => BehaviorConverter.Convert(new BehaviorSyntax(null, [], [binding], binding.Location)), binding);
        AssertRefused(() => RenderPlanner.Plan(new ScreenplaySceneVisitor().Visit(application), []), binding);
    }

    [Fact]
    public void should_refuse_a_fallback_even_without_alternatives()
    {
        var binding = new InteractionBindingSyntax(new BuiltInInteractionTriggerSyntax(InteractionTriggerKind.Click, SourceLocation.Start), null, [], SourceLocation.Start)
        {
            Otherwise = new([], SourceLocation.Start)
        };
        AssertRefused(() => BehaviorConverter.Convert(new BehaviorSyntax(null, [], [binding], binding.Location)), binding);
    }

    static void AssertRefused(Action action, InteractionBindingSyntax binding)
    {
        var error = Catch.Exception(action);
        error.ShouldBeOfExactType<UnsupportedGuardedInteraction>();
        ((UnsupportedGuardedInteraction)error).Location.ShouldEqual(binding.Location);
        error.Message.ShouldContain(UnsupportedGuardedInteraction.DiagnosticCode);
        error.Message.ShouldContain("https://github.com/Cratis/Stage/issues/209");
        error.Message.ShouldContain("https://github.com/Cratis/Scene/issues/68");
    }
}
