// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.when_admitting_command_responses;

public class with_a_collection_response : given.a_composite_command_response
{
    SemanticCommand _command = null!;
    IEnumerable<string> _features = [];

    // Screenplay refuses collection responses (PLAY0489), so Stage's own refusal is reached only by a hand-built model.
    void Establish()
    {
        var composite = CommandIn(Composite);
        var address = composite.Properties.Single(property => property.Name == "address");
        _command = composite with { Response = new SemanticScalarCommandResponse(address.Id, address.Type with { IsCollection = true }) };
    }

    void Because() => _features = SemanticVersionFeatures.InCommand(_command).Select(feature => feature.Message);

    [Fact] void should_refuse_a_collection_response() => _features.ShouldContain(reason => reason.Contains("collection", StringComparison.Ordinal));
    [Fact] void should_refuse_a_collection_response_with_the_command_name() => _features.ShouldContain(reason => reason.StartsWith("Command 'Move'", StringComparison.Ordinal));
}
