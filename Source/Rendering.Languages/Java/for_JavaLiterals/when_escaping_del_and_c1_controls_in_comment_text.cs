// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLiterals;

public class when_escaping_del_and_c1_controls_in_comment_text : Specification
{
    string _input = string.Empty;
    string _result = string.Empty;

    void Establish() => _input = "before" + new string([.. Enumerable.Range(0x7F, 0x21).Select(value => (char)value)]) + "afteré\u00a0";

    void Because() => _result = JavaLiterals.EscapeLineComment(_input);

    [Fact] void should_flatten_del_and_every_c1_control() => _result.ShouldEqual("before" + new string(' ', 0x21) + "afteré\u00a0");
}
