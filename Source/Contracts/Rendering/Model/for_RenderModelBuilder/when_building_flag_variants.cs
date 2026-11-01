// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelBuilder;

public class when_building_flag_variants : given.a_response_only_command
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    void should_derive_each_flag_independently(bool identifier, bool generated, bool reads)
    {
        var source = $$"""
            {{(reads ? "command reads protected" : "")}}
            concept Token : String
              generate nanoid
            module Messaging
              feature Echoes
                slice StateChange Reply
                  command Echo
                    message String
                    id Uuid{{(identifier ? " identifier" : "")}}
                    {{(generated ? "token Token generated" : "")}}
                    {{(reads ? "reads Current by id" : "")}}
                    returns message
                slice StateChange Seed
                  event Seeded
                  event Removed
                  command Remove
                    id Uuid identifier
                    produces Removed
                      for id
                  command Seed
                    id Uuid identifier
                    produces Seeded
                      for id
                slice StateView View
                  readmodel Current
                    id Uuid key
                  projection BuildCurrent => Current
                    from Seeded
                      id = $eventSourceId
                    remove with Removed
            """;
        var result = RenderModelBuilder.Build(CompileRequest(source));
        result.Items.Commands.Single(value => value.Name == "Echo").Flags.ShouldEqual(new(identifier, generated, reads));
    }
}
