// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_InlineBodyScanner.when_scanning;

public class and_unicode_escapes_form_literal_delimiters : given.a_native_body
{
    void Establish() => _body = "\\u0022String\\u0022; \\u002f\\u002f List\nActual value;";
    void Because() => Scan();

    [Fact] void should_skip_the_decoded_string() => _identifiers.Contains("String").ShouldBeFalse();
    [Fact] void should_skip_the_decoded_comment() => _identifiers.Contains("List").ShouldBeFalse();
    [Fact] void should_not_desynchronize_literal_skipping() => _identifiers.Contains("Actual").ShouldBeTrue();
}
