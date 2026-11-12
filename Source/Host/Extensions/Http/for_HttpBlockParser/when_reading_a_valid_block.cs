// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Http.for_HttpBlockParser;

public class when_reading_a_valid_block : Specification
{
    const string Block = """
        http
          post "/pickups"
          header "Idempotency-Key" = $call.idempotencyKey
          query "dryRun" = false
          body
            reference = parcelId
            address
              street = address.street
              floor = 3
          response 201
            trackingNumber = body.tracking
            httpStatus = status
          response 409 -> NoCapacity
        """;

    HttpBlockParse _result = null!;

    void Because() => _result = HttpBlockParser.Parse("Carrier", "BookPickup", Block);

    [Fact] void should_have_no_errors() => _result.Errors.ShouldBeEmpty();
    [Fact] void should_name_the_ability() => (_result.Definition!.System + "." + _result.Definition.Ability).ShouldEqual("Carrier.BookPickup");
    [Fact] void should_read_the_method_and_path() => (_result.Definition!.Method + " " + _result.Definition.Path).ShouldEqual("POST /pickups");
    [Fact] void should_read_a_call_value_in_a_header() => _result.Definition!.Headers.Single().Value.Kind.ShouldEqual(HttpValueKind.Call);
    [Fact] void should_read_a_literal_in_the_query() => _result.Definition!.Query.Single().Value.Kind.ShouldEqual(HttpValueKind.Literal);
    [Fact] void should_read_the_body_fields_in_order() => _result.Definition!.Body.Select(field => field.Name).ShouldEqual(["reference", "address"]);
    [Fact] void should_read_a_nested_object() => _result.Definition!.Body[1].Fields.Select(field => field.Name).ShouldEqual(["street", "floor"]);
    [Fact] void should_read_the_responses() => _result.Definition!.Responses.Select(rule => rule.Status).ShouldEqual([201, 409]);
    [Fact] void should_read_the_outcome_of_a_status() => _result.Definition!.Responses[1].Outcome.ShouldEqual("NoCapacity");
    [Fact] void should_read_what_is_returned() => _result.Definition!.Responses[0].Output.Select(value => value.Name).ShouldEqual(["trackingNumber", "httpStatus"]);
}
