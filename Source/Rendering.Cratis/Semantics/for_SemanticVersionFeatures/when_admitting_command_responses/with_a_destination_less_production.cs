// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.when_admitting_command_responses;

public class with_a_destination_less_production : given.a_composite_command_response
{
    const string DestinationLess = """
        concept Receipt : Uuid
          generate uuidv7
        module Banking
          feature Recording
            slice StateChange Record
              command Record
                receipt Receipt generated identifier
                produces event Recorded
        """;

    SemanticCommand _command = null!;
    IEnumerable<string> _features = [];

    // Screenplay derives the destination from the generated identifier, so legacy destination allocation is reached only by a hand-built model.
    void Establish()
    {
        var recorded = CommandIn(DestinationLess);
        _command = recorded with { Produces = [.. recorded.Produces.Select(produced => produced with { Destination = null })] };
    }

    void Because() => _features = SemanticVersionFeatures.InCommand(_command).Select(feature => feature.Message);

    [Fact] void should_name_the_destination_less_production() => _features.ShouldContain(reason => reason.Contains("destination-less", StringComparison.Ordinal));
}
