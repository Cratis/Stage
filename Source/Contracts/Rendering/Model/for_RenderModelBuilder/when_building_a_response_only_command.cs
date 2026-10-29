// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelBuilder;

public class when_building_a_response_only_command : given.a_response_only_command
{
    RenderCommand Item => _result.Items.Commands.Single();

    void Because() => _result = RenderModelBuilder.Build(_request);

    [Fact] void should_bind_identity() => Item.Id.ShouldEqual(_command.Id);
    [Fact] void should_bind_location() => Item.Location.Slice.ShouldEqual(_slice.Id);
    [Fact] void should_split_words_without_target_casing() => Item.NameWords.ShouldEqual(["Echo"]);
    [Fact] void should_keep_property_identity() => Item.Payload.Properties.Single().Id.ShouldEqual(_command.Properties[0].Id);
    [Fact] void should_resolve_text() => Item.Payload.Properties.Single().Type.ShouldEqual(new(RenderTypeKind.Primitive, SemanticPrimitiveType.Text, null, false, false));
    [Fact] void should_keep_scalar_response_source() => Assert.IsType<RenderResponse.Scalar>(Item.Payload.Response).Source.ShouldEqual(_command.Properties[0].Id);
    [Fact] void should_keep_empty_productions() => Item.Payload.Produces.ShouldBeEmpty();
    [Fact] void should_keep_empty_reads() => Item.Payload.Reads.ShouldBeEmpty();
    [Fact] void should_keep_empty_validations() => Item.Payload.Validations.ShouldBeEmpty();
    [Fact] void should_keep_empty_requirements() => Item.Payload.Requirements.ShouldBeEmpty();
    [Fact] void should_keep_empty_code_validations() => Item.Payload.CodeValidations.ShouldBeEmpty();
    [Fact] void should_keep_absent_handler() => Item.Payload.Handler.ShouldBeNull();
    [Fact] void should_keep_absent_authorization() => Item.Payload.Authorization.ShouldBeNull();
    [Fact] void should_keep_absent_route() => Item.Payload.Route.ShouldBeNull();
    [Fact] void should_keep_absent_destination() => Item.Payload.Destination.ShouldBeNull();
    [Fact] void should_keep_false_flags() => Item.Flags.ShouldEqual(new(false, false, false));
    [Fact] void should_not_synthesize_reactions() => _result.Items.Reactions.ShouldBeEmpty();
    [Fact] void should_report_absent_provenance() => Item.Provenance.Reason.ShouldEqual("loader does not retain compiler SourceMap");
    [Fact] void should_apply_provenance_to_members() => Item.Payload.Properties.Single().Provenance.Status.ShouldEqual("Absent");
    [Fact] void should_build_structurally_equal_models() => RenderModelBuilder.Build(_request).ShouldEqual(_result);
    [Fact] void should_bind_revision() => _result.Source.Revision.ShouldEqual(_request.Model.Revision);
    [Fact] void should_keep_the_schema_version() => RenderModel.Schema.ShouldEqual("stage-render-model/v1");
}
