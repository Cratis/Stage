// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Http.for_HttpBlockParser;

public class when_reading_an_invalid_block : Specification
{
    HttpBlockParse _authorization = null!;
    HttpBlockParse _apiKey = null!;
    HttpBlockParse _noResponse = null!;
    HttpBlockParse _noMethod = null!;
    HttpBlockParse _absoluteAddress = null!;
    HttpBlockParse _traversal = null!;
    HttpBlockParse _getWithBody = null!;
    HttpBlockParse _json = null!;
    HttpBlockParse _twice = null!;
    HttpBlockParse _responseOnRequest = null!;
    HttpBlockParse _empty = null!;

    void Because()
    {
        _authorization = Read("post \"/a\"\nheader \"authorization\" = \"Bearer abc\"\nresponse 200");
        _apiKey = Read("post \"/a\"\nheader \"X-Api-Key\" = \"abc\"\nresponse 200");
        _noResponse = Read("post \"/a\"");
        _noMethod = Read("response 200");
        _absoluteAddress = Read("post \"https://elsewhere.example/a\"\nresponse 200");
        _traversal = Read("post \"/a/../b\"\nresponse 200");
        _getWithBody = Read("get \"/a\"\nbody\n  x = 1\nresponse 200");
        _json = Read("post \"/a\"\nbody\n  {\"x\": 1}\nresponse 200");
        _twice = Read("post \"/a\"\nresponse 200\nresponse 200");
        _responseOnRequest = Read("post \"/a\"\nheader \"X\" = 1 + 2\nresponse 200");
        _empty = Read("http");
    }

    static HttpBlockParse Read(string block) => HttpBlockParser.Parse("Carrier", "Ability", block);

    [Fact] void should_refuse_an_authorization_header_because_credentials_come_from_settings() => _authorization.Errors.Single().ShouldContain("settings and secrets");
    [Fact] void should_refuse_the_api_key_header_whatever_its_case() => _apiKey.Errors.Single().ShouldContain("settings and secrets");
    [Fact] void should_have_no_definition_for_an_invalid_block() => _authorization.Definition.ShouldBeNull();
    [Fact] void should_need_a_response() => _noResponse.Errors.ShouldContain("The http block needs at least one response line.");
    [Fact] void should_need_a_method() => _noMethod.Errors.Any(error => error.Contains("method and a path", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_refuse_an_absolute_address() => _absoluteAddress.Errors.Single().ShouldContain("base address");
    [Fact] void should_refuse_a_path_that_climbs() => _traversal.Errors.Single().ShouldContain("base address");
    [Fact] void should_refuse_a_body_on_get() => _getWithBody.Errors.Single().ShouldContain("cannot have a body");
    [Fact] void should_refuse_json() => _json.Errors.Single().ShouldContain("is not a body field");
    [Fact] void should_refuse_one_status_twice() => _twice.Errors.Single().ShouldContain("200");
    [Fact] void should_refuse_something_that_is_not_a_value() => _responseOnRequest.Errors.Single().ShouldContain("is not a value");
    [Fact] void should_refuse_an_empty_block() => _empty.Errors.Single().ShouldContain("empty");
    [Fact] void should_name_the_line_of_an_error() => _json.Errors.Single().StartsWith("Line 3:", StringComparison.Ordinal).ShouldBeTrue();
}
