// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_local_class_is_named_like_the_placeholder_exception : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(Class("example", "A", Txt("class UnsupportedOperationException {}\n"), Txt(_pack.Placeholder(GapRole.ThrowingOperationBody, "C", "i")), Txt("\n")));

    [Fact] void should_keep_the_java_rooted_name_so_that_the_local_class_cannot_capture_it() =>
        Text(_result[0]).ShouldEqual(Expected("A", "class UnsupportedOperationException {}\nthrow new java.lang.UnsupportedOperationException(\"C: i\");\n"));
}
