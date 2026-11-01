// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Commands.for_SemanticQueryValidation;

public class when_validating_incomplete_multipart_keys : given.a_multipart_query
{
    [Theory]
    [InlineData("missing")]
    [InlineData("scalar")]
    [InlineData("null")]
    public void should_leave_unsupplied_parts_to_existing_shape_handling(string shape)
    {
        var key = shape switch
        {
            "missing" => new SemanticCompositeValue([new(_method, SemanticValue.Text("card"))]),
            "scalar" => SemanticValue.Text("not a composite"),
            _ => SemanticValue.Null
        };
        var request = new SemanticSpecificationQueryResult(_query.Id, key, []);
        SemanticQueryValidation.Failures(_plan, _query, request).ShouldBeEmpty();
    }

    [Fact]
    void should_delegate_an_explicit_null_member_without_synthesizing_a_value()
    {
        var key = new SemanticCompositeValue([new(_invoice, SemanticValue.Null)]);
        var request = new SemanticSpecificationQueryResult(_query.Id, key, []);
        var part = _query.KeyParts.Single(part => part.KeyProperty == _invoice);
        var concepts = _plan.Model.Application.Concepts.ToDictionary(concept => concept.Id);
        var types = _plan.Model.Application.Types.ToDictionary(type => type.Id);
        SemanticValidationFailure[] expectedFailures = [];
        SemanticValidationFailure[] actualFailures = [];
        var expected = Catch.Exception(() => expectedFailures = [.. SemanticConceptValues.Validate(part.Type, SemanticValue.Null, concepts, types)]);
        var actual = Catch.Exception(() => actualFailures = [.. SemanticQueryValidation.Failures(_plan, _query, request)]);
        (actual?.GetType()).ShouldEqual(expected?.GetType());
        actualFailures.ShouldEqual(expectedFailures);
    }
}
