// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaOutputIntegrity;

public class when_complete_output_has_no_integrity_violation : Specification
{
    Exception?[] _errors = [];

    void Because() => _errors = [.. new[] { "", "class Example {}\r\n", "class Example {} // 😀\n", @"class Example {} // \ud83d\ude00", @"class Example {} // \\uE000", @"class Example {} // \\uZZZZ", @"class Example {} // \u005cuE000" }
        .Select(text => Catch.Exception(() => JavaOutputIntegrity.Validate(text)))];

    [Fact] void should_accept_valid_pairs_and_preserve_jls_eligibility_without_recursive_decoding() => _errors.ShouldEachConformTo(error => error == null);
}
