// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_recovering_native_command_identifiers : Specification
{
    [Theory]
    [InlineData("string", "Id", false)]
    [InlineData("string", "(EventSourceId)this.Id", true)]
    [InlineData("Guid", "Id", true)]
    [InlineData("Guid", "new EventSourceId((Id).ToString()!)", false)]
    [InlineData("int", "new EventSourceId((Id).ToString())", true)]
    [InlineData("long", "new EventSourceId(Id.ToString(CultureInfo.InvariantCulture))", false)]
    [InlineData("TextIdentity", "new EventSourceId((Id).ToString())", true)]
    [InlineData("TextIdentity", "new EventSourceId(Id)", false)]
    [InlineData("NumberIdentity", "new EventSourceId((Id).ToString())", false)]
    [InlineData("UuidIdentity", "new EventSourceId((Id).ToString())", true)]
    [InlineData("UuidIdentity", "new EventSourceId((Guid)Id)", false)]
    [InlineData("SourceIdentity", "Id", false)]
    [InlineData("EventSourceId", "Id", true)]
    public void should_preserve_only_the_proven_command_identifier(string type, string identity, bool block)
    {
        var recovered = given.arc_recovery.Native(given.native_command_sources.Identifier(type, identity, block: block));
        recovered.AssertHealthy();
        Assert.Equal("Id", recovered.Command.Identifier);
        Assert.DoesNotContain(recovered.BoundCommand.Properties, property => property.IsGenerated);
        Assert.Equal("Account", recovered.Command.Authoring.Route.Source);
        Assert.Equal("Main", recovered.Command.Authoring.Route.Stream);
        Assert.Equal("AuthorRegistered", recovered.Command.Produces.Single().EventName);
        Assert.True(recovered.BoundCommand.Properties.Single(property => property.Name == "id").IsIdentifier);
        Assert.False(recovered.BoundCommand.Properties.Single(property => property.Name == "otherId").IsIdentifier);
        Assert.Single(recovered.BoundCommand.Produces);
        Assert.True(recovered.Command.Produces.Single().UsesCommandContext);
        Assert.Contains("for id", recovered.Result.Source, StringComparison.Ordinal);
        recovered.AssertNameMapping(0);
    }
}
#endif
