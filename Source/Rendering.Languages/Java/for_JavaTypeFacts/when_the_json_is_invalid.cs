// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaTypeFacts;

public class when_the_json_is_invalid : Specification
{
    Exception[] _errors = [];

    void Because()
    {
        string[] inputs =
        [
            "not json", "null", "[]", "{}",
            "{\"types\":{},\"unknown\":true}", "{\"types\":null}", "{\"types\":[]}",
            "{\"types\":{\"example.Outer\":{\"names\":[]}}}",
            "{\"types\":{\"example..Outer\":[]}}", "{\"types\":{\"example.class\":[]}}",
            "{\"types\":{\"example.Outer$Inner\":[]}}", "{\"types\":{\"example.Outer\":[\"\"]}}",
            "{\"types\":{\"example.Outer\":[\"1Name\"]}}", "{\"types\":{\"example.Outer\":[\"class\"]}}",
            "{\"types\":{\"example.Outer\":[null]}}", "{\"types\":{\"example.Outer\":[\"two words\"]}}",
            "{\"types\":{},\"types\":{}}", "{\"types\":{\"example.Outer\":[],\"example.Outer\":[]}}"
        ];
        _errors = [.. inputs.Select(json => Catch.Exception(() => JavaTypeFacts.Parse(json)))];
    }

    [Fact] void should_throw_a_type_facts_exception_for_every_invalid_shape() => _errors.ShouldEachConformTo(error => error is JavaTypeFactsException);
}
