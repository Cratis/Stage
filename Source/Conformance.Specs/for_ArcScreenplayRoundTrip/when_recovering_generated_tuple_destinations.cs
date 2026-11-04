// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_recovering_generated_tuple_destinations : Specification
{
    [Theory]
    [InlineData("(string, EventForEventSourceId)", "(Name, WRAPPER)")]
    [InlineData("(EventForEventSourceId, string)", "(WRAPPER, Name)")]
    [InlineData("(string, EventForEventSourceId[])", "(Name, [WRAPPER])")]
    [InlineData("(EventForEventSourceId[], string)", "([WRAPPER], Name)")]
    [InlineData("Task<(string, EventForEventSourceId)>", "Task.FromResult((Name, WRAPPER))")]
    [InlineData("ValueTask<(EventForEventSourceId, string)>", "ValueTask.FromResult((WRAPPER, Name))")]
    [InlineData("Task<(string, EventForEventSourceId[])>", "Task.FromResult((Name, new[] { WRAPPER }))")]
    public void should_preserve_the_generated_destination_and_scalar_response(string signature, string expression)
    {
        var recovered = given.arc_recovery.Native(given.native_command_sources.Generated(signature, expression));
        recovered.AssertHealthy();
        AssertGenerated(recovered, 1);
        Assert.Equal("AuthorRegistered", Assert.Single(recovered.Command.Produces).EventName);
        recovered.AssertNameMapping(0);
        Assert.Equal("Name", recovered.Command.Authoring.Response);
        Assert.Equal(recovered.BoundCommand.Properties.Single(property => property.Name == "name").Id, Assert.IsType<SemanticScalarCommandResponse>(recovered.BoundCommand.Response).Source);
    }

    [Theory]
    [InlineData("(string, EventForEventSourceId, EventForEventSourceId)", "(Name, WRAPPER, AUDIT)")]
    [InlineData("(string, EventForEventSourceId[])", "(Name, [WRAPPER, AUDIT])")]
    public void should_keep_one_generated_identity_for_wrapper_only_siblings(string signature, string expression)
    {
        var recovered = given.arc_recovery.Native(given.native_command_sources.Generated(signature, expression));
        recovered.AssertHealthy();
        AssertGenerated(recovered, 2);
        Assert.Equal(["AuthorRegistered", "AuthorAudited"], recovered.Command.Produces.Select(production => production.EventName));
        recovered.AssertNameMapping(0);
        recovered.AssertNameMapping(1);
    }

    internal static void AssertGenerated(given.arc_recovery recovered, int productions)
    {
        Assert.Equal("author", recovered.Command.Authoring.Identifier);
        var generated = Assert.Single(recovered.BoundCommand.Properties.Where(property => property.IsGenerated));
        Assert.Equal("author", generated.Name);
        Assert.True(generated.IsIdentifier);
        Assert.Equal(productions, recovered.BoundCommand.Produces.Length);
        Assert.All(recovered.BoundCommand.Produces, production => Assert.Equal(generated.Id, Assert.IsType<SemanticResolvedExpression>(production.Destination).Target));
        Assert.Contains("author AuthorId generated identifier", recovered.Result.Source, StringComparison.Ordinal);
        Assert.Contains("for author", recovered.Result.Source, StringComparison.Ordinal);
    }
}
#endif
