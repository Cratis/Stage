// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_EventRouteSyntaxAdmission;

public class when_auditing_the_route_surface : Specification
{
    [Fact]
    public void should_require_a_decision_for_every_route_like_member()
    {
        var actual = typeof(SyntaxNode).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith("Cratis.Screenplay.Syntax", StringComparison.Ordinal) == true && typeof(SyntaxNode).IsAssignableFrom(type))
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(property => property.Name.Contains("Stream", StringComparison.OrdinalIgnoreCase) || property.Name.Contains("Route", StringComparison.OrdinalIgnoreCase) || property.Name == "EventSources")
                .Select(property => $"{type.Name}.{property.Name}"))
            .Order(StringComparer.Ordinal);

        // Application collections are traversed by the walker. Screen navigation belongs to UiSyntaxAdmission,
        // not event routing. Concurrency dimensions describe append scope, not a named route selection.
        // Every other owner below has explicit route detection in EventRouteSyntaxAdmission.
        string.Join(' ', actual).ShouldEqual("ApplicationSyntax.EventSources CommandStreamSyntax.Stream CommandStreamSyntax.StreamId CommandStreamSyntax.StreamIdParts CommandSyntax.Stream CommandSyntax.StreamCandidates ConcurrencySyntax.EventStreamId ConcurrencySyntax.EventStreamType EventSourceSyntax.Streams EventStreamSyntax.StreamId EventStreamSyntax.StreamIdParts ScreenNavigateSyntax.Route SpecificationEventSyntax.NoStream SpecificationEventSyntax.Stream SpecificationExampleSyntax.NoStream SpecificationExampleSyntax.Stream SpecificationRedeliverySyntax.NoStream SpecificationRedeliverySyntax.Stream SpecificationStreamSyntax.Stream SpecificationStreamSyntax.StreamId SpecificationStreamSyntax.StreamIdParts");
    }

    [Fact]
    public void should_detect_every_event_route_owner()
    {
        var location = SourceLocation.Start;
        var stream = new SpecificationStreamSyntax("Invoice", "Changes", location);
        SyntaxNode[] routed =
        [
            new EventSourceSyntax("Invoice", location),
            new EventStreamSyntax("Changes", location),
            new CommandStreamSyntax("Invoice", "Changes", location),
            new CommandSyntax("RegisterInvoice", [], null, [], [], null, location) { StreamCandidates = [new("Invoice", "Changes", location)] },
            new SpecificationEventSyntax("InvoiceRegistered", [], location) { Stream = stream },
            new SpecificationExampleSyntax("Registered", "InvoiceRegistered", [], location) { Stream = stream },
            new SpecificationRedeliverySyntax("InvoiceRegistered", "React", [], location) { Stream = stream },
            stream,
            new SpecificationNoStreamSyntax(location)
        ];
        foreach (var node in routed) EventRouteSyntaxAdmission.IsRouted(node).ShouldBeTrue();
    }

    [Fact]
    public void should_keep_capture_appends_unrouted_until_the_language_adds_routing()
    {
        var members = typeof(CaptureAppendSyntax).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => property.Name).Where(name => name != nameof(SyntaxNode.Location)).Order(StringComparer.Ordinal);
        string.Join(' ', members).ShouldEqual("Event Mappings Tags When");
        EventRouteSyntaxAdmission.IsRouted(new CaptureAppendSyntax("InvoiceRegistered", null, [], SourceLocation.Start)).ShouldBeFalse();
    }
}
