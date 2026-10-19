// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_admitting_command_responses : Specification
{
    internal const string Composite = """
        concept ProjectId : Uuid
        eventsource Project
          identifier ProjectId
        type Address
          street String
          city String
        module Projects
          feature Moving
            slice StateChange Move
              command Move
                projectId ProjectId identifier
                address Address
                produces event Moved
                  for projectId
                  address Address = address
                returns address
        """;

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

    IEnumerable<string> _composite = [];
    IEnumerable<string> _collection = [];
    IEnumerable<string> _destinationLess = [];
    string _code = string.Empty;

    static SemanticCommand CommandIn(string source) => SemanticVersionFeatures.Slices(invoice_model.Compile(source).Application).Single().Commands.Single();

    void Because()
    {
        var composite = CommandIn(Composite);
        var address = composite.Properties.Single(property => property.Name == "address");

        // Screenplay refuses collection responses (PLAY0489), so Stage's own refusal is reached only by a hand-built model.
        var collection = composite with { Response = new SemanticScalarCommandResponse(address.Id, address.Type with { IsCollection = true }) };
        _collection = SemanticVersionFeatures.InCommand(collection).Select(feature => feature.Message);
        _composite = SemanticVersionFeatures.InCommand(composite).Select(feature => feature.Message);

        // Screenplay derives the destination from the generated identifier, so legacy destination allocation is reached only by a hand-built model.
        var recorded = CommandIn(DestinationLess);
        var destinationLess = recorded with { Produces = [.. recorded.Produces.Select(produced => produced with { Destination = null })] };
        _destinationLess = SemanticVersionFeatures.InCommand(destinationLess).Select(feature => feature.Message);
        var plan = invoice_model.Plan(invoice_model.Compile(Composite));
        Assert.True(plan.Success, string.Join('\n', plan.Diagnostics));
        _code = Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Move.cs", StringComparison.Ordinal)).Bytes.AsSpan());
    }

    [Fact] void should_admit_a_composite_response() => _composite.ShouldBeEmpty();
    [Fact] void should_return_the_composite_value_with_its_event() => Assert.Matches(@"\((global::[\w.]+\.)?Address, [^()]+\) Handle\(", _code);
    [Fact] void should_refuse_a_collection_response() => _collection.ShouldContain(reason => reason.Contains("collection", StringComparison.Ordinal));
    [Fact] void should_refuse_a_collection_response_with_the_command_name() => _collection.ShouldContain(reason => reason.StartsWith("Command 'Move'", StringComparison.Ordinal));
    [Fact] void should_name_the_destination_less_production() => _destinationLess.ShouldContain(reason => reason.Contains("destination-less", StringComparison.Ordinal));
}
