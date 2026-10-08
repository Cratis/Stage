// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Screenplay;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_rendering_explicit_destinations
{
    [Theory]
    [InlineData("String", "elsewhere")]
    [InlineData("Uuid", "8954e4e1-8cee-4b43-8109-08045e7c4063")]
    [InlineData("Int", "42")]
    [InlineData("DestinationId", "8954e4e1-8cee-4b43-8109-08045e7c4063")]
    public void should_return_a_single_event_with_its_destination(string type, string expected)
    {
        var source = Model(type, "produces Changed\n  for destination\n  name = name");
        var (command, assembly, content) = Render(source, type, expected);
        if (type == "String" || type == "Uuid")
        {
            content.ShouldContain("EventForEventSourceId(Destination, new Changed(Name))");
            content.ShouldNotContain("ToString()");
        }

        var produced = (EventForEventSourceId)command.GetType().GetMethod("Handle")!.Invoke(command, [])!;
        produced.EventSourceId.ToString().ShouldEqual(expected);
        produced.Event.GetType().GetProperty("Name")!.GetValue(produced.Event).ShouldEqual("payload");
        assembly.GetTypes().ShouldContain(candidate => candidate.Name == "Changed");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void should_keep_conditional_and_implicit_destinations_independent(bool includeConditional)
    {
        var source = Model("String", """
            produces when name != ""
              Changed
                for destination
                name = name
            produces OtherChanged
              for other
            produces ImplicitChanged
              for id
            """);
        var (command, _, _) = Render(source, "String", "elsewhere", includeConditional ? "payload" : string.Empty, omitLastDestination: true);
        var produced = ((IEnumerable<object>)command.GetType().GetMethod("Handle")!.Invoke(command, [])!).ToArray();
        produced.Length.ShouldEqual(includeConditional ? 3 : 2);
        if (includeConditional)
        {
            ((EventForEventSourceId)produced[0]).EventSourceId.ToString().ShouldEqual("elsewhere");
        }

        ((EventForEventSourceId)produced[^2]).EventSourceId.ToString().ShouldEqual("another");
        produced[^1].GetType().Name.ShouldEqual("ImplicitChanged");
    }

    static string Model(string type, string productions) => $$"""
        concept DestinationId : Uuid
        module Billing
          feature Accounts
            slice StateChange Change
              command Change
                id {{(type == "DestinationId" ? "DestinationId" : "Uuid")}} identifier
                destination {{type}}
                other String
                name String
                {{productions.Replace("\n", "\n        ", StringComparison.Ordinal)}}
              event Changed
                name String
              event OtherChanged
              event ImplicitChanged
        """;

    static (object Command, System.Reflection.Assembly Assembly, string Content) Render(string source, string type, string destination, string name = "payload", bool omitLastDestination = false)
    {
        var compilation = new ScreenplayCompiler().Compile(source);
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var application = new ApplicationSet([compilation.Value!]);
        var slice = application.Slices.Single();
        if (omitLastDestination)
        {
            var declaration = slice.Slice.Commands.Single();
            slice = slice with
            {
                Slice = slice.Slice with
                {
                    Commands = [declaration with { Produces = [.. declaration.Produces.Select(produced => produced.Event == "ImplicitChanged" ? produced with { For = null } : produced)] }],
                },
            };
        }

        var rendered = new StateChangeSliceRenderer().Render(slice, application, "Generated");
        var files = application.Concepts.Values.Select(concept => ConceptRenderer.Render(concept, application, "Generated"))
            .Append(rendered);
        var assembly = RenderedOutput.Load(files);
        var value = type switch
        {
            "Uuid" => Guid.Parse(destination),
            "Int" => int.Parse(destination, System.Globalization.CultureInfo.InvariantCulture),
            "DestinationId" => Activator.CreateInstance(assembly.GetTypes().Single(candidate => candidate.Name == "DestinationId"), Guid.Parse(destination))!,
            _ => destination,
        };
        var identifier = type == "DestinationId"
            ? Activator.CreateInstance(value.GetType(), Guid.NewGuid())!
            : Guid.NewGuid();
        var command = Activator.CreateInstance(assembly.GetTypes().Single(candidate => candidate.Name == "Change"), identifier, value, "another", name)!;
        return (command, assembly, rendered.Content);
    }
}
