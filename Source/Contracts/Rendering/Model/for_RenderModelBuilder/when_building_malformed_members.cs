// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelBuilder;

public class when_building_malformed_members : given.a_response_only_command
{
    [Fact]
    void should_reject_duplicate_mapping_targets_at_the_model_boundary()
    {
        var property = _command.Properties[0].Id;
        var mapping = new SemanticPropertyMapping(property, new SemanticValueExpression(new SemanticTextValue("mapped")));
        var command = _command with { Produces = [new(Id("event"), null, null, [mapping, mapping])] };
        var error = Assert.Throws<InvalidArtifactRenderContract>(() => new RenderPayloadNormalizer(_request.Model.Application).Command(command, _slice));
        error.Message.ShouldContain("Duplicate mapping target");
        error.Message.ShouldContain(property.ToString());
    }

    [Fact]
    void should_classify_a_missing_policy_as_an_invalid_contract()
    {
        var command = _command with { Authorization = new SemanticPolicyReference("missing-policy") };
        var error = Assert.Throws<InvalidArtifactRenderContract>(() => new RenderPayloadNormalizer(_request.Model.Application).Command(command, _slice));
        error.Message.ShouldEqual("Missing policy declaration 'missing-policy'.");
    }
}
