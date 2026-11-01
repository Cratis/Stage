// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelBuilder;

public class when_building_command_variants : given.a_response_only_command
{
    RenderCommandPayload _absent = null!;
    RenderCommandPayload _record = null!;

    void Because()
    {
        var property = _command.Properties[0];
        var normalizer = new RenderPayloadNormalizer(_request.Model.Application);
        _absent = normalizer.Command(_command with { Response = null, Properties = [property with { IsIdentifier = true, IsGenerated = true }], Handler = new("body", 2), CodeValidations = [new("validation")] }, _slice);
        _record = normalizer.Command(_command with { Response = new SemanticRecordCommandResponse([new("message", property.Type, property.Id)]) }, _slice);
    }

    [Fact] void should_keep_record_response_fields() => Assert.IsType<RenderResponse.Record>(_record.Response).Fields.Single().Source.ShouldEqual(_command.Properties[0].Id);
    [Fact] void should_keep_absent_response_distinct_from_record() => Assert.IsType<RenderResponse.Absent>(_absent.Response);
    [Fact] void should_keep_handler_reference() => _absent.Handler.ShouldEqual(new("body", 2));
    [Fact] void should_keep_code_validation_reference() => _absent.CodeValidations.Single().RequirementId.ShouldEqual("validation");
    [Fact] void should_keep_identifier_flag() => _absent.Properties.Single().IsIdentifier.ShouldBeTrue();
    [Fact] void should_keep_generated_flag() => _absent.Properties.Single().IsGenerated.ShouldBeTrue();
    [Fact] void should_not_mutate_input_flags() => _command.Properties[0].IsGenerated.ShouldBeFalse();
}
