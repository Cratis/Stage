// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_recovering_native_tuple_destinations : Specification
{
    [Theory]
    [InlineData("public (string, EventForEventSourceId) Handle() => (Name, WRAPPER);")]
    [InlineData("public (EventForEventSourceId, string) Handle() => (WRAPPER, Name);")]
    [InlineData("public (Receipt, EventForEventSourceId) Handle() => (new Receipt(Name), WRAPPER);")]
    [InlineData("public (EventForEventSourceId, Receipt) Handle() => (WRAPPER, new(Name));")]
    [InlineData("public (string, EventForEventSourceId[]) Handle() => (Name, [WRAPPER]);")]
    [InlineData("public (EventForEventSourceId[], string) Handle() => ([WRAPPER], Name);")]
    [InlineData("public (string, List<EventForEventSourceId>) Handle() => (Name, new() { WRAPPER });")]
    [InlineData("public (List<EventForEventSourceId>, string) Handle() => (new List<EventForEventSourceId> { WRAPPER }, Name);")]
    [InlineData("public (string, object[]) Handle() => (Name, [WRAPPER, new AuthorAudited(Name)]);")]
    [InlineData("public (object[], string) Handle() => (new object[] { WRAPPER, new AuthorAudited(Name) }, Name);")]
    [InlineData("public Task<(string, EventForEventSourceId)> Handle() => Task.FromResult((Name, WRAPPER));")]
    [InlineData("public Cratis.Monads.Result<(string, EventForEventSourceId), string> Handle() => (Name, WRAPPER);")]
    public void should_keep_the_response_payload_and_each_inline_destination(string handler)
    {
        var recovered = given.arc_recovery.Native(given.native_command_sources.Tuple(handler));
        recovered.AssertHealthy(redundantRoute: true);
        recovered.AssertPropertyDestination(0, "OtherId");
        recovered.AssertNameMapping(0);
        Assert.Equal("AuthorRegistered", recovered.Command.Produces.First().EventName);
        AssertResponse(recovered, handler.Contains("Receipt", StringComparison.Ordinal));
        var mixed = handler.Contains("AuthorAudited", StringComparison.Ordinal);
        Assert.Equal(mixed ? 2 : 1, recovered.BoundCommand.Produces.Length);
        if (mixed)
        {
            Assert.True(recovered.Command.Produces.Last().UsesCommandContext);
            Assert.Equal("AuthorAudited", recovered.Command.Produces.Last().EventName);
            recovered.AssertNameMapping(1);
        }
    }

    [Fact]
    public void should_keep_independent_source_stream_key_and_destination_for_each_tuple_element()
    {
        var recovered = given.arc_recovery.Native(given.native_command_sources.Tuple("public (string, EventForEventSourceId, EventForEventSourceId) Handle() => (Name, WRAPPER, AUDIT);"));
        recovered.AssertHealthy(redundantRoute: true);
        Assert.Equal(2, recovered.BoundCommand.Produces.Length);
        recovered.AssertPropertyDestination(0, "OtherId");
        recovered.AssertPropertyDestination(1, "ThirdId");
        recovered.AssertNameMapping(0);
        recovered.AssertNameMapping(1);
        Assert.Equal("Account", recovered.Command.Authoring.Route.Source);
        Assert.Equal("Main", recovered.Command.Authoring.Route.Stream);
        var production = recovered.Command.Produces.Last();
        Assert.Equal("AuthorAudited", production.EventName);
        Assert.Equal("Ledger", production.Route.Source);
        Assert.Equal("Postings", production.Route.Stream);
        Assert.Equal("Month", production.Route.StreamId);
        var ledger = recovered.Model.Application.EventSources.Single(source => source.Name == "Ledger");
        var postings = ledger.Streams.Single(stream => stream.Name == "Postings");
        Assert.Equal("stored-ledger", ledger.SourceKind);
        Assert.Equal("stored-postings", postings.StreamKind);
        var route = recovered.BoundCommand.Produces[1].Route;
        Assert.Equal(ledger.Id, route.Source);
        Assert.Equal(postings.Id, route.Stream);
        Assert.Equal(recovered.BoundCommand.Properties.Single(property => property.Name == "month").Id, Assert.IsType<SemanticResolvedExpression>(route.StreamId).Target);
        AssertResponse(recovered, false);
    }

    static void AssertResponse(given.arc_recovery recovered, bool record)
    {
        Assert.NotNull(recovered.Command.Authoring.ResponseType);
        if (record)
        {
            var field = Assert.Single(Assert.IsType<SemanticRecordCommandResponse>(recovered.BoundCommand.Response).Fields);
            Assert.Equal("name", field.Name);
            Assert.Equal(recovered.BoundCommand.Properties.Single(property => property.Name == "name").Id, field.Source);
            Assert.Equal(new PropertyPathSource("Name"), Assert.Single(recovered.Command.Authoring.ResponseFields).Source);
            return;
        }
        Assert.Equal("Name", recovered.Command.Authoring.Response);
        Assert.Equal(recovered.BoundCommand.Properties.Single(property => property.Name == "name").Id, Assert.IsType<SemanticScalarCommandResponse>(recovered.BoundCommand.Response).Source);
    }
}
#endif
