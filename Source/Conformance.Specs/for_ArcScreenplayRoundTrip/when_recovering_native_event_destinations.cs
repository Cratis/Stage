// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_recovering_native_event_destinations : Specification
{
    [Theory]
    [InlineData("string", "Id", "new EventSourceId((OtherId).ToString())")]
    [InlineData("Guid", "Id", "new EventSourceId((OtherId).ToString()!)")]
    [InlineData("int", "new EventSourceId(Id.ToString())", "new EventSourceId((OtherId).ToString())")]
    [InlineData("long", "new EventSourceId(Id.ToString())", "new EventSourceId(OtherId.ToString(CultureInfo.InvariantCulture))")]
    [InlineData("TextIdentity", "new EventSourceId(Id.ToString())", "new EventSourceId((OtherId).ToString())")]
    [InlineData("NumberIdentity", "new EventSourceId(Id.ToString())", "new EventSourceId((OtherId).ToString())")]
    [InlineData("UuidIdentity", "new EventSourceId(Id.ToString())", "new EventSourceId((OtherId).ToString())")]
    [InlineData("SourceIdentity", "Id", "OtherId")]
    public void should_bind_the_non_identifier_destination_to_other_id(string type, string identity, string destination)
    {
        var recovered = given.arc_recovery.Native(given.native_command_sources.Identifier(type, identity, destination));
        recovered.AssertHealthy(redundantRoute: true);
        recovered.AssertPropertyDestination(0, "OtherId");
        recovered.AssertNameMapping(0);
        Assert.Equal("Id", recovered.Command.Identifier);
        Assert.False(recovered.Command.Produces.Single().UsesCommandContext);
        Assert.Contains("for otherId", recovered.Result.Source, StringComparison.Ordinal);
        Assert.DoesNotContain("for id", recovered.Result.Source, StringComparison.Ordinal);
        Assert.Single(recovered.BoundCommand.Produces);
    }
}
#endif
