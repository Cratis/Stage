// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_roundtripping_stage_generated_destinations : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_keep_the_generated_response_and_exact_destination_of_unmodified_rendered_code(bool routed)
    {
        var source = $$"""
            concept AuthorId : Uuid
            {{(routed ? "eventsource Account\n  identifier AuthorId\n  stream Registrations" : string.Empty)}}
            module Banking
              feature Authors
                slice StateChange Registration
                  command RegisterAuthor
                    author AuthorId generated identifier
                    name String
            {{(routed ? "        stream Account.Registrations" : string.Empty)}}
                    produces event AuthorRegistered
                      name String = name
                      for author
                    returns author
            """;
        var recovered = given.arc_recovery.Render(source);
        recovered.AssertHealthy();
        when_recovering_generated_tuple_destinations.AssertGenerated(recovered, 1);
        recovered.AssertNameMapping(0);
        var generated = recovered.BoundCommand.Properties.Single(property => property.IsGenerated);
        Assert.Equal(generated.Id, Assert.IsType<SemanticScalarCommandResponse>(recovered.BoundCommand.Response).Source);
        if (routed)
        {
            Assert.Equal("Account", recovered.Command.Authoring.Route.Source);
            Assert.Equal("Registrations", recovered.Command.Authoring.Route.Stream);
            var account = Assert.Single(recovered.Model.Application.EventSources);
            var registrations = Assert.Single(account.Streams);
            Assert.Equal(account.Id, recovered.BoundCommand.Route.Source);
            Assert.Equal(registrations.Id, recovered.BoundCommand.Route.Stream);
            Assert.Null(recovered.BoundCommand.Route.StreamId);
        }
    }
}
#endif
