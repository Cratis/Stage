// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Specifications.Commands.for_SemanticQueryValidation;

public class when_validating_multipart_query_arguments : given.a_multipart_query
{
    SemanticSpecificationQueryResult _request = null!;
    string[] _messages = [];
    string[] _reordered = [];

    void Establish()
    {
        var region = _plan.Model.Application.Concepts.Single(concept => concept.Name == "RegionCode");
        var type = new SemanticTypeReference(SemanticTypeReferenceKind.Concept, SemanticPrimitiveType.Unknown, region.Id, false, false);
        _query = _query with { Filters = [new(_query.Id, "region", type, default), new(region.Id, "omitted", type with { IsOptional = true }, default)] };
        _request = new(_query.Id, new SemanticCompositeValue([new(_method, SemanticValue.Text("x")), new(_invoice, SemanticValue.Text("X"))]), [])
        {
            Filters = [new(_query.Id, SemanticValue.Text("Z"))]
        };
    }

    void Because()
    {
        _messages = [.. SemanticQueryValidation.Failures(_plan, _query, _request).Select(failure => failure.Message)];
        var key = (SemanticCompositeValue)_request.Key;
        var reordered = _request with { Key = key with { Properties = [.. key.Properties.Reverse()] } };
        _reordered = [.. SemanticQueryValidation.Failures(_plan, _query, reordered).Select(failure => failure.Message)];
    }

    [Fact] void should_have_no_scalar_argument() => _query.Argument.ShouldBeNull();
    [Fact] void should_validate_key_parts_in_model_order_before_supplied_filters() => _messages.ShouldEqual(["Invoice id is too short", "Method is too short", "Region is too short"]);
    [Fact] void should_match_members_by_property_identity_instead_of_position() => _reordered.ShouldEqual(_messages);
    [Fact] void should_not_validate_an_omitted_optional_filter() => _messages.Length.ShouldEqual(3);
}
