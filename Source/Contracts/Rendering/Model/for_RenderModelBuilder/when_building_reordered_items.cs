// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelBuilder;

public class when_building_reordered_items : given.a_response_only_command
{
    void Establish() => _request = CompileRequest("""
        module Messaging
          feature Echoes
            slice StateChange Reply
              command FirstEcho
                message String
                returns message
              command SecondEcho
                message String
                returns message
            slice Automation React
              reaction FirstReaction
                when Startup
              reaction SecondReaction
                when Shutdown
        """);

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    void should_ignore_command_and_reaction_declaration_order(bool reverseCommands, bool reverseReactions)
    {
        var application = _request.Model.Application;
        var module = application.Modules[0];
        var feature = module.Features[0];
        var commandSlice = feature.Slices[0];
        var reactionSlice = feature.Slices[1];
        var expected = RenderModelBuilder.Build(_request);
        var reordered = feature with
        {
            Slices = [
                commandSlice with { Commands = reverseCommands ? [.. commandSlice.Commands.Reverse()] : commandSlice.Commands },
                reactionSlice with { Reactions = reverseReactions ? [.. reactionSlice.Reactions.Reverse()] : reactionSlice.Reactions }
            ]
        };
        var actual = RenderModelBuilder.Build(WithApplication(application with { Modules = [module with { Features = [reordered] }] }));
        actual.ShouldEqual(expected);
        actual.Items.Commands.Select(value => value.Id.ToString()).ShouldEqual(commandSlice.Commands.Select(value => value.Id.ToString()).Order(StringComparer.Ordinal));
        actual.Items.Reactions.Select(value => value.Id.ToString()).ShouldEqual(reactionSlice.Reactions.Select(value => value.Id.ToString()).Order(StringComparer.Ordinal));
    }
}
