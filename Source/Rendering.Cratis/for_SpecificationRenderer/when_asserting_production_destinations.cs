// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Cratis.Stage.Rendering.Cratis.Specifications;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer;

public class when_asserting_production_destinations : given.a_slice_with_specifications
{
    [Theory]
    [InlineData("String", "elsewhere")]
    [InlineData("Uuid", "8954E4E1-8CEE-4B43-8109-08045E7C4063")]
    [InlineData("Int", 42)]
    [InlineData("DestinationId", "8954E4E1-8CEE-4B43-8109-08045E7C4063")]
    [InlineData("Reference", "elsewhere")]
    public void should_assert_against_the_same_typed_destination_as_the_handler(string type, object destination)
    {
        var application = Application(type, "destination");
        var (actual, expected) = RenderAndInvoke(application, destination, [Event("Changed")]);
        ((EventForEventSourceId)actual).EventSourceId.ShouldEqual(expected.Single());
        expected.Single().ToString().ShouldNotEqual("command-source");
    }

    [Theory]
    [InlineData("\"fixed\"")]
    [InlineData("$context.command.destination")]
    public void should_share_expression_resolution_between_handler_and_assertion(string expression)
    {
        var application = Application("String", expression);
        var (actual, expected) = RenderAndInvoke(application, "elsewhere", [Event("Changed")]);
        ((EventForEventSourceId)actual).EventSourceId.ShouldEqual(expected.Single());
    }

    [Fact]
    public void should_keep_conditional_multiple_and_implicit_destinations_separate()
    {
        var application = Application("String", "destination");
        var slice = application.Slices.Single().Slice;
        var command = slice.Commands.Single();
        var conditional = command.Produces.Single() with
        {
            When = new ComparisonConditionSyntax("name", ComparisonOperator.NotEqual, new Screenplay.Syntax.LiteralExpressionSyntax(string.Empty, SourceLocation.Start), SourceLocation.Start),
        };
        var changed = command.Produces.Single() with { Event = "OtherChanged", For = new PathExpressionSyntax("other", SourceLocation.Start) };
        var implicitEvent = command.Produces.Single() with { Event = "ImplicitChanged", For = null };
        application = ReplaceCommand(application, command with { Produces = [conditional, changed, implicitEvent] });
        var (actual, expected) = RenderAndInvoke(application, "elsewhere", [Event("Changed"), Event("OtherChanged"), Event("ImplicitChanged")]);
        var produced = ((IEnumerable<object>)actual).ToArray();
        ((EventForEventSourceId)produced[0]).EventSourceId.ShouldEqual(expected[0]);
        ((EventForEventSourceId)produced[1]).EventSourceId.ShouldEqual(expected[1]);
        produced[2].GetType().Name.ShouldEqual("ImplicitChanged");
        expected[2].ToString().ShouldEqual("command-source");
    }

    [Fact]
    public void should_match_repeated_nonconditional_event_types_to_each_destination()
    {
        var application = Application("String", "destination");
        var command = application.Slices.Single().Slice.Commands.Single();
        application = ReplaceCommand(application, command with { Produces = [command.Produces.Single(), command.Produces.Single() with { For = new PathExpressionSyntax("other", SourceLocation.Start) }] });
        var (actual, expected) = RenderAndInvoke(application, "elsewhere", [Event("Changed"), Event("Changed")]);
        var produced = ((IEnumerable<object>)actual).Cast<EventForEventSourceId>().ToArray();
        produced.Select(value => value.EventSourceId).ShouldEqual(expected);
    }

    [Fact]
    public void should_not_guess_a_destination_for_repeated_conditional_event_types()
    {
        var application = Application("String", "destination");
        var command = application.Slices.Single().Slice.Commands.Single();
        var conditional = command.Produces.Single() with { When = new ComparisonConditionSyntax("name", ComparisonOperator.Equal, new Screenplay.Syntax.LiteralExpressionSyntax("payload", SourceLocation.Start), SourceLocation.Start) };
        application = ReplaceCommand(application, command with { Produces = [conditional, command.Produces.Single() with { For = new PathExpressionSyntax("other", SourceLocation.Start) }] });
        var error = Catch.Exception(() => RenderAndInvoke(application, "elsewhere", [Event("Changed")]));
        error.ShouldBeOfExactType<UnsupportedSpecificationDestination>();
    }

    static ApplicationSet Application(string type, string expression)
    {
        var compilation = new ScreenplayCompiler().Compile($$"""
            concept DestinationId : Uuid
            concept Reference : String
            module Billing
              feature Accounts
                slice StateChange Change
                  command Change
                    id {{(type == "DestinationId" ? "DestinationId" : "String")}} identifier
                    destination {{type}}
                    other String
                    name String
                    produces Changed
                      for {{expression}}
                  event Changed
                  event OtherChanged
                  event ImplicitChanged
            """);
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return new ApplicationSet([compilation.Value!]);
    }

    static ApplicationSet ReplaceCommand(ApplicationSet application, CommandSyntax command)
    {
        var original = application.Applications.Single();
        var module = original.Modules.Single();
        var feature = module.Features.Single();
        return new ApplicationSet([original with { Modules = [module with { Features = [feature with { Slices = [feature.Slices.Single() with { Commands = [command] }] }] }] }]);
    }

    static (object Actual, EventSourceId[] Expected) RenderAndInvoke(ApplicationSet application, object destination, SpecificationEventSyntax[] events)
    {
        var slice = application.Slices.Single();
        var command = slice.Slice.Commands.Single();
        var specification = Specification("Destinations", when: When("Change", ("id", command.Properties.First().Type.Name == "DestinationId" ? "137ac9e1-a91f-4b49-a44c-72bf086dfaf5" : "command-source"), ("destination", destination), ("other", "another"), ("name", "payload")), then: events);
        var assertions = SpecificationRenderer.Render(specification, command, slice, application, "Generated");
        assertions.Diagnostics.ShouldBeEmpty();
        var tree = CSharpSyntaxTree.ParseText(assertions.Content, new CSharpParseOptions(preprocessorSymbols: ["DEBUG"]));
        var calls = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Where(call => call.Expression.ToString().Contains("ShouldHaveAppendedEvent", StringComparison.Ordinal));
        var destinations = calls.Select(call => call.ArgumentList.Arguments[0].ToString());
        var construction = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single(node => node.Type.ToString() == "Change");
        var probe = new RenderedFile("Probe.cs", $$"""
            namespace Generated.Billing.Accounts.Change;
            public static class Probe
            {
                public static object Actual() => {{construction}}.Handle();
                public static global::Cratis.Chronicle.Events.EventSourceId[] Expected() => [{{string.Join(", ", destinations)}}];
            }
            """);
        var files = application.Concepts.Values.Select(concept => ConceptRenderer.Render(concept, application, "Generated"))
            .Concat([new StateChangeSliceRenderer().Render(slice, application, "Generated"), assertions, probe]);
        var assembly = RenderedOutput.Load(files);
        var generated = assembly.GetTypes().Single(candidate => candidate.Name == "Probe");
        return (generated.GetMethod("Actual")!.Invoke(null, [])!, (EventSourceId[])generated.GetMethod("Expected")!.Invoke(null, [])!);
    }
}
