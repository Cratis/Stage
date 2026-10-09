// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_EventRouteSyntaxAdmission;

public class when_refusing_event_routes : Specification
{
    [Theory]
    [InlineData("source")]
    [InlineData("source-without-streams")]
    [InlineData("command")]
    [InlineData("example-stream")]
    [InlineData("example-no-stream")]
    [InlineData("redelivery-stream")]
    [InlineData("redelivery-no-stream")]
    [InlineData("occurrence-stream")]
    [InlineData("occurrence-no-stream")]
    public void should_refuse_routes_at_the_authored_location(string form)
    {
        var location = SourceLocation.Start with { Path = "Routes.play", Line = 10 };
        var slice = WithRoute(Slice(location), form, location);
        var application = new ApplicationSyntax([], [], [], [new ModuleSyntax("Billing", [], [new FeatureSyntax("Invoices", [], [slice], location)], location)], location)
        {
            EventSources = form switch
            {
                "source" => [new EventSourceSyntax("Invoice", location) { Streams = [new("Changes", location)] }],
                "source-without-streams" => [new EventSourceSyntax("Invoice", location)],
                _ => []
            }
        };
        var error = Catch.Exception(() => new EventRouteSyntaxAdmission().VisitApplication(application));
        error.ShouldBeOfExactType<UnsupportedEventSourceRoutes>();
        error.Message.StartsWith("STAGE-ESM-030:", StringComparison.Ordinal).ShouldBeTrue();
        ((UnsupportedEventSourceRoutes)error).Location.ShouldEqual(location);
    }

    [Fact]
    public void should_admit_unrouted_nodes()
    {
        var location = SourceLocation.Start;
        SyntaxNode[] nodes =
        [
            Slice(location).Commands.Single(),
            new SpecificationExampleSyntax("Registered", "InvoiceRegistered", [], location),
            new SpecificationRedeliverySyntax("InvoiceRegistered", "React", [], location),
            new SpecificationEventSyntax("InvoiceRegistered", [], location),
            new CaptureAppendSyntax("InvoiceRegistered", null, [], location)
        ];
        foreach (var node in nodes)
        {
            EventRouteSyntaxAdmission.IsRouted(node).ShouldBeFalse();
            Catch.Exception(() => new EventRouteSyntaxAdmission().VisitNode(node)).ShouldBeNull();
        }
    }

    [Fact]
    public void should_refuse_standalone_route_nodes()
    {
        var location = SourceLocation.Start;
        foreach (var node in new SyntaxNode[] { new EventStreamSyntax("Changes", location), new CommandStreamSyntax("Invoice", "Changes", location), new SpecificationStreamSyntax("Invoice", "Changes", location), new SpecificationNoStreamSyntax(location) })
        {
            var error = Catch.Exception(() => new EventRouteSyntaxAdmission().VisitNode(node));
            error.ShouldBeOfExactType<UnsupportedEventSourceRoutes>();
            ((UnsupportedEventSourceRoutes)error).Location.ShouldEqual(location);
        }
    }

    static SliceSyntax Slice(SourceLocation location) => new(Cratis.Screenplay.Syntax.SliceType.StateChange, "RegisterInvoice", [], [new CommandSyntax("RegisterInvoice", [], null, [], [], null, location)], [], [], [], [], [], [], [], location);

    static SliceSyntax WithRoute(SliceSyntax slice, string form, SourceLocation location)
    {
        var route = new SpecificationStreamSyntax("Invoice", "Changes", location);
        var noStream = new SpecificationNoStreamSyntax(location);
        var example = new SpecificationExampleSyntax("Registered", "InvoiceRegistered", [], location)
        {
            Stream = form == "example-stream" ? route : null,
            NoStream = form == "example-no-stream" ? noStream : null
        };
        var redelivery = new SpecificationRedeliverySyntax("InvoiceRegistered", "React", [], location)
        {
            Stream = form == "redelivery-stream" ? route : null,
            NoStream = form == "redelivery-no-stream" ? noStream : null
        };
        var occurrence = new SpecificationEventSyntax("InvoiceRegistered", [], location)
        {
            Stream = form == "occurrence-stream" ? route : null,
            NoStream = form == "occurrence-no-stream" ? noStream : null
        };
        var specification = new SpecificationSyntax("Routes", [], null, [], [], location)
        {
            WhenRedelivered = form.StartsWith("redelivery", StringComparison.Ordinal) ? redelivery : null,
            ThenEvents = form.StartsWith("occurrence", StringComparison.Ordinal) ? [occurrence] : []
        };

        return slice with
        {
            Commands = form == "command" ? [slice.Commands.Single() with { Stream = new("Invoice", "Changes", location) }] : slice.Commands,
            Specifications = [specification],
            Examples = form.StartsWith("example", StringComparison.Ordinal) ? [example] : []
        };
    }
}
