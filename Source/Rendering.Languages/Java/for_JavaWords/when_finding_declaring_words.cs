// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaWords;

public class when_finding_declaring_words : Specification
{
    JavaWordSet _qualified = null!;
    JavaWordSet _varargs = null!;
    JavaWordSet _whitespace = null!;
    JavaWordSet _ignorable = null!;
    JavaWordSet _before_dot = null!;

    void Because()
    {
        _qualified = JavaWords.Scan("java.util.List");
        _varargs = JavaWords.Scan("String... args");
        _whitespace = JavaWords.Scan("Space \t\f\r\n. Next\u2028. Nonbreaking\u00a0.");
        _ignorable = JavaWords.Scan("Jav\u0000a.util.List");
        _before_dot = JavaWords.Scan("java\u0000.util.List");
    }

    [Fact] void should_find_all_qualified_name_segments() => _qualified.All.SetEquals(["java", "util", "List"]).ShouldBeTrue();
    [Fact] void should_only_declare_the_last_qualified_segment() => _qualified.Declaring.SetEquals(["List"]).ShouldBeTrue();
    [Fact] void should_count_the_varargs_name() => _varargs.Declaring.SetEquals(["args"]).ShouldBeTrue();
    [Fact] void should_skip_only_java_whitespace_before_a_dot() => _whitespace.Declaring.SetEquals(["Next", "Nonbreaking"]).ShouldBeTrue();
    [Fact] void should_union_the_separated_and_joined_declaring_passes() => _ignorable.Declaring.SetEquals(["Jav", "List"]).ShouldBeTrue();
    [Fact] void should_not_treat_an_ignorable_separator_as_java_whitespace() => _before_dot.Declaring.SetEquals(["java", "List"]).ShouldBeTrue();
}
